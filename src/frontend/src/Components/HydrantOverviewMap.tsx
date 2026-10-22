import { useEffect, useRef } from 'react';
import Feature from 'ol/Feature';
import Map from 'ol/Map';
import View from 'ol/View';
import Point from 'ol/geom/Point';
import TileLayer from 'ol/layer/Tile';
import VectorLayer from 'ol/layer/Vector';
import OSM from 'ol/source/OSM';
import VectorSource from 'ol/source/Vector';
import { fromLonLat } from 'ol/proj';
import { Circle as CircleStyle, Fill, Stroke, Style } from 'ol/style';
import type { Hydrant } from './HydrantManager';
import { hasMappableCoordinates } from './hydrantMapCoordinates';
import 'ol/ol.css';

interface HydrantOverviewMapProps {
    hydrants: Hydrant[];
    selectedHydrantId: number | null;
    onSelectHydrant: (id: number) => void;
}

const markerStyle = new Style({ image: new CircleStyle({
    radius: 6,
    fill: new Fill({ color: '#2563eb' }),
    stroke: new Stroke({ color: '#fff', width: 2 }),
}) });
const selectedMarkerStyle = new Style({
    image: new CircleStyle({
        radius: 10,
        fill: new Fill({ color: '#1d4ed8' }),
        stroke: new Stroke({ color: '#facc15', width: 3 }),
    }),
    zIndex: 1,
});

export default function HydrantOverviewMap({ hydrants, selectedHydrantId, onSelectHydrant }: HydrantOverviewMapProps) {
    const containerRef = useRef<HTMLDivElement>(null);
    const mapRef = useRef<Map | null>(null);
    const sourceRef = useRef<VectorSource<Feature<Point>> | null>(null);
    const hasFittedRef = useRef(false);
    const selectionRef = useRef({ selectedHydrantId, onSelectHydrant });
    const unmappableCount = hydrants.filter(h => !hasMappableCoordinates(h.latitude, h.longitude)).length;

    useEffect(() => {
        selectionRef.current = { selectedHydrantId, onSelectHydrant };
    }, [selectedHydrantId, onSelectHydrant]);

    useEffect(() => {
        const container = containerRef.current;
        if (!container) return;

        const source = new VectorSource<Feature<Point>>({ wrapX: false });
        const hydrantLayer = new VectorLayer({ source, style: markerStyle });
        const map = new Map({
            target: container,
            layers: [new TileLayer({ source: new OSM({
                url: import.meta.env.VITE_OSM_TILE_URL || 'https://tile.openstreetmap.org/{z}/{x}/{y}.png',
            }) }), hydrantLayer],
            view: new View({ center: fromLonLat([10.5, 51.2]), zoom: 6 }),
        });
        const hitOptions = { layerFilter: (layer: unknown) => layer === hydrantLayer, hitTolerance: 8 };
        map.on('singleclick', event => {
            const ids = [...new Set(map.getFeaturesAtPixel(event.pixel, hitOptions)
                .map(feature => feature.get('hydrantId') as number))].sort((a, b) => a - b);
            if (!ids.length) return;
            // Repeated clicks cycle through hydrants whose markers overlap.
            const current = ids.indexOf(selectionRef.current.selectedHydrantId ?? -1);
            selectionRef.current.onSelectHydrant(ids[(current + 1) % ids.length]);
        });
        map.on('pointermove', event => {
            if (!event.dragging) container.style.cursor = map.hasFeatureAtPixel(event.pixel, hitOptions) ? 'pointer' : '';
        });
        mapRef.current = map;
        sourceRef.current = source;
        const resizeObserver = typeof ResizeObserver !== 'undefined' ? new ResizeObserver(() => map.updateSize()) : null;
        resizeObserver?.observe(container);
        const frame = requestAnimationFrame(() => map.updateSize());

        return () => {
            cancelAnimationFrame(frame);
            resizeObserver?.disconnect();
            map.setTarget(undefined);
            map.dispose();
            mapRef.current = null;
            sourceRef.current = null;
            hasFittedRef.current = false;
        };
    }, []);

    useEffect(() => {
        const source = sourceRef.current;
        const map = mapRef.current;
        if (!source || !map) return;
        source.clear();
        source.addFeatures(hydrants.filter(h => hasMappableCoordinates(h.latitude, h.longitude)).map(h => {
            const feature = new Feature({ geometry: new Point(fromLonLat([h.longitude, h.latitude])), hydrantId: h.id });
            feature.setId(h.id);
            return feature;
        }));
        const extent = source.getExtent();
        if (!hasFittedRef.current && source.getFeatures().length && extent) {
            map.getView().fit(extent, { size: map.getSize(), padding: [40, 40, 40, 40], maxZoom: 17 });
            hasFittedRef.current = true;
        }
    }, [hydrants]);

    useEffect(() => {
        const source = sourceRef.current;
        const map = mapRef.current;
        if (!source || !map) return;
        source.getFeatures().forEach(feature => feature.setStyle(feature.getId() === selectedHydrantId ? selectedMarkerStyle : undefined));
        const selected = selectedHydrantId === null ? null : source.getFeatureById(selectedHydrantId);
        const geometry = selected?.getGeometry();
        if (geometry) {
            map.getView().setCenter(geometry.getCoordinates());
            map.getView().setZoom(Math.max(map.getView().getZoom() ?? 6, 16));
        }
    }, [hydrants, selectedHydrantId]);

    return (
        <section className="hydrant-overview" aria-labelledby="hydrant-overview-title">
            <h2 id="hydrant-overview-title">Hydrantenkarte</h2>
            <p id="hydrant-overview-hint">Blaue Punkte markieren die Hydranten. Ein Klick öffnet den Eintrag in der Liste.
                Bei überlagerten Punkten erneut klicken, um den nächsten Hydranten auszuwählen.</p>
            <div ref={containerRef} className="hydrant-overview-map" role="region" aria-label="Karte der Hydranten"
                aria-describedby="hydrant-overview-hint" tabIndex={0} />
            {unmappableCount > 0 && <p className="hydrant-map-warning">{unmappableCount} Hydranten können wegen ihrer Koordinaten
                nicht auf der Karte dargestellt werden. Sie sind weiterhin in der Liste verfügbar.</p>}
        </section>
    );
}
