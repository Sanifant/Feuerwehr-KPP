import { useMemo, useRef, useState } from 'react';
import type { FormEvent } from 'react';
import { Link } from 'react-router-dom';
import { useActiveIncident } from '../hooks/useActiveIncident';
import { useRole } from '../hooks/useRole';
import { acquireMapLease, buildCommandId, closeIncident, createIncident } from '../api/incidentApi';
import { getIncidentSessionId } from '../api/incidentSession';
import IncidentPositionMap from './IncidentPositionMap';

function errorMessage(error: unknown, fallback: string) {
    const problem = (error as { response?: { data?: { title?: string; detail?: string } } })?.response?.data;
    return problem?.title ? `${problem.title}${problem.detail ? ` ${problem.detail}` : ''}` : fallback;
}

function localDateTimeNow() {
    const now = new Date();
    return new Date(now.getTime() - now.getTimezoneOffset() * 60000).toISOString().slice(0, 16);
}

export default function IncidentOverviewPage() {
    const { incident, loading, error, refresh, setActiveIncident } = useActiveIncident();
    const { canEditSituationMap, canViewSituationMap, canViewIncidentDiary } = useRole();
    const sessionId = useMemo(getIncidentSessionId, []);
    const [draft, setDraft] = useState({ name: '', incidentNumber: '', keyword: '', description: '' });
    const [latitude, setLatitude] = useState('');
    const [longitude, setLongitude] = useState('');
    const [alarmedAt, setAlarmedAt] = useState(localDateTimeNow);
    const [busy, setBusy] = useState(false);
    const busyRef = useRef(false);
    const [confirmClose, setConfirmClose] = useState(false);
    const [message, setMessage] = useState('');
    const [failed, setFailed] = useState(false);
    const lat = Number(latitude);
    const lon = Number(longitude);
    const validPosition = latitude !== '' && longitude !== '' && Number.isFinite(lat) && Number.isFinite(lon)
        && lat >= -90 && lat <= 90 && lon >= -180 && lon <= 180;
    const position = useMemo<[number, number] | null>(() => validPosition ? [lon, lat] : null, [validPosition, lon, lat]);

    const create = async (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();
        if (busyRef.current || !canEditSituationMap || incident || error) return;
        if (!draft.name.trim() || !validPosition || !alarmedAt || !Number.isFinite(new Date(alarmedAt).getTime())) {
            setFailed(true);
            setMessage('Bitte Einsatzname, Alarmierungszeitpunkt und eine gültige Einsatzposition angeben.');
            return;
        }
        busyRef.current = true;
        setBusy(true);
        try {
            const created = await createIncident({
                name: draft.name.trim(), incidentNumber: draft.incidentNumber.trim() || undefined,
                keyword: draft.keyword.trim() || undefined, description: draft.description.trim() || undefined,
                alarmedAtUtc: new Date(alarmedAt).toISOString(),
                originLongitude: lon, originLatitude: lat, sessionId, commandId: buildCommandId(),
            });
            setActiveIncident(created.incident);
            setDraft({ name: '', incidentNumber: '', keyword: '', description: '' });
            setLatitude('');
            setLongitude('');
            setAlarmedAt(localDateTimeNow());
            setFailed(false);
            setMessage('Einsatz wurde angelegt.');
        } catch (failure: unknown) {
            setFailed(true);
            setMessage(errorMessage(failure, 'Einsatz konnte nicht angelegt werden.'));
            await refresh();
        } finally {
            busyRef.current = false;
            setBusy(false);
        }
    };

    const finish = async () => {
        if (busyRef.current || !canEditSituationMap || !incident) return;
        busyRef.current = true;
        setBusy(true);
        try {
            // Der Server vergibt die Sperre nur, wenn keine andere Sitzung bearbeitet.
            const { lease } = await acquireMapLease(incident.id, { sessionId, commandId: buildCommandId() });
            await closeIncident(incident.id, { sessionId, leaseToken: lease.leaseToken, commandId: buildCommandId() });
            setActiveIncident(null);
            setConfirmClose(false);
            setFailed(false);
            setMessage('Einsatz wurde beendet. Lagekarte und Einsatztagebuch sind jetzt inaktiv.');
        } catch (failure: unknown) {
            setFailed(true);
            setMessage(errorMessage(failure, 'Einsatz konnte nicht beendet werden.'));
            await refresh();
        } finally {
            busyRef.current = false;
            setBusy(false);
        }
    };

    if (!canViewSituationMap && !canViewIncidentDiary) return <section className="card"><h2>Einsatzübersicht</h2><p>Keine Berechtigung.</p></section>;

    return <div className="incident-page">
        <section className="card incident-top">
            <h2>Einsatzübersicht</h2>
            <p>Aktiven Einsatz anlegen und nach Einsatzende abschließen.</p>
            {message && <p role={failed ? 'alert' : 'status'}>{message}</p>}
            {loading && <p role="status">Aktiver Einsatz wird geladen …</p>}
            {error && <p role="alert">{error}</p>}
            <div className="incident-actions">
                <button type="button" disabled={busy || loading} onClick={() => { void refresh(); }}>Aktualisieren</button>
            </div>
        </section>

        {!loading && (incident ? <section className="card incident-top">
            <h3>Aktiver Einsatz: {incident.name}</h3>
            <dl className="incident-details">
                <div><dt>Status</dt><dd>Aktiv</dd></div>
                <div><dt>Einsatznummer</dt><dd>{incident.incidentNumber || '–'}</dd></div>
                <div><dt>Stichwort</dt><dd>{incident.keyword || '–'}</dd></div>
                <div><dt>Alarmierungszeitpunkt</dt><dd>{new Date(incident.alarmedAtUtc).toLocaleString('de-DE')}</dd></div>
                <div><dt>Angelegt am</dt><dd>{new Date(incident.createdAtUtc).toLocaleString('de-DE')}</dd></div>
                <div><dt>Einsatzposition</dt><dd>{incident.originLatitude.toFixed(5)}, {incident.originLongitude.toFixed(5)}</dd></div>
            </dl>
            {incident.description && <p className="incident-description">{incident.description}</p>}
            <div className="incident-actions">
                {canViewSituationMap && <Link to="/incident-map">Lagekarte öffnen</Link>}
                {canViewIncidentDiary && <Link to="/incident-diary">Einsatztagebuch öffnen</Link>}
                {canEditSituationMap && !confirmClose && <button type="button" className="btn-danger" disabled={busy} onClick={() => setConfirmClose(true)}>Einsatz beenden</button>}
            </div>
            {confirmClose && <div className="incident-close-confirmation">
                <p>Einsatz „{incident.name}“ endgültig beenden? Danach können Lagekarte und Einsatztagebuch nicht mehr bearbeitet werden.</p>
                <div className="incident-actions">
                    <button type="button" className="btn-danger" disabled={busy} onClick={() => { void finish(); }}>{busy ? 'Einsatz wird beendet …' : 'Endgültig beenden'}</button>
                    <button type="button" disabled={busy} onClick={() => setConfirmClose(false)}>Abbrechen</button>
                </div>
            </div>}
        </section> : !error && <section className="card incident-top">
            <h3>Kein aktiver Einsatz</h3>
            <p>Lagekarte und Einsatztagebuch werden nach dem Anlegen eines Einsatzes aktiviert.</p>
            {canEditSituationMap ? <form onSubmit={create}>
                <fieldset className="incident-create-fields" disabled={busy}>
                    <legend>Neuen Einsatz anlegen</legend>
                    <div className="incident-create-grid">
                        <label>Einsatzname *<input required maxLength={200} value={draft.name} onChange={event => setDraft({ ...draft, name: event.target.value })} /></label>
                        <label>Einsatznummer<input maxLength={100} value={draft.incidentNumber} onChange={event => setDraft({ ...draft, incidentNumber: event.target.value })} /></label>
                        <label>Stichwort<input maxLength={200} value={draft.keyword} onChange={event => setDraft({ ...draft, keyword: event.target.value })} /></label>
                        <label>Alarmierungszeitpunkt *<input type="datetime-local" required value={alarmedAt} onChange={event => setAlarmedAt(event.target.value)} /></label>
                        <label>Beschreibung<textarea maxLength={4000} value={draft.description} onChange={event => setDraft({ ...draft, description: event.target.value })} /></label>
                        <label>Breitengrad *<input type="number" required min={-90} max={90} step="any" value={latitude} onChange={event => setLatitude(event.target.value)} /></label>
                        <label>Längengrad *<input type="number" required min={-180} max={180} step="any" value={longitude} onChange={event => setLongitude(event.target.value)} /></label>
                    </div>
                    <p className="small-note">Alarmierungszeitpunkt in Ortszeit ({Intl.DateTimeFormat().resolvedOptions().timeZone}). Einsatzposition auf der Karte wählen oder Koordinaten eingeben.</p>
                    <IncidentPositionMap position={position} onSelect={selected => {
                        if (busyRef.current) return;
                        setLongitude(selected[0].toFixed(6)); setLatitude(selected[1].toFixed(6));
                    }} />
                    <button type="submit">{busy ? 'Einsatz wird angelegt …' : 'Einsatz anlegen'}</button>
                </fieldset>
            </form> : <p>Zum Anlegen eines Einsatzes ist die Berechtigung zur Lagekartenbearbeitung erforderlich.</p>}
        </section>)}
    </div>;
}
