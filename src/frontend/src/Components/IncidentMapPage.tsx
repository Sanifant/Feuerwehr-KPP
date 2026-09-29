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
import { click } from 'ol/events/condition';
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
import type { MapElementDto } from '../api/incidentApi';
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

    feature.setStyle(createFeatureStyle(feature));
    return feature;
}

function createFeatureStyle(feature: FeatureLike) {
    const label = `${feature.get('symbolId') ? `${feature.get('symbolId')} ` : ''}${feature.get('label') ?? ''}`.trim();
    const color = feature.get('colorHex') || '#d62828';

    if (feature.getGeometry() instanceof Point) {
        return new Style({
            image: new CircleStyle({
                radius: 8,
                fill: new Fill({ color }),
                stroke: new Stroke({ color: '#fff', width: 2 }),
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
        stroke: new Stroke({ color, width: 2 }),
        text: new Text({
            text: label,
            fill: new Fill({ color: '#111' }),
            stroke: new Stroke({ color: '#fff', width: 2 }),
        }),
    });
}

export function IncidentMapPage() {
    const { hasRole } = useAuth();
    const { canEditSituationMap, canViewSituationMap } = useRole();
    const mapRef = useRef<Map | null>(null);
    const mapContainerRef = useRef<HTMLDivElement>(null);
    const vectorSourceRef = useRef(new VectorSource());
    const hydrantSourceRef = useRef(new VectorSource());
    const drawRef = useRef<Draw | null>(null);
    const modifyRef = useRef<Modify | null>(null);
    const translateRef = useRef<Translate | null>(null);
    const selectRef = useRef<Select | null>(null);
    const heartbeatRef = useRef<number | null>(null);

    const [incident, setIncident] = useState<any | null>(null);
    const [elements, setElements] = useState<MapElementDto[]>([]);
    const [auditRows, setAuditRows] = useState<any[]>([]);
    const [lease, setLease] = useState<any | null>(null);
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

    const sessionId = useMemo(() => getSessionId(), []);

    const canView = canViewSituationMap;
    const canEdit = canEditSituationMap;
    const isLeaseOwner = Boolean(lease && lease.sessionId === sessionId);

    const loadActive = useCallback(async () => {
        try {
            const active = await getActiveIncident();
            setIncident(active);
            const snapshot = await getMapState(active.id);
            setElements(snapshot.elements);
            setLease(snapshot.lease);
            setStatusText(snapshot.lease ? `Sperre: ${snapshot.lease.userDisplayName} bis ${new Date(snapshot.lease.expiresAtUtc).toLocaleTimeString('de-DE')}` : 'Keine aktive Bearbeitungssperre.');
            const logs = await getMapAudit(active.id);
            setAuditRows(logs);
        } catch {
            setIncident(null);
            setElements([]);
            setLease(null);
            setAuditRows([]);
            setStatusText('Kein aktiver Einsatz vorhanden.');
        }
    }, []);

    useEffect(() => {
        if (!canView || !mapContainerRef.current) {
            return;
        }

        const osmLayer = new TileLayer({
            source: new OSM({
                attributions: ['© OpenStreetMap contributors'],
                url: import.meta.env.VITE_OSM_TILE_URL || 'https://tile.openstreetmap.org/{z}/{x}/{y}.png',
            }),
        });

        const hydrantLayer = new VectorLayer({
            source: hydrantSourceRef.current,
            style: new Style({
                image: new CircleStyle({ radius: 5, fill: new Fill({ color: '#00a8e8' }), stroke: new Stroke({ color: '#fff', width: 1 }) }),
            }),
        });

        const mapElementLayer = new VectorLayer({
            source: vectorSourceRef.current,
        });

        const map = new Map({
            target: mapContainerRef.current,
            layers: [osmLayer, hydrantLayer, mapElementLayer],
            view: new View({ center: fromLonLat([10.5, 51.2]), zoom: 7 }),
        });

        const selectInteraction = new Select({ condition: click, layers: [mapElementLayer] });
        selectInteraction.on('select', (event) => {
            const selected = event.selected[0];
            const elementId = selected?.get('elementId');
            setSelectedElementId(elementId ?? null);
        });

        const modifyInteraction = new Modify({ source: vectorSourceRef.current });
        modifyInteraction.on('modifyend', async (event) => {
            if (!incident || !lease || !canEdit) {
                return;
            }

            for (const feature of event.features.getArray()) {
                const dto = feature.get('sourceElement') as MapElementDto;
                const geometryJson = new GeoJSON().writeGeometry(feature.getGeometry()!, {
                    dataProjection: 'EPSG:4326',
                    featureProjection: 'EPSG:3857',
                });

                try {
                    await upsertMapElement(incident.id, {
                        commandId: buildCommandId(),
                        sessionId,
                        leaseToken: lease.leaseToken,
                        elementId: dto.id,
                        elementType: dto.elementType,
                        symbolId: dto.symbolId,
                        category: dto.category,
                        label: dto.label,
                        radioCallName: dto.radioCallName,
                        strength: dto.strength,
                        note: dto.note,
                        colorHex: dto.colorHex,
                        expectedVersion: dto.version,
                        geometryJson,
                    });
                } catch {
                    setStatusText('Änderung wurde vom Server abgelehnt. Zustand wird neu geladen.');
                    await loadActive();
                }
            }
        });

        const translateInteraction = new Translate({ features: selectInteraction.getFeatures() });
        translateInteraction.on('translateend', async (event) => {
            if (!incident || !lease || !canEdit) {
                return;
            }

            const feature = event.features.item(0);
            if (!feature) {
                return;
            }

            const dto = feature.get('sourceElement') as MapElementDto;
            const geometryJson = new GeoJSON().writeGeometry(feature.getGeometry()!, {
                dataProjection: 'EPSG:4326',
                featureProjection: 'EPSG:3857',
            });

            try {
                await upsertMapElement(incident.id, {
                    commandId: buildCommandId(),
                    sessionId,
                    leaseToken: lease.leaseToken,
                    elementId: dto.id,
                    elementType: dto.elementType,
                    symbolId: dto.symbolId,
                    category: dto.category,
                    label: dto.label,
                    radioCallName: dto.radioCallName,
                    strength: dto.strength,
                    note: dto.note,
                    colorHex: dto.colorHex,
                    expectedVersion: dto.version,
                    geometryJson,
                });
            } catch {
                setStatusText('Verschiebung wurde abgelehnt. Zustand wird neu geladen.');
                await loadActive();
            }
        });

        map.addInteraction(selectInteraction);
        map.addInteraction(modifyInteraction);
        map.addInteraction(translateInteraction);

        mapRef.current = map;
        selectRef.current = selectInteraction;
        modifyRef.current = modifyInteraction;
        translateRef.current = translateInteraction;

        const clickListener = map.on('singleclick', async (event) => {
            if (!incident) {
                const coords = toLonLat(event.coordinate) as [number, number];
                setSelectedCreationPosition(coords);
                return;
            }

            if (!canEdit || !lease || lease.sessionId !== sessionId) {
                return;
            }

            const selected = symbolCatalog.find(s => s.id === selectedSymbol);
            if (!selected) {
                return;
            }

            const geometryJson = JSON.stringify({ type: 'Point', coordinates: toLonLat(event.coordinate) });
            try {
                await upsertMapElement(incident.id, {
                    commandId: buildCommandId(),
                    sessionId,
                    leaseToken: lease.leaseToken,
                    elementType: 'Symbol',
                    symbolId: selected.id,
                    category: selected.category,
                    label: selected.label,
                    colorHex: '#d62828',
                    geometryJson,
                });
            } catch {
                setStatusText('Symbol konnte nicht gespeichert werden.');
            }
        });

        return () => {
            map.un('singleclick', clickListener.listener);
            map.setTarget(undefined);
            map.dispose();
            mapRef.current = null;
            selectRef.current = null;
            modifyRef.current = null;
            translateRef.current = null;
        };
    }, [canView, canEdit, incident, lease, loadActive, selectedSymbol, sessionId]);

    useEffect(() => {
        if (!incident) {
            vectorSourceRef.current.clear();
            return;
        }

        vectorSourceRef.current.clear();
        elements.forEach((x) => vectorSourceRef.current.addFeature(toFeature(x)));
    }, [elements, incident]);

    useEffect(() => {
        if (!incident) {
            return;
        }

        hydrantSourceRef.current.clear();

        if (!showHydrants) {
            return;
        }

        getMapState(incident.id).then((snapshot) => {
            snapshot.hydrants.forEach((h) => {
                const feature = new Feature({
                    geometry: new Point(fromLonLat([h.longitude, h.latitude])),
                    isHydrant: true,
                });
                feature.setStyle(new Style({ image: new CircleStyle({ radius: 4, fill: new Fill({ color: '#00a8e8' }), stroke: new Stroke({ color: '#fff', width: 1 }) }) }));
                hydrantSourceRef.current.addFeature(feature);
            });
        }).catch(() => {});
    }, [showHydrants, incident]);

    useEffect(() => {
        if (!canView) {
            return;
        }

        loadActive();
    }, [canView, loadActive]);

    useEffect(() => {
        if (!incident || !canView) {
            return;
        }

        let disposed = false;
        let localConnection: any;
        let mapUpdatedHandler: (() => Promise<void>) | null = null;
        let incidentUpdatedHandler: (() => Promise<void>) | null = null;

        const subscribe = async () => {
            const conn = await ensureIncidentConnection();
            localConnection = conn;

            await conn.invoke('SubscribeIncident', incident.id);
            if (hasRole('SituationMapViewer') || hasRole('SituationMapEditor')) {
                await conn.invoke('SubscribeMap', incident.id);
            }

            mapUpdatedHandler = async () => {
                if (!disposed) {
                    await loadActive();
                }
            };
            incidentUpdatedHandler = async () => {
                if (!disposed) {
                    await loadActive();
                }
            };

            conn.on('MapUpdated', mapUpdatedHandler);
            conn.on('IncidentUpdated', incidentUpdatedHandler);
        };

        subscribe().catch(() => setStatusText('Live-Verbindung konnte nicht hergestellt werden.'));

        return () => {
            disposed = true;
            if (localConnection) {
                if (mapUpdatedHandler) {
                    localConnection.off('MapUpdated', mapUpdatedHandler);
                }
                if (incidentUpdatedHandler) {
                    localConnection.off('IncidentUpdated', incidentUpdatedHandler);
                }
            }
        };
    }, [incident?.id, canView, hasRole, loadActive]);

    useEffect(() => {
        if (heartbeatRef.current) {
            window.clearInterval(heartbeatRef.current);
            heartbeatRef.current = null;
        }

        if (!incident || !lease || lease.sessionId !== sessionId || !canEdit) {
            return;
        }

        heartbeatRef.current = window.setInterval(async () => {
            try {
                await heartbeatMapLease(incident.id, {
                    sessionId,
                    leaseToken: lease.leaseToken,
                    commandId: buildCommandId(),
                });
            } catch {
                setStatusText('Heartbeat fehlgeschlagen. Schreibzugriff pausiert.');
            }
        }, 30000);

        return () => {
            if (heartbeatRef.current) {
                window.clearInterval(heartbeatRef.current);
                heartbeatRef.current = null;
            }
        };
    }, [incident, lease, canEdit, sessionId]);

    const filteredSymbols = useMemo(() => symbolCatalog.filter((symbol) => {
        const matchesCategory = filterCategory === 'Alle' || symbol.category === filterCategory;
        const q = searchSymbol.trim().toLowerCase();
        const matchesSearch = q.length === 0 || symbol.label.toLowerCase().includes(q) || symbol.id.toLowerCase().includes(q);
        return matchesCategory && matchesSearch;
    }), [filterCategory, searchSymbol]);

    const categories = useMemo(() => ['Alle', ...Array.from(new Set(symbolCatalog.map(x => x.category)))], []);

    if (!canView) {
        return <div className="card"><h2>Lagekarte</h2><p>Keine Berechtigung.</p></div>;
    }

    const createNewIncident = async () => {
        if (!selectedCreationPosition || !newIncidentName.trim()) {
            setStatusText('Bitte Einsatzname und Einsatzposition auf der Karte wählen.');
            return;
        }

        try {
            await createIncident({
                name: newIncidentName.trim(),
                incidentNumber: newIncidentNumber || undefined,
                keyword: newIncidentKeyword || undefined,
                description: newIncidentDescription || undefined,
                originLongitude: selectedCreationPosition[0],
                originLatitude: selectedCreationPosition[1],
                sessionId,
                commandId: buildCommandId(),
            });
            await loadActive();
            setStatusText('Einsatz wurde angelegt und Sperre übernommen.');
        } catch (error: any) {
            setStatusText(error?.response?.data?.title ?? 'Einsatz konnte nicht angelegt werden.');
        }
    };

    const acquireLease = async () => {
        if (!incident) {
            return;
        }
        try {
            const response = await acquireMapLease(incident.id, { sessionId, commandId: buildCommandId() });
            setLease(response.lease);
            setStatusText('Bearbeitungssperre übernommen.');
            await loadActive();
        } catch (error: any) {
            setStatusText(error?.response?.data?.title ?? 'Sperre konnte nicht übernommen werden.');
        }
    };

    const releaseLease = async () => {
        if (!incident || !lease) {
            return;
        }
        try {
            await releaseMapLease(incident.id, { sessionId, leaseToken: lease.leaseToken, commandId: buildCommandId() });
            setLease(null);
            setStatusText('Bearbeitungssperre freigegeben.');
            await loadActive();
        } catch {
            setStatusText('Sperre konnte nicht freigegeben werden.');
        }
    };

    const closeActiveIncident = async () => {
        if (!incident || !lease || !confirm('Einsatz wirklich abschließen? Dieser Schritt ist endgültig.')) {
            return;
        }

        try {
            await closeIncident(incident.id, { sessionId, leaseToken: lease.leaseToken, commandId: buildCommandId() });
            setStatusText('Einsatz abgeschlossen.');
            await loadActive();
        } catch {
            setStatusText('Einsatz konnte nicht abgeschlossen werden.');
        }
    };

    const createPolygonInteraction = () => {
        if (!mapRef.current) {
            return;
        }

        if (drawRef.current) {
            mapRef.current.removeInteraction(drawRef.current);
            drawRef.current = null;
        }

        const draw = new Draw({ source: vectorSourceRef.current, type: 'Polygon' });
        draw.on('drawend', async (event) => {
            if (!incident || !lease || !canEdit) {
                return;
            }

            const geometryJson = new GeoJSON().writeGeometry(event.feature.getGeometry()!, {
                dataProjection: 'EPSG:4326',
                featureProjection: 'EPSG:3857',
            });

            try {
                await upsertMapElement(incident.id, {
                    commandId: buildCommandId(),
                    sessionId,
                    leaseToken: lease.leaseToken,
                    elementType: 'Polygon',
                    category: 'Fläche',
                    label: 'Fläche',
                    colorHex: '#f77f00',
                    geometryJson,
                });
                await loadActive();
            } catch {
                setStatusText('Fläche konnte nicht gespeichert werden.');
            }
        });

        drawRef.current = draw;
        mapRef.current.addInteraction(draw);
    };

    const selectedElement = elements.find(x => x.id === selectedElementId) ?? null;

    const saveSelectedProperties = async () => {
        if (!incident || !selectedElement || !lease || !canEdit) {
            return;
        }

        try {
            await upsertMapElement(incident.id, {
                commandId: buildCommandId(),
                sessionId,
                leaseToken: lease.leaseToken,
                elementId: selectedElement.id,
                elementType: selectedElement.elementType,
                symbolId: selectedElement.symbolId,
                category: selectedElement.category,
                label: selectedElement.label,
                radioCallName: selectedElement.radioCallName,
                strength: selectedElement.strength,
                note: selectedElement.note,
                colorHex: selectedElement.colorHex,
                geometryJson: selectedElement.geometryJson,
                expectedVersion: selectedElement.version,
            });
            await loadActive();
        } catch {
            setStatusText('Eigenschaften konnten nicht gespeichert werden.');
        }
    };

    const removeSelectedElement = async () => {
        if (!incident || !selectedElement || !lease || !canEdit) {
            return;
        }

        try {
            await deleteMapElement(incident.id, selectedElement.id, {
                commandId: buildCommandId(),
                sessionId,
                leaseToken: lease.leaseToken,
                expectedVersion: selectedElement.version,
            });
            setSelectedElementId(null);
            await loadActive();
        } catch {
            setStatusText('Element konnte nicht gelöscht werden.');
        }
    };

    return (
        <div className="incident-page">
            <section className="card incident-top">
                <h2>Lagekarte</h2>
                <p>{statusText}</p>
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
                        <button type="button" onClick={acquireLease} disabled={!!lease && !isLeaseOwner && !lease.isExpired}>Bearbeitung übernehmen</button>
                        <button type="button" onClick={releaseLease} disabled={!lease || !isLeaseOwner}>Bearbeitung freigeben</button>
                        <button type="button" onClick={createPolygonInteraction} disabled={!lease || !isLeaseOwner}>Fläche zeichnen</button>
                        <button type="button" onClick={closeActiveIncident} disabled={!lease || !isLeaseOwner}>Einsatz abschließen</button>
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
                            <button key={symbol.id} type="button" className={selectedSymbol === symbol.id ? 'active' : ''} onClick={() => setSelectedSymbol(symbol.id)}>
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
                        <div className="selected-editor">
                            <h4>Elementeigenschaften</h4>
                            <input value={selectedElement.label ?? ''} onChange={(e) => setElements(prev => prev.map(x => x.id === selectedElement.id ? { ...x, label: e.target.value } : x))} placeholder="Beschriftung" />
                            <input value={selectedElement.radioCallName ?? ''} onChange={(e) => setElements(prev => prev.map(x => x.id === selectedElement.id ? { ...x, radioCallName: e.target.value } : x))} placeholder="Funkrufname" />
                            <input value={selectedElement.strength ?? ''} onChange={(e) => setElements(prev => prev.map(x => x.id === selectedElement.id ? { ...x, strength: e.target.value } : x))} placeholder="Stärke" />
                            <textarea value={selectedElement.note ?? ''} onChange={(e) => setElements(prev => prev.map(x => x.id === selectedElement.id ? { ...x, note: e.target.value } : x))} placeholder="Bemerkung" />
                            <input type="color" value={selectedElement.colorHex ?? '#d62828'} onChange={(e) => setElements(prev => prev.map(x => x.id === selectedElement.id ? { ...x, colorHex: e.target.value } : x))} />
                            <div className="incident-actions">
                                <button type="button" onClick={saveSelectedProperties} disabled={!lease || !isLeaseOwner}>Speichern</button>
                                <button type="button" onClick={removeSelectedElement} disabled={!lease || !isLeaseOwner}>Löschen</button>
                            </div>
                        </div>
                    )}
                </aside>

                <div className="card incident-map-container">
                    <div ref={mapContainerRef} className="incident-map" />
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
                            {auditRows.map((row: any) => (
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
