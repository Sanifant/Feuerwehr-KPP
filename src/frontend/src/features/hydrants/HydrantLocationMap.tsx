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
import 'ol/ol.css';

interface HydrantLocationMapProps {
    latitude: number | '';
    longitude: number | '';
}

export default function HydrantLocationMap({ latitude, longitude }: HydrantLocationMapProps) {
    const containerRef = useRef<HTMLDivElement>(null);
    const mapRef = useRef<Map | null>(null);
    const sourceRef = useRef<VectorSource<Feature<Point>> | null>(null);
    const hasCoordinates = latitude !== '' && longitude !== '';
    const validCoordinates = hasCoordinates && Number.isFinite(latitude) && Number.isFinite(longitude)
        && latitude >= -90 && latitude <= 90 && longitude >= -180 && longitude <= 180;
    const visibleCoordinates = validCoordinates && Math.abs(latitude) <= 85.0511287798066;

    useEffect(() => {
        if (!containerRef.current) return;

        const source = new VectorSource<Feature<Point>>();
        const map = new Map({
            target: containerRef.current,
            layers: [
                new TileLayer({ source: new OSM({
                    url: import.meta.env.VITE_OSM_TILE_URL || 'https://tile.openstreetmap.org/{z}/{x}/{y}.png',
                }) }),
                new VectorLayer({
                    source,
                    style: new Style({ image: new CircleStyle({
                        radius: 9,
                        fill: new Fill({ color: '#dc2626' }),
                        stroke: new Stroke({ color: '#fff', width: 3 }),
                    }) }),
                }),
            ],
            view: new View({ center: fromLonLat([10.5, 51.2]), zoom: 6 }),
        });
        mapRef.current = map;
        sourceRef.current = source;
        // The dialog becomes visible after mounting; measure it in the next frame.
        const frame = requestAnimationFrame(() => map.updateSize());

        return () => {
            cancelAnimationFrame(frame);
            map.setTarget(undefined);
            map.dispose();
            mapRef.current = null;
            sourceRef.current = null;
        };
    }, []);

    useEffect(() => {
        const map = mapRef.current;
        const source = sourceRef.current;
        if (!map || !source) return;

        source.clear();
        if (!visibleCoordinates) return;

        const point = fromLonLat([longitude, latitude]);
        source.addFeature(new Feature({ geometry: new Point(point) }));
        map.getView().setCenter(point);
        map.getView().setZoom(17);
    }, [latitude, longitude, visibleCoordinates]);

    return (
        <section className="hydrant-location-preview" aria-labelledby="hydrant-map-title">
            <h3 id="hydrant-map-title">Standort prüfen</h3>
            <p id="hydrant-map-hint" role="status">
                {visibleCoordinates
                    ? `Markierter Standort: ${latitude}, ${longitude}`
                    : validCoordinates
                        ? 'Dieser Breitengrad liegt außerhalb des darstellbaren Kartenbereichs (±85,0511°).'
                    : hasCoordinates
                        ? 'Bitte gültige Koordinaten eingeben: Breitengrad −90 bis 90, Längengrad −180 bis 180.'
                        : 'Breiten- und Längengrad eingeben, um den Standort auf der Karte zu prüfen.'}
            </p>
            <div ref={containerRef} className="hydrant-location-map" role="region"
                aria-label="Kartenvorschau des Hydrantenstandorts" aria-describedby="hydrant-map-hint" tabIndex={0} />
        </section>
    );
}
