import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { useActiveIncident } from '../hooks/useActiveIncident';

export default function IncidentTitlebar() {
    const { incident } = useActiveIncident();
    const [now, setNow] = useState(Date.now);

    useEffect(() => {
        if (!incident) return;
        const tick = () => setNow(Date.now());
        tick();
        const timer = window.setInterval(tick, 1000);
        return () => window.clearInterval(timer);
    }, [incident]);

    if (!incident) return null;
    const elapsed = Math.max(0, Math.floor((now - Date.parse(incident.alarmedAtUtc)) / 1000));
    const hours = Math.floor(elapsed / 3600);
    const minutes = Math.floor(elapsed / 60) % 60;
    const seconds = elapsed % 60;
    const duration = [hours, minutes, seconds].map(value => String(value).padStart(2, '0')).join(':');
    const date = new Date(now);

    return <section className="titlebar-incident" aria-label="Aktiver Einsatz">
        <Link to="/incidents" className="titlebar-incident__name" title={`${incident.name} · ${incident.keyword || 'Kein Stichwort'}`}>
            <strong>{incident.name}</strong>
            <span>Stichwort: {incident.keyword || '–'}</span>
        </Link>
        <div className="titlebar-incident__time">
            <span>Uhrzeit</span>
            <time dateTime={date.toISOString()}>{date.toLocaleTimeString('de-DE', { hour: '2-digit', minute: '2-digit', second: '2-digit', hour12: false })}</time>
        </div>
        <div className="titlebar-incident__time" title="Dauer seit der Alarmierung (Stunden:Minuten:Sekunden)">
            <span>Einsatzdauer</span>
            <time dateTime={`PT${elapsed}S`}>{duration}</time>
        </div>
    </section>;
}
