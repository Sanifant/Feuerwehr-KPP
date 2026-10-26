import { useEffect, useRef, useState } from 'react';
import { getArchivedDiaryEntries } from '../api/incidentApi';
import type { DiaryEntry, DiaryRevision, IncidentSummary } from '../api/incidentApi';
import { getApiErrorMessage } from '../api/errors';

const dateTime = (value: string) => new Date(value).toLocaleString('de-DE', { timeZone: 'Europe/Berlin' });
const revisionType = (type: DiaryRevision['revisionType']) => type === 'Canceled' ? 'Storniert' : type === 'Edited' ? 'Korrigiert' : 'Erstellt';

function RevisionContent({ revision }: { revision: DiaryRevision }) {
    return <div className="archive-revision">
        <dl className="incident-details">
            <div><dt>Ereigniszeit</dt><dd>{dateTime(revision.eventTimestampUtc)}</dd></div>
            <div><dt>Kategorie</dt><dd>{revision.categoryNameSnapshot}</dd></div>
            <div><dt>Absender</dt><dd>{revision.sender || '–'}</dd></div>
            <div><dt>Empfänger</dt><dd>{revision.recipient || '–'}</dd></div>
            <div><dt>Übermittlungsweg</dt><dd>{revision.transmissionType || '–'}</dd></div>
        </dl>
        <p className="incident-description">{revision.text}</p>
        {revision.cancellationReason && <p>Stornierungsgrund: {revision.cancellationReason}</p>}
        <p className="small-note">Version {revision.revisionNumber} · {revisionType(revision.revisionType)} · Erfasst am {dateTime(revision.createdAtUtc)} von {revision.createdByDisplayName}</p>
    </div>;
}

export default function IncidentArchiveDiaryModal({ incident, onClose }: { incident: IncidentSummary; onClose: () => void }) {
    const dialogRef = useRef<HTMLDialogElement>(null);
    const [entries, setEntries] = useState<DiaryEntry[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState('');
    const [attempt, setAttempt] = useState(0);

    useEffect(() => {
        const dialog = dialogRef.current;
        if (!dialog) return;
        const previousFocus = document.activeElement;
        const previousOverflow = document.body.style.overflow;
        dialog.showModal();
        dialog.querySelector<HTMLButtonElement>('.modal-close')?.focus();
        document.body.style.overflow = 'hidden';
        return () => {
            dialog.close();
            document.body.style.overflow = previousOverflow;
            if (previousFocus instanceof HTMLElement && previousFocus.isConnected) previousFocus.focus();
        };
    }, []);

    useEffect(() => {
        let disposed = false;
        setLoading(true);
        setError('');
        getArchivedDiaryEntries(incident.id).then(data => { if (!disposed) setEntries(data); })
            .catch((failure: unknown) => { if (!disposed) setError(getApiErrorMessage(failure, 'Das Einsatztagebuch konnte nicht geladen werden.')); })
            .finally(() => { if (!disposed) setLoading(false); });
        return () => { disposed = true; };
    }, [incident.id, attempt]);

    return <dialog ref={dialogRef} className="modal archive-diary-modal" aria-modal="true" aria-labelledby="archive-diary-title"
        onCancel={event => { event.preventDefault(); onClose(); }} onClick={event => { if (event.target === event.currentTarget) onClose(); }}>
        <header className="modal-header">
            <h2 id="archive-diary-title">Einsatztagebuch: {incident.name}</h2>
            <button type="button" className="modal-close" onClick={onClose} aria-label="Schließen">×</button>
        </header>
        <p>Abgeschlossener Einsatz · {incident.incidentNumber || 'Keine Einsatznummer'} · {incident.keyword || 'Kein Stichwort'}</p>
        <p className="small-note">Schreibgeschütztes Archiv. Alle Zeitangaben in Europe/Berlin.</p>
        {loading && <p role="status">Einsatztagebuch wird geladen …</p>}
        {error && <div role="alert"><p>{error}</p><button type="button" onClick={() => setAttempt(value => value + 1)}>Erneut versuchen</button></div>}
        {!loading && !error && entries.length === 0 && <p>Keine Tagebucheinträge vorhanden.</p>}
        {!loading && !error && entries.map(entry => <article className="archive-diary-entry" key={entry.id}>
            <h3>Eintrag #{entry.entryNumber}{entry.isCanceled ? ' · Storniert' : entry.currentRevision > 1 ? ' · Korrigiert' : ''}</h3>
            <RevisionContent revision={entry.currentRevisionData} />
            {entry.revisions.length > 1 && <details><summary>Versionshistorie ({entry.revisions.length} Versionen)</summary>
                <ol className="archive-revisions">{entry.revisions.map(revision => <li key={revision.id}><RevisionContent revision={revision} /></li>)}</ol>
            </details>}
        </article>)}
        <div className="incident-actions"><button type="button" className="btn-secondary" onClick={onClose}>Schließen</button></div>
    </dialog>;
}
