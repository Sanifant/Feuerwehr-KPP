import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import type { FeatureLike } from 'ol/Feature';
import Feature from 'ol/Feature';
import Map from 'ol/Map';
import View from 'ol/View';
import Point from 'ol/geom/Point';
import Geometry from 'ol/geom/Geometry';
import OSM from 'ol/source/OSM';
import VectorSource from 'ol/source/Vector';
import TileLayer from 'ol/layer/Tile';
import VectorLayer from 'ol/layer/Vector';
import { Draw, Modify, Select, Translate } from 'ol/interaction';
import GeoJSON from 'ol/format/GeoJSON';
import { fromLonLat, toLonLat } from 'ol/proj';
import { Fill, Stroke, Style, Text, Circle as CircleStyle } from 'ol/style';
import { click, never, primaryAction } from 'ol/events/condition';
import { unByKey } from 'ol/Observable';
import { useAuth } from '../AuthContext';
import { useRole } from '../hooks/useRole';
import {
    acquireMapLease,
    buildCommandId,
    closeIncident,
    createIncident,
    deleteMapElement,
    getActiveIncident,
    getMapAudit,
    getMapAuditExportUrl,
    getMapPdfExportUrl,
    getMapState,
    heartbeatMapLease,
    releaseMapLease,
    upsertMapElement,
} from '../api/incidentApi';
import type { ActiveIncidentSnapshot, EditorLease, IncidentSummary, MapElementDto } from '../api/incidentApi';
import { ensureIncidentConnection } from '../api/incidentLive';
import 'ol/ol.css';

const symbolCatalog = [
    { id: 'vehicle-engine', category: 'Fahrzeuge', label: 'Löschfahrzeug', icon: '🚒' },
    { id: 'vehicle-ladder', category: 'Fahrzeuge', label: 'Drehleiter', icon: '🪜' },
    { id: 'unit-command', category: 'Führung', label: 'Einsatzleitung', icon: '⭐' },
    { id: 'hazard-smoke', category: 'Gefahrenstellen', label: 'Rauch', icon: '💨' },
    { id: 'water-source', category: 'Wasserentnahme', label: 'Wasserentnahme', icon: '💧' },
    { id: 'staging-area', category: 'Bereitstellungsräume', label: 'Bereitstellungsraum', icon: '📍' },
    { id: 'unit-team', category: 'Einheiten', label: 'Trupp', icon: '👥' },
];

function getSessionId() {
    const existing = sessionStorage.getItem('incident-session-id');
    if (existing) {
        return existing;
    }

    const id = crypto.randomUUID();
    sessionStorage.setItem('incident-session-id', id);
    return id;
}

function toFeature(element: MapElementDto): Feature<Geometry> {
    const geoJson = new GeoJSON();
    const geometry = geoJson.readGeometry(element.geometryJson, {
        dataProjection: 'EPSG:4326',
        featureProjection: 'EPSG:3857',
    });

    const feature = new Feature({
        geometry,
        elementId: element.id,
        elementType: element.elementType,
        symbolId: element.symbolId,
        label: element.label,
        colorHex: element.colorHex,
        version: element.version,
        sourceElement: element,
    });

    feature.setId(element.id);
    feature.setStyle(createFeatureStyle(feature));
    return feature;
}

function createFeatureStyle(feature: FeatureLike, selected = false) {
    const label = `${feature.get('symbolId') ? `${feature.get('symbolId')} ` : ''}${feature.get('label') ?? ''}`.trim();
    const color = feature.get('colorHex') || '#d62828';

    if (feature.getGeometry() instanceof Point) {
        return new Style({
            image: new CircleStyle({
                radius: selected ? 10 : 8,
                fill: new Fill({ color }),
                stroke: new Stroke({ color: selected ? '#1677ff' : '#fff', width: selected ? 4 : 2 }),
            }),
            text: new Text({
                text: label,
                offsetY: -16,
                fill: new Fill({ color: '#111' }),
                stroke: new Stroke({ color: '#fff', width: 2 }),
            }),
        });
    }

    return new Style({
        fill: new Fill({ color: `${color}66` }),
        stroke: new Stroke({ color: selected ? '#1677ff' : color, width: selected ? 4 : 2 }),
        text: new Text({
            text: label,
            fill: new Fill({ color: '#111' }),
            stroke: new Stroke({ color: '#fff', width: 2 }),
        }),
    });
}

type EditMode = 'move' | 'symbol' | 'polygon' | 'vertices';

interface MapAuditRow {
    id: string;
    sequenceNumber: number;
    occurredAtUtc: string;
    action: string;
    userDisplayName: string;
    elementId?: string;
}

const geometryFormat = new GeoJSON();

/** Persistenz in WGS84; OpenLayers-Geometrien bleiben in Web Mercator. */
function writeGeometry(geometry: Geometry) {
    return geometryFormat.writeGeometry(geometry, {
        dataProjection: 'EPSG:4326',
        featureProjection: 'EPSG:3857',
    });
}

/** Bestehende Metadaten und Versionsprüfung bei einer Geometrieänderung erhalten. */
function elementPayload(element: MapElementDto) {
    return {
        elementId: element.id,
        elementType: element.elementType,
        symbolId: element.symbolId,
        category: element.category,
        label: element.label,
        radioCallName: element.radioCallName,
        strength: element.strength,
        note: element.note,
        colorHex: element.colorHex,
        expectedVersion: element.version,
    };
}

function getHttpStatus(error: unknown): number | undefined {
    return (error as { response?: { status?: number } } | null)?.response?.status;
}

function getErrorTitle(error: unknown, fallback: string): string {
    return (error as { response?: { data?: { title?: string } } } | null)?.response?.data?.title ?? fallback;
}

export function IncidentMapPage() {
    const { hasRole } = useAuth();
    const { canEditSituationMap: canEdit, canViewSituationMap: canView } = useRole();
    const sessionId = useMemo(() => getSessionId(), []);
    const mapRef = useRef<Map | null>(null);
    const mapContainerRef = useRef<HTMLDivElement>(null);
    const vectorSourceRef = useRef(new VectorSource<Feature<Geometry>>());
    const hydrantSourceRef = useRef(new VectorSource<Feature<Point>>());
    const drawRef = useRef<Draw | null>(null);
    const modifyRef = useRef<Modify | null>(null);
    const translateRef = useRef<Translate | null>(null);
    const selectRef = useRef<Select | null>(null);

    const [incident, setIncident] = useState<IncidentSummary | null>(null);
    const [elements, setElements] = useState<MapElementDto[]>([]);
    const [auditRows, setAuditRows] = useState<MapAuditRow[]>([]);
    const [lease, setLease] = useState<EditorLease | null>(null);
    const [hydrants, setHydrants] = useState<ActiveIncidentSnapshot['hydrants']>([]);
    const [statusText, setStatusText] = useState('Initialisiere...');
    const [selectedSymbol, setSelectedSymbol] = useState(symbolCatalog[0].id);
    const [searchSymbol, setSearchSymbol] = useState('');
    const [filterCategory, setFilterCategory] = useState('Alle');
    const [showHydrants, setShowHydrants] = useState(true);
    const [selectedElementId, setSelectedElementId] = useState<string | null>(null);
    const [newIncidentName, setNewIncidentName] = useState('');
    const [newIncidentNumber, setNewIncidentNumber] = useState('');
    const [newIncidentKeyword, setNewIncidentKeyword] = useState('');
    const [newIncidentDescription, setNewIncidentDescription] = useState('');
    const [selectedCreationPosition, setSelectedCreationPosition] = useState<[number, number] | null>(null);
    const [editMode, setEditMode] = useState<EditMode>('move');
    const [isSaving, setIsSaving] = useState(false);
    const [isInteracting, setIsInteracting] = useState(false);
    const [writePaused, setWritePaused] = useState(false);

    // Die OpenLayers-Instanz bleibt bestehen. Ihre Listener lesen aktuelle React-Werte
    // aus diesem Ref, statt bei jedem Heartbeat die komplette Karte neu aufzubauen.
    const stateRef = useRef({ incident, lease, canEdit, selectedSymbol, editMode, writePaused });
    const savingRef = useRef(false);
    const pausedRef = useRef(false);
    const geometryBeforeRef = useRef(new globalThis.Map<Feature<Geometry>, Geometry>());
    const interactingRef = useRef(false);
    const refreshPendingRef = useRef(false);
    const loadRequestRef = useRef(0);
    const centeredIncidentRef = useRef<string | null>(null);
    const serverClockOffsetRef = useRef(0);

    useEffect(() => {
        stateRef.current = { incident, lease, canEdit, selectedSymbol, editMode, writePaused };
    }, [incident, lease, canEdit, selectedSymbol, editMode, writePaused]);

    const pauseWriting = useCallback((paused: boolean) => {
        pausedRef.current = paused;
        setWritePaused(paused);
    }, []);

    // Auch unmittelbar im Pointer-Handler prüfen: Ein abgelaufener Lease darf nicht
    // bis zum nächsten React-Render weiterverwendet werden. Der Server prüft erneut.
    const canWriteNow = useCallback(() => {
        const current = stateRef.current;
        return current.canEdit && current.incident?.status === 'Active' &&
            current.lease?.sessionId === sessionId && !current.lease.isExpired &&
            Date.parse(current.lease.expiresAtUtc) > Date.now() + serverClockOffsetRef.current &&
            !pausedRef.current;
    }, [sessionId]);

    const isLeaseOwner = Boolean(lease && lease.sessionId === sessionId);
    const canWrite = canEdit && incident?.status === 'Active' && isLeaseOwner &&
        Boolean(lease && !lease.isExpired && Date.parse(lease.expiresAtUtc) > Date.now() + serverClockOffsetRef.current) &&
        !writePaused;
    const isBusy = isSaving || isInteracting;
    const selectedElement = elements.find(x => x.id === selectedElementId) ?? null;

    const loadActive = useCallback(async (afterSave = false): Promise<boolean> => {
        // Live-Meldungen dürfen ein gerade gezogenes Feature nicht ersetzen.
        if (interactingRef.current || (savingRef.current && !afterSave)) {
            refreshPendingRef.current = true;
            return false;
        }
        const request = ++loadRequestRef.current;
        try {
            const active = await getActiveIncident();
            const snapshot = await getMapState(active.id);
            if (request !== loadRequestRef.current) return false;
            if (interactingRef.current || (savingRef.current && !afterSave)) {
                refreshPendingRef.current = true;
                return false;
            }
            serverClockOffsetRef.current = Date.parse(snapshot.serverUtc) - Date.now();
            // Listener ebenfalls sofort aktualisieren, bevor React neu gerendert hat.
            stateRef.current.incident = { ...active, status: snapshot.incidentStatus };
            stateRef.current.lease = snapshot.lease;
            setIncident(stateRef.current.incident);
            setElements(snapshot.elements);
            setLease(snapshot.lease);
            setHydrants(snapshot.hydrants);
            pauseWriting(false);
            refreshPendingRef.current = false;
            setStatusText(snapshot.lease
                ? `Sperre: ${snapshot.lease.userDisplayName} bis ${new Date(snapshot.lease.expiresAtUtc).toLocaleTimeString('de-DE')}`
                : 'Keine aktive Bearbeitungssperre.');

            // Ein Fehler beim Protokollladen löscht nicht die erfolgreich geladene Karte.
            try {
                const logs = await getMapAudit(active.id);
                if (request === loadRequestRef.current) setAuditRows(logs);
            } catch {
                if (request === loadRequestRef.current) setStatusText('Karte geladen; Änderungslog derzeit nicht erreichbar.');
            }
            return true;
        } catch (error: unknown) {
            if (request !== loadRequestRef.current) return false;
            if (getHttpStatus(error) === 404) {
                stateRef.current.incident = null;
                stateRef.current.lease = null;
                setIncident(null);
                setElements([]);
                setLease(null);
                setHydrants([]);
                setAuditRows([]);
                setSelectedElementId(null);
                setStatusText('Kein aktiver Einsatz vorhanden.');
            } else {
                // Bei Netzfehlern den letzten Kartenstand behalten, aber Schreiben sperren.
                setStatusText('Aktualisierung fehlgeschlagen. Letzter Stand wird angezeigt; Schreiben pausiert.');
            }
            pauseWriting(true);
            return false;
        }
    }, [pauseWriting]);

    const restoreGeometry = useCallback(() => {
        geometryBeforeRef.current.forEach((geometry, feature) => feature.setGeometry(geometry.clone()));
        geometryBeforeRef.current.clear();
        interactingRef.current = false;
        setIsInteracting(false);
    }, []);

    // Alle Schreibaktionen werden serialisiert. Erst nach Übernahme des neuen
    // Serverstands wird die nächste Aktion mit der aktualisierten Version erlaubt.
    const runMutation = useCallback(async (
        operation: (context: { incident: IncidentSummary; lease: EditorLease }) => Promise<void>,
        successMessage: string,
        failureMessage: string,
        rollback?: () => void,
    ) => {
        const current = stateRef.current;
        if (savingRef.current) return false;
        if (!canWriteNow() || !current.incident || !current.lease) {
            rollback?.();
            restoreGeometry();
            setStatusText('Keine gültige Bearbeitungssperre. Änderung wurde zurückgesetzt.');
            return false;
        }
        savingRef.current = true;
        setIsSaving(true);
        ++loadRequestRef.current; // Antworten älterer Leseanfragen nicht mehr anwenden.
        let saved = false;
        try {
            await operation({ incident: current.incident, lease: current.lease });
            saved = true;
        } catch {
            rollback?.();
            restoreGeometry();
        } finally {
            geometryBeforeRef.current.clear();
            interactingRef.current = false;
            setIsInteracting(false);
        }
        const refreshed = await loadActive(true);
        savingRef.current = false;
        setIsSaving(false);
        setStatusText(saved
            ? `${successMessage}${refreshed ? '' : ' Aktualisierung fehlgeschlagen; Schreiben pausiert.'}`
            : `${failureMessage}${refreshed ? ' Serverstand wurde neu geladen.' : ' Letzter Stand wiederhergestellt; Schreiben pausiert.'}`);
        return saved;
    }, [canWriteNow, loadActive, restoreGeometry]);

    const rememberGeometry = useCallback((features: Feature<Geometry>[]) => {
        geometryBeforeRef.current.clear();
        for (const feature of features) {
            const geometry = feature.getGeometry();
            if (geometry) geometryBeforeRef.current.set(feature, geometry.clone());
        }
        interactingRef.current = true;
        setIsInteracting(true);
        ++loadRequestRef.current;
    }, []);

    const persistGeometry = useCallback(async (features: Feature<Geometry>[]) => {
        const changed = features.filter(feature => {
            const before = geometryBeforeRef.current.get(feature);
            const after = feature.getGeometry();
            // Vergleich in Kartenprojektion: keine Rundungsänderung durch WGS84-Umrechnung.
            return before && after && geometryFormat.writeGeometry(before) !== geometryFormat.writeGeometry(after);
        });
        if (!changed.length) {
            restoreGeometry();
            if (refreshPendingRef.current) void loadActive();
            return;
        }
        await runMutation(async (context) => {
            for (const feature of changed) {
                const dto = feature.get('sourceElement') as MapElementDto;
                const geometryJson = writeGeometry(feature.getGeometry()!);
                const response = await upsertMapElement(context.incident.id, {
                    ...elementPayload(dto),
                    commandId: buildCommandId(),
                    sessionId,
                    leaseToken: context.lease.leaseToken,
                    geometryJson,
                });
                const saved = response.element as MapElementDto;
                // Eine HTTP-200-Antwort allein reicht nicht: Die API muss die neue
                // Geometrie zurückliefern, sonst darf die UI keinen Erfolg anzeigen.
                if (geometryFormat.writeGeometry(geometryFormat.readGeometry(saved.geometryJson)) !==
                    geometryFormat.writeGeometry(geometryFormat.readGeometry(geometryJson))) {
                    throw new Error('Der Server hat die neue Geometrie nicht übernommen.');
                }
                feature.set('sourceElement', saved);
                feature.set('version', saved.version);
                setElements(previous => previous.map(x => x.id === saved.id ? saved : x));
            }
        }, 'Position/Form gespeichert.', 'Änderung wurde nicht bestätigt und zurückgesetzt.', restoreGeometry);
    }, [loadActive, restoreGeometry, runMutation, sessionId]);

    const persistNewElement = useCallback(async (geometry: Geometry, elementType: 'Symbol' | 'Polygon') => {
        const symbol = symbolCatalog.find(x => x.id === stateRef.current.selectedSymbol)!;
        await runMutation(async (context) => {
            await upsertMapElement(context.incident.id, {
                commandId: buildCommandId(),
                sessionId,
                leaseToken: context.lease.leaseToken,
                elementType,
                symbolId: elementType === 'Symbol' ? symbol.id : undefined,
                category: elementType === 'Symbol' ? symbol.category : 'Fläche',
                label: elementType === 'Symbol' ? symbol.label : 'Fläche',
                colorHex: elementType === 'Symbol' ? '#d62828' : '#f77f00',
                geometryJson: writeGeometry(geometry),
            });
        }, 'Element gespeichert.', 'Element konnte nicht gespeichert werden.');
        setEditMode('move');
    }, [runMutation, sessionId]);

    useEffect(() => {
        if (!canView || !mapContainerRef.current) return;
        const osmLayer = new TileLayer({
            source: new OSM({
                attributions: ['© OpenStreetMap contributors'],
                url: import.meta.env.VITE_OSM_TILE_URL || 'https://tile.openstreetmap.org/{z}/{x}/{y}.png',
            }),
        });
        const hydrantLayer = new VectorLayer({
            source: hydrantSourceRef.current,
            style: new Style({ image: new CircleStyle({
                radius: 5, fill: new Fill({ color: '#00a8e8' }),
                stroke: new Stroke({ color: '#fff', width: 1 }),
            }) }),
        });
        const mapElementLayer = new VectorLayer({ source: vectorSourceRef.current });
        const map = new Map({
            target: mapContainerRef.current,
            layers: [osmLayer, hydrantLayer, mapElementLayer],
            view: new View({ center: fromLonLat([10.5, 51.2]), zoom: 7 }),
        });

        const select = new Select({
            condition: click,
            toggleCondition: never,
            layers: [mapElementLayer],
            hitTolerance: 8,
            style: null, // Hervorhebung wird zusammen mit den fachlichen Symbolstilen gesetzt.
        });
        select.on('select', () => setSelectedElementId(select.getFeatures().item(0)?.get('elementId') ?? null));

        const modify = new Modify({
            features: select.getFeatures(),
            condition: event => primaryAction(event) && canWriteNow() && !savingRef.current && stateRef.current.editMode === 'vertices',
        });
        modify.on('modifystart', event => rememberGeometry(event.features.getArray()));
        modify.on('modifyend', event => { void persistGeometry(event.features.getArray()); });

        // Layer statt Auswahlcollection: Ein Symbol/eine Fläche lässt sich direkt
        // greifen. Die Hydrantenebene bleibt ausdrücklich außerhalb dieser Interaktion.
        const translate = new Translate({
            layers: [mapElementLayer],
            filter: feature => Boolean(feature.get('elementId')),
            hitTolerance: 8,
            condition: event => primaryAction(event) && canWriteNow() && !savingRef.current && stateRef.current.editMode === 'move',
        });
        translate.on('translatestart', event => {
            const feature = event.features.item(0);
            select.getFeatures().clear();
            select.getFeatures().push(feature);
            setSelectedElementId(feature.get('elementId'));
            rememberGeometry(event.features.getArray());
        });
        translate.on('translateend', event => { void persistGeometry(event.features.getArray()); });

        // Ohne source: Eine neue Fläche wird erst nach erfolgreichem Speichern als
        // reguläres Feature übernommen. Abgelehnte Entwürfe bleiben nicht in der Karte.
        const draw = new Draw({
            type: 'Polygon',
            stopClick: true,
            condition: event => primaryAction(event) && canWriteNow() && !savingRef.current,
        });
        draw.on('drawstart', () => {
            interactingRef.current = true;
            setIsInteracting(true);
            ++loadRequestRef.current;
        });
        draw.on('drawabort', () => {
            interactingRef.current = false;
            setIsInteracting(false);
            if (refreshPendingRef.current) void loadActive();
        });
        draw.on('drawend', event => { void persistNewElement(event.feature.getGeometry()!, 'Polygon'); });

        modify.setActive(false);
        translate.setActive(false);
        draw.setActive(false);
        map.addInteraction(select);
        map.addInteraction(modify);
        map.addInteraction(translate);
        map.addInteraction(draw);
        mapRef.current = map;
        selectRef.current = select;
        modifyRef.current = modify;
        translateRef.current = translate;
        drawRef.current = draw;

        const clickListener = map.on('singleclick', event => {
            const current = stateRef.current;
            if (!current.incident) {
                if (current.canEdit) setSelectedCreationPosition(toLonLat(event.coordinate) as [number, number]);
                return;
            }
            if (current.editMode !== 'symbol' || !canWriteNow() || savingRef.current) return;
            // Bestehende Elemente anklicken darf niemals nebenbei ein Symbol erzeugen.
            if (map.hasFeatureAtPixel(event.pixel, { layerFilter: layer => layer === mapElementLayer, hitTolerance: 8 })) return;
            void persistNewElement(new Point(event.coordinate), 'Symbol');
        });
        const onKeyDown = (event: KeyboardEvent) => {
            if (event.key !== 'Escape') return;
            draw.abortDrawing();
            setEditMode('move');
        };
        document.addEventListener('keydown', onKeyDown);

        return () => {
            ++loadRequestRef.current;
            unByKey(clickListener);
            document.removeEventListener('keydown', onKeyDown);
            map.setTarget(undefined);
            map.dispose();
            mapRef.current = null;
            selectRef.current = null;
            modifyRef.current = null;
            translateRef.current = null;
            drawRef.current = null;
            centeredIncidentRef.current = null;
            geometryBeforeRef.current.clear();
            interactingRef.current = false;
        };
    }, [canView, canWriteNow, loadActive, persistGeometry, persistNewElement, rememberGeometry]);

    useEffect(() => {
        const enabled = canWrite && !isSaving;
        translateRef.current?.setActive(enabled && editMode === 'move');
        modifyRef.current?.setActive(enabled && editMode === 'vertices' && selectedElement?.elementType === 'Polygon');
        selectRef.current?.setActive(!isSaving && (editMode === 'move' || editMode === 'vertices' || !canWrite));
        if (!enabled || editMode !== 'polygon') drawRef.current?.abortDrawing();
        drawRef.current?.setActive(enabled && editMode === 'polygon');
        if (!canWrite && !savingRef.current) restoreGeometry();
    }, [canView, canWrite, editMode, isSaving, selectedElement?.elementType, restoreGeometry]);

    useEffect(() => {
        // Features nach ID abgleichen statt die Quelle zu leeren: Auswahl und
        // Interaktionen behalten dieselben Objektinstanzen und damit ihren Bezug.
        if (isBusy) return;
        const source = vectorSourceRef.current;
        const incoming = new Set(elements.map(x => x.id));
        source.getFeatures().forEach(feature => {
            if (!incident || !incoming.has(String(feature.getId()))) {
                selectRef.current?.getFeatures().remove(feature);
                source.removeFeature(feature);
            }
        });
        if (incident) {
            for (const dto of elements) {
                let feature = source.getFeatureById(dto.id) as Feature<Geometry> | null;
                if (!feature) {
                    feature = toFeature(dto);
                    source.addFeature(feature);
                } else {
                    const previous = feature.get('sourceElement') as MapElementDto;
                    if (previous.geometryJson !== dto.geometryJson) feature.setGeometry(toFeature(dto).getGeometry());
                    feature.setProperties({ ...toFeature(dto).getProperties(), geometry: feature.getGeometry() });
                }
                feature.setStyle(createFeatureStyle(feature, dto.id === selectedElementId));
            }
        }
        if (selectedElementId && !incoming.has(selectedElementId)) setSelectedElementId(null);
    }, [canView, elements, incident?.id, selectedElementId, isBusy]);

    useEffect(() => {
        if (!mapRef.current || !incident || centeredIncidentRef.current === incident.id) return;
        mapRef.current.getView().setCenter(fromLonLat([incident.originLongitude, incident.originLatitude]));
        mapRef.current.getView().setZoom(16);
        centeredIncidentRef.current = incident.id;
    }, [canView, incident]);

    useEffect(() => {
        hydrantSourceRef.current.clear();
        if (!showHydrants || !incident) return;
        hydrantSourceRef.current.addFeatures(hydrants.map(h => new Feature({
            geometry: new Point(fromLonLat([h.longitude, h.latitude])),
            isHydrant: true,
        })));
    }, [showHydrants, hydrants, incident?.id]);

    useEffect(() => { if (canView) void loadActive(); }, [canView, loadActive]);

    useEffect(() => {
        if (!incident || !canView) return;
        let disposed = false;
        let connection: Awaited<ReturnType<typeof ensureIncidentConnection>> | undefined;
        const refresh = () => { if (!disposed) void loadActive(); };
        const subscribe = async () => {
            const conn = await ensureIncidentConnection();
            if (disposed) return;
            connection = conn;
            conn.on('MapUpdated', refresh);
            conn.on('IncidentUpdated', refresh);
            await conn.invoke('SubscribeIncident', incident.id);
            if (!disposed && (hasRole('SituationMapViewer') || hasRole('SituationMapEditor'))) {
                await conn.invoke('SubscribeMap', incident.id);
            }
        };
        void subscribe().catch(() => { if (!disposed) setStatusText('Live-Verbindung konnte nicht hergestellt werden.'); });
        return () => {
            disposed = true;
            connection?.off('MapUpdated', refresh);
            connection?.off('IncidentUpdated', refresh);
        };
    }, [incident?.id, canView, hasRole, loadActive]);

    useEffect(() => {
        if (!incident || !lease || !isLeaseOwner || !canEdit || incident.status !== 'Active') return;
        let disposed = false;
        let pending = false;
        const timer = window.setInterval(async () => {
            if (pending) return;
            pending = true;
            try {
                const response = await heartbeatMapLease(incident.id, {
                    sessionId, leaseToken: lease.leaseToken, commandId: buildCommandId(),
                });
                if (!disposed) {
                    serverClockOffsetRef.current = Date.parse(response.serverUtc) - Date.now();
                    stateRef.current.lease = response.lease;
                    setLease(response.lease);
                    // Nach einem Netzfehler erst über einen frischen Snapshot wieder freigeben.
                    if (pausedRef.current) void loadActive();
                }
            } catch {
                if (!disposed) {
                    pauseWriting(true);
                    setStatusText('Heartbeat fehlgeschlagen. Schreibzugriff pausiert.');
                }
            } finally { pending = false; }
        }, 30000);
        return () => { disposed = true; window.clearInterval(timer); };
    }, [incident?.id, incident?.status, lease?.leaseToken, isLeaseOwner, canEdit, sessionId, loadActive, pauseWriting]);

    useEffect(() => {
        if (!lease || !isLeaseOwner) return;
        const delay = Date.parse(lease.expiresAtUtc) - Date.now() - serverClockOffsetRef.current;
        const timer = window.setTimeout(() => {
            pauseWriting(true);
            setStatusText('Bearbeitungssperre abgelaufen. Bitte Bearbeitung erneut übernehmen.');
        }, Math.max(0, delay));
        return () => window.clearTimeout(timer);
    }, [lease?.expiresAtUtc, isLeaseOwner, pauseWriting]);

    const filteredSymbols = useMemo(() => symbolCatalog.filter(symbol => {
        const matchesCategory = filterCategory === 'Alle' || symbol.category === filterCategory;
        const q = searchSymbol.trim().toLowerCase();
        return matchesCategory && (!q || symbol.label.toLowerCase().includes(q) || symbol.id.toLowerCase().includes(q));
    }), [filterCategory, searchSymbol]);
    const categories = useMemo(() => ['Alle', ...Array.from(new Set(symbolCatalog.map(x => x.category)))], []);

    if (!canView) return <div className="card"><h2>Lagekarte</h2><p>Keine Berechtigung.</p></div>;

    const createNewIncident = async () => {
        if (!canEdit || !selectedCreationPosition || !newIncidentName.trim()) {
            setStatusText('Bitte Einsatzname und Einsatzposition auf der Karte wählen.');
            return;
        }
        try {
            await createIncident({
                name: newIncidentName.trim(), incidentNumber: newIncidentNumber || undefined,
                keyword: newIncidentKeyword || undefined, description: newIncidentDescription || undefined,
                originLongitude: selectedCreationPosition[0], originLatitude: selectedCreationPosition[1],
                sessionId, commandId: buildCommandId(),
            });
            await loadActive();
        } catch (error: unknown) {
            setStatusText(getErrorTitle(error, 'Einsatz konnte nicht angelegt werden.'));
        }
    };

    const acquireLease = async () => {
        if (!incident || isBusy) return;
        try {
            await acquireMapLease(incident.id, { sessionId, commandId: buildCommandId() });
            await loadActive();
        } catch (error: unknown) {
            setStatusText(getErrorTitle(error, 'Sperre konnte nicht übernommen werden.'));
        }
    };
    const releaseLease = async () => {
        if (!incident || !lease || !isLeaseOwner || isBusy) return;
        pauseWriting(true);
        try {
            await releaseMapLease(incident.id, { sessionId, leaseToken: lease.leaseToken, commandId: buildCommandId() });
            stateRef.current.lease = null;
            setLease(null);
            setEditMode('move');
            await loadActive();
        } catch { setStatusText('Sperre konnte nicht freigegeben werden. Bitte aktualisieren.'); }
    };
    const closeActiveIncident = async () => {
        if (isBusy || !canWriteNow() || !confirm('Einsatz wirklich abschließen? Dieser Schritt ist endgültig.')) return;
        await runMutation(async context => {
            await closeIncident(context.incident.id, { sessionId, leaseToken: context.lease.leaseToken, commandId: buildCommandId() });
        }, 'Einsatz abgeschlossen.', 'Einsatz konnte nicht abgeschlossen werden.');
    };
    const createPolygonInteraction = () => {
        if (canWriteNow() && !isBusy) setEditMode('polygon');
    };
    const saveSelectedProperties = async () => {
        if (!selectedElement || isBusy) return;
        await runMutation(async context => {
            await upsertMapElement(context.incident.id, {
                ...elementPayload(selectedElement), geometryJson: selectedElement.geometryJson,
                commandId: buildCommandId(), sessionId, leaseToken: context.lease.leaseToken,
            });
        }, 'Eigenschaften gespeichert.', 'Eigenschaften konnten nicht gespeichert werden.');
    };
    const removeSelectedElement = async () => {
        if (!selectedElement || isBusy) return;
        await runMutation(async context => {
            await deleteMapElement(context.incident.id, selectedElement.id, {
                commandId: buildCommandId(), sessionId, leaseToken: context.lease.leaseToken,
                expectedVersion: selectedElement.version,
            });
            setSelectedElementId(null);
        }, 'Element gelöscht.', 'Element konnte nicht gelöscht werden.');
    };
    return (
        <div className="incident-page">
            <section className="card incident-top">
                <h2>Lagekarte</h2>
                <p role="status">{isSaving ? 'Änderung wird gespeichert …' : statusText}</p>
                {!incident && canEdit && (
                    <div className="incident-create-grid">
                        <input value={newIncidentName} onChange={(e) => setNewIncidentName(e.target.value)} placeholder="Einsatzname*" />
                        <input value={newIncidentNumber} onChange={(e) => setNewIncidentNumber(e.target.value)} placeholder="Einsatznummer" />
                        <input value={newIncidentKeyword} onChange={(e) => setNewIncidentKeyword(e.target.value)} placeholder="Stichwort" />
                        <input value={newIncidentDescription} onChange={(e) => setNewIncidentDescription(e.target.value)} placeholder="Beschreibung" />
                        <div className="small-note">Position auf Karte klicken: {selectedCreationPosition ? `${selectedCreationPosition[1].toFixed(5)}, ${selectedCreationPosition[0].toFixed(5)}` : 'noch nicht gewählt'}</div>
                        <button type="button" onClick={createNewIncident}>Einsatz anlegen</button>
                    </div>
                )}

                {incident && (
                    <div className="incident-meta-line">
                        <strong>{incident.name}</strong>
                        <span>Status: {incident.status === 'Closed' ? 'Abgeschlossen' : 'Aktiv'}</span>
                        <span>Koordinate: {incident.originLatitude.toFixed(5)}, {incident.originLongitude.toFixed(5)}</span>
                    </div>
                )}

                {incident && canEdit && (
                    <div className="incident-actions">
                        <button type="button" onClick={acquireLease} disabled={isBusy || incident.status !== 'Active' || (!!lease && !isLeaseOwner && !lease.isExpired)}>Bearbeitung übernehmen</button>
                        <button type="button" onClick={releaseLease} disabled={isBusy || !lease || !isLeaseOwner}>Bearbeitung freigeben</button>
                        <button type="button" aria-pressed={editMode === 'move'} onClick={() => setEditMode('move')} disabled={!canWrite || isBusy}>Verschieben / Auswählen</button>
                        <button type="button" aria-pressed={editMode === 'vertices'} onClick={() => setEditMode('vertices')} disabled={!canWrite || isBusy || selectedElement?.elementType !== 'Polygon'}>Eckpunkte bearbeiten</button>
                        <button type="button" aria-pressed={editMode === 'polygon'} onClick={createPolygonInteraction} disabled={!canWrite || isBusy}>Fläche zeichnen</button>
                        <button type="button" onClick={closeActiveIncident} disabled={!canWrite || isBusy}>Einsatz abschließen</button>
                        <button type="button" onClick={() => { void loadActive(); }} disabled={isBusy}>Aktualisieren</button>
                        {editMode !== 'move' && <button type="button" onClick={() => { drawRef.current?.abortDrawing(); setEditMode('move'); }} disabled={isSaving}>Abbrechen (Esc)</button>}
                        <a href={incident ? getMapAuditExportUrl(incident.id) : '#'} target="_blank" rel="noreferrer">CSV-Export</a>
                        <a href={incident ? getMapPdfExportUrl(incident.id) : '#'} target="_blank" rel="noreferrer">PDF-Export</a>
                    </div>
                )}
            </section>

            <section className="incident-layout">
                <aside className="card incident-sidebar">
                    <h3>Symbolkatalog</h3>
                    <input placeholder="Suche" value={searchSymbol} onChange={(e) => setSearchSymbol(e.target.value)} />
                    <select value={filterCategory} onChange={(e) => setFilterCategory(e.target.value)}>
                        {categories.map(c => <option key={c} value={c}>{c}</option>)}
                    </select>
                    <div className="symbol-grid">
                        {filteredSymbols.map((symbol) => (
                            <button key={symbol.id} type="button" className={editMode === 'symbol' && selectedSymbol === symbol.id ? 'active' : ''} aria-pressed={editMode === 'symbol' && selectedSymbol === symbol.id} disabled={!canWrite || isBusy} onClick={() => { setSelectedSymbol(symbol.id); setEditMode('symbol'); }}>
                                <span>{symbol.icon}</span>
                                <small>{symbol.label}</small>
                            </button>
                        ))}
                    </div>

                    <label className="checkbox-row">
                        <input type="checkbox" checked={showHydrants} onChange={(e) => setShowHydrants(e.target.checked)} />
                        Hydrantenebene anzeigen
                    </label>

                    {selectedElement && (
                        <fieldset className="selected-editor" disabled={!canWrite || isBusy} style={{ border: 0, margin: 0, padding: 0 }}>
                            <h4>Elementeigenschaften</h4>
                            <input value={selectedElement.label ?? ''} onChange={(e) => setElements(prev => prev.map(x => x.id === selectedElement.id ? { ...x, label: e.target.value } : x))} placeholder="Beschriftung" />
                            <input value={selectedElement.radioCallName ?? ''} onChange={(e) => setElements(prev => prev.map(x => x.id === selectedElement.id ? { ...x, radioCallName: e.target.value } : x))} placeholder="Funkrufname" />
                            <input value={selectedElement.strength ?? ''} onChange={(e) => setElements(prev => prev.map(x => x.id === selectedElement.id ? { ...x, strength: e.target.value } : x))} placeholder="Stärke" />
                            <textarea value={selectedElement.note ?? ''} onChange={(e) => setElements(prev => prev.map(x => x.id === selectedElement.id ? { ...x, note: e.target.value } : x))} placeholder="Bemerkung" />
                            <input type="color" value={selectedElement.colorHex ?? '#d62828'} onChange={(e) => setElements(prev => prev.map(x => x.id === selectedElement.id ? { ...x, colorHex: e.target.value } : x))} />
                            <div className="incident-actions">
                                <button type="button" onClick={saveSelectedProperties} disabled={!canWrite || isBusy}>Speichern</button>
                                <button type="button" onClick={removeSelectedElement} disabled={!canWrite || isBusy}>Löschen</button>
                            </div>
                        </fieldset>
                    )}
                </aside>

                <div className="card incident-map-container">
                    <p className="small-note">
                        {!canWrite ? 'Leseansicht: Karte verschieben und zoomen; Elemente anklicken.'
                            : editMode === 'symbol' ? 'Symbol platzieren: auf eine freie Kartenposition klicken. Danach ist Verschieben wieder aktiv.'
                            : editMode === 'polygon' ? 'Fläche zeichnen: Eckpunkte anklicken, mit Doppelklick abschließen; Esc bricht ab.'
                            : editMode === 'vertices' ? 'Eckpunkte der ausgewählten Fläche ziehen. Zum Bewegen der ganzen Fläche auf Verschieben wechseln.'
                            : 'Symbole oder Flächen direkt mit der Maus ziehen. Die Änderung wird beim Loslassen gespeichert.'}
                    </p>
                    <div ref={mapContainerRef} className="incident-map" tabIndex={0} aria-label="Lagekarte: Symbole und Flächen auswählen und verschieben" />
                    <p className="small-note">Hintergrundkarte: © OpenStreetMap contributors</p>
                </div>
            </section>

            <section className="card">
                <h3>Lagekarten-Änderungslog</h3>
                <div className="log-table-wrap">
                    <table className="log-table">
                        <thead>
                            <tr>
                                <th>#</th>
                                <th>Zeit (UTC)</th>
                                <th>Aktion</th>
                                <th>Benutzer</th>
                                <th>Element</th>
                            </tr>
                        </thead>
                        <tbody>
                            {auditRows.map((row) => (
                                <tr key={row.id}>
                                    <td>{row.sequenceNumber}</td>
                                    <td>{new Date(row.occurredAtUtc).toISOString()}</td>
                                    <td>{row.action}</td>
                                    <td>{row.userDisplayName}</td>
                                    <td>{row.elementId ?? '-'}</td>
                                </tr>
                            ))}
                            {auditRows.length === 0 && (
                                <tr><td colSpan={5}>Keine Einträge</td></tr>
                            )}
                        </tbody>
                    </table>
                </div>
            </section>
        </div>
    );
}

export default IncidentMapPage;