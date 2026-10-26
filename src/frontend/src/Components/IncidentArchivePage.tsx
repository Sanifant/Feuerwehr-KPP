import { useCallback, useEffect, useRef, useState } from 'react';
import { useRole } from '../hooks/useRole';
import { downloadArchivedIncidentPdf, getIncidentArchive } from '../api/incidentApi';
import type { IncidentSummary } from '../api/incidentApi';
import { getApiErrorMessage } from '../api/errors';
import IncidentArchiveDiaryModal from './IncidentArchiveDiaryModal';

const pageSize = 25;
const dateTime = (value?: string) => value ? new Date(value).toLocaleString('de-DE', { timeZone: 'Europe/Berlin' }) : '–';

export default function IncidentArchivePage() {
    const { isAdmin } = useRole();
    const [incidents, setIncidents] = useState<IncidentSummary[]>([]);
    const [page, setPage] = useState(0);
    const [hasMore, setHasMore] = useState(false);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState('');
    const [selected, setSelected] = useState<IncidentSummary | null>(null);
    const [downloading, setDownloading] = useState<string | null>(null);
    const downloadRef = useRef(false);
    const requestRef = useRef(0);

    const load = useCallback(async (requestedPage: number) => {
        if (!isAdmin) return;
        const request = ++requestRef.current;
        setLoading(true);
        setError('');
        try {
            const result = await getIncidentArchive(requestedPage, pageSize);
            if (request !== requestRef.current) return;
            setIncidents(previous => requestedPage === 1 ? result : [...previous, ...result]);
            setPage(requestedPage);
            setHasMore(result.length === pageSize);
        } catch (failure: unknown) {
            if (request === requestRef.current) setError(getApiErrorMessage(failure, 'Das Einsatzarchiv konnte nicht geladen werden.'));
        } finally {
            if (request === requestRef.current) setLoading(false);
        }
    }, [isAdmin]);
    const invalidate = useCallback(() => { ++requestRef.current; }, []);

    useEffect(() => {
        void load(1);
        return invalidate;
    }, [load, invalidate]);

    const download = async (incident: IncidentSummary) => {
        if (!isAdmin || downloadRef.current) return;
        downloadRef.current = true;
        setDownloading(incident.id);
        setError('');
        try {
            await downloadArchivedIncidentPdf(incident.id);
        } catch (failure: unknown) {
            setError(getApiErrorMessage(failure, 'Die PDF konnte nicht heruntergeladen werden.'));
        } finally {
            downloadRef.current = false;
            setDownloading(null);
        }
    };

    if (!isAdmin) return <section className="card"><h2>Einsatzarchiv</h2><p role="alert">Das Einsatzarchiv ist nur für Administratoren zugänglich.</p></section>;

    return <div className="incident-page">
        <section className="card incident-top">
            <h2>Einsatzarchiv</h2>
            <p>Abgeschlossene Einsätze und ihre Einsatztagebücher. Alle Zeitangaben in Europe/Berlin.</p>
            <div className="incident-actions"><button type="button" disabled={loading} onClick={() => { void load(1); }}>Aktualisieren</button></div>
            {error && <p role="alert">{error}</p>}
            {loading && <p role="status">Einsätze werden geladen …</p>}
            {downloading && <p role="status">PDF wird heruntergeladen …</p>}
        </section>
        <section className="card" aria-label="Abgeschlossene Einsätze" aria-busy={loading}>
            <div className="log-table-wrap">
                <table className="log-table archive-table">
                    <thead><tr><th>Einsatz</th><th>Einsatznummer</th><th>Stichwort</th><th>Alarmiert am</th><th>Beendet am</th><th>PDF</th></tr></thead>
                    <tbody>{incidents.map(incident => <tr key={incident.id} onClick={() => setSelected(incident)}>
                        <td><button type="button" className="archive-open-button" onClick={() => setSelected(incident)}>{incident.name}</button></td>
                        <td>{incident.incidentNumber || '–'}</td><td>{incident.keyword || '–'}</td>
                        <td>{dateTime(incident.alarmedAtUtc)}</td><td>{dateTime(incident.closedAtUtc)}</td>
                        <td><button type="button" className="btn-secondary" disabled={downloading !== null}
                            aria-label={`Einsatz ${incident.name} als PDF herunterladen`} onClick={event => { event.stopPropagation(); void download(incident); }}>
                            {downloading === incident.id ? 'Wird geladen …' : 'PDF herunterladen'}
                        </button></td>
                    </tr>)}</tbody>
                </table>
            </div>
            {!loading && !error && incidents.length === 0 && <p>Keine abgeschlossenen Einsätze vorhanden.</p>}
            {hasMore && <button type="button" disabled={loading} onClick={() => { void load(page + 1); }}>Weitere Einsätze laden</button>}
        </section>
        {selected && <IncidentArchiveDiaryModal key={selected.id} incident={selected} onClose={() => setSelected(null)} />}
    </div>;
}
