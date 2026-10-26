import { Link, Outlet, useLocation } from 'react-router-dom';
import { useActiveIncident } from '../hooks/useActiveIncident';
import { useRole } from '../hooks/useRole';

export default function ActiveIncidentRoute() {
    const { incident, loading, error, refresh } = useActiveIncident();
    const { canViewSituationMap, canViewIncidentDiary } = useRole();
    const { pathname } = useLocation();
    const permitted = pathname === '/incident-map' ? canViewSituationMap : canViewIncidentDiary;
    if (!permitted) return <section className="card"><h2>Keine Berechtigung.</h2></section>;
    if (loading) return <section className="card"><p role="status">Aktiver Einsatz wird geladen …</p></section>;
    if (error || !incident) return <section className="card incident-top">
        <h2>{pathname === '/incident-map' ? 'Lagekarte' : 'Einsatztagebuch'}</h2>
        <p role={error ? 'alert' : 'status'}>{error ?? 'Bitte zuerst einen aktiven Einsatz in der Einsatzübersicht anlegen.'}</p>
        <Link to="/incidents">Zur Einsatzübersicht</Link>
        {error && <button type="button" onClick={() => { void refresh(); }}>Erneut versuchen</button>}
    </section>;
    return <Outlet key={incident.id} />;
}
