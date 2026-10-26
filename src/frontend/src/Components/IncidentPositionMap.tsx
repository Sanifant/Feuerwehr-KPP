import { useEffect, useRef } from 'react';
import Feature from 'ol/Feature';
import Map from 'ol/Map';
import View from 'ol/View';
import Point from 'ol/geom/Point';
import TileLayer from 'ol/layer/Tile';
import VectorLayer from 'ol/layer/Vector';
import OSM from 'ol/source/OSM';
import VectorSource from 'ol/source/Vector';
import { fromLonLat, toLonLat } from 'ol/proj';
import 'ol/ol.css';

interface Props {
    position: [number, number] | null;
    onSelect: (position: [number, number]) => void;
}

export default function IncidentPositionMap({ position, onSelect }: Props) {
    const containerRef = useRef<HTMLDivElement>(null);
    const sourceRef = useRef(new VectorSource<Feature<Point>>());
    const mapRef = useRef<Map | null>(null);
    const selectRef = useRef(onSelect);

    useEffect(() => { selectRef.current = onSelect; }, [onSelect]);
    useEffect(() => {
        if (!containerRef.current) return;
        const map = new Map({
            target: containerRef.current,
            layers: [
                new TileLayer({ source: new OSM({
                    url: import.meta.env.VITE_OSM_TILE_URL || 'https://tile.openstreetmap.org/{z}/{x}/{y}.png',
                }) }),
                new VectorLayer({ source: sourceRef.current }),
            ],
            view: new View({ center: fromLonLat([10.5, 51.2]), zoom: 7 }),
        });
        map.on('singleclick', event => {
            const [longitude, latitude] = toLonLat(event.coordinate);
            selectRef.current([((longitude + 180) % 360 + 360) % 360 - 180, latitude]);
        });
        mapRef.current = map;
        return () => { map.setTarget(undefined); map.dispose(); mapRef.current = null; };
    }, []);

    useEffect(() => {
        sourceRef.current.clear();
        if (!position) return;
        const coordinate = fromLonLat(position);
        sourceRef.current.addFeature(new Feature({ geometry: new Point(coordinate) }));
        mapRef.current?.getView().setCenter(coordinate);
    }, [position]);

    return <div ref={containerRef} className="incident-position-map" role="region" tabIndex={0}
        aria-label="Einsatzposition auf der Karte wählen" />;
}
