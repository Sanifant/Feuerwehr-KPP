import { useEffect, useMemo, useState } from 'react';
import { Fragment } from 'react';
import { useRole } from '../hooks/useRole';
import {
    buildCommandId,
    cancelDiaryEntry,
    createDiaryEntry,
    getActiveIncident,
    getDiaryCategories,
    getDiaryEntries,
    getDiaryPdfExportUrl,
    getIncidentArchive,
    updateDiaryEntry,
} from '../api/incidentApi';
import { ensureIncidentConnection } from '../api/incidentLive';
import type { DiaryCategory, DiaryEntry } from '../api/incidentApi';

const displayTimezone = 'Europe/Berlin';

function toInputUtc(value: Date) {
    return value.toISOString().slice(0, 16);
}

export function IncidentDiaryPage() {
    const { canViewIncidentDiary, canEditIncidentDiary } = useRole();
    const [incident, setIncident] = useState<any | null>(null);
    const [entries, setEntries] = useState<DiaryEntry[]>([]);
    const [categories, setCategories] = useState<DiaryCategory[]>([]);
    const [archive, setArchive] = useState<any[]>([]);
    const [statusText, setStatusText] = useState('Lade...');
    const [draft, setDraft] = useState({
        eventTimestampUtc: toInputUtc(new Date()),
        categoryCode: 'meldung',
        text: '',
        sender: '',
        recipient: '',
        transmissionType: '',
    });
    const [editDrafts, setEditDrafts] = useState<Record<string, any>>({});
    const [expandedEntryId, setExpandedEntryId] = useState<string | null>(null);

    const loadData = async () => {
        try {
            const active = await getActiveIncident();
            setIncident(active);

            const [entryData, categoryData, archives] = await Promise.all([
                getDiaryEntries(active.id),
                getDiaryCategories(active.id),
                getIncidentArchive(),
            ]);

            setEntries(entryData);
            setCategories(categoryData);
            setArchive(archives);
            setStatusText(`Aktiver Einsatz: ${active.name}`);
        } catch {
            setIncident(null);
            setEntries([]);
            setCategories([]);
            setStatusText('Kein aktiver Einsatz verfügbar.');
            const archives = await getIncidentArchive().catch(() => []);
            setArchive(archives);
        }
    };

    useEffect(() => {
        if (!canViewIncidentDiary) {
            return;
        }

        loadData();
    }, [canViewIncidentDiary]);

    useEffect(() => {
        if (!incident) {
            return;
        }

        let disposed = false;
        let connection: any;
        let diaryUpdatedHandler: (() => Promise<void>) | null = null;

        ensureIncidentConnection().then(async (conn) => {
            connection = conn;
            await conn.invoke('SubscribeIncident', incident.id);
            await conn.invoke('SubscribeDiary', incident.id);

            diaryUpdatedHandler = async () => {
                if (!disposed) {
                    await loadData();
                }
            };

            conn.on('DiaryUpdated', diaryUpdatedHandler);
        }).catch(() => {
            setStatusText('Live-Verbindung getrennt. Stand kann veraltet sein.');
        });

        return () => {
            disposed = true;
            if (connection && diaryUpdatedHandler) {
                connection.off('DiaryUpdated', diaryUpdatedHandler);
            }
        };
    }, [incident?.id]);

    const activeCategories = useMemo(() => categories.filter(c => c.isActive), [categories]);

    if (!canViewIncidentDiary) {
        return <div className="card"><h2>Einsatztagebuch</h2><p>Keine Berechtigung.</p></div>;
    }

    const submitNewEntry = async () => {
        if (!incident || !canEditIncidentDiary) {
            return;
        }

        if (!draft.categoryCode || !draft.text.trim()) {
            setStatusText('Kategorie und Text sind Pflichtfelder.');
            return;
        }

        try {
            await createDiaryEntry(incident.id, {
                commandId: buildCommandId(),
                eventTimestampUtc: new Date(draft.eventTimestampUtc).toISOString(),
                categoryCode: draft.categoryCode,
                text: draft.text,
                sender: draft.sender || null,
                recipient: draft.recipient || null,
                transmissionType: draft.transmissionType || null,
            });

            setDraft((prev) => ({ ...prev, text: '', sender: '', recipient: '', transmissionType: '' }));
            setStatusText('Eintrag gespeichert.');
            await loadData();
        } catch (error: any) {
            setStatusText(error?.response?.data?.title ?? 'Eintrag konnte nicht gespeichert werden.');
        }
    };

    const openEdit = (entry: DiaryEntry) => {
        const current = entry.currentRevisionData;
        setEditDrafts((prev) => ({
            ...prev,
            [entry.id]: {
                eventTimestampUtc: toInputUtc(new Date(current.eventTimestampUtc)),
                categoryCode: current.categoryCode,
                text: current.text,
                sender: current.sender ?? '',
                recipient: current.recipient ?? '',
                transmissionType: current.transmissionType ?? '',
                expectedRevision: entry.currentRevision,
                cancellationReason: '',
            },
        }));

        setExpandedEntryId(entry.id);
    };

    const submitEdit = async (entry: DiaryEntry) => {
        if (!incident || !canEditIncidentDiary) {
            return;
        }

        const edit = editDrafts[entry.id];
        if (!edit) {
            return;
        }

        try {
            await updateDiaryEntry(incident.id, entry.id, {
                commandId: buildCommandId(),
                expectedRevision: edit.expectedRevision,
                eventTimestampUtc: new Date(edit.eventTimestampUtc).toISOString(),
                categoryCode: edit.categoryCode,
                text: edit.text,
                sender: edit.sender || null,
                recipient: edit.recipient || null,
                transmissionType: edit.transmissionType || null,
            });
            setStatusText('Korrektur gespeichert.');
            await loadData();
        } catch (error: any) {
            setStatusText(error?.response?.data?.title ?? 'Korrektur fehlgeschlagen. Lokaler Entwurf bleibt erhalten.');
        }
    };

    const submitCancel = async (entry: DiaryEntry) => {
        if (!incident || !canEditIncidentDiary) {
            return;
        }

        const edit = editDrafts[entry.id];
        if (!edit?.cancellationReason?.trim()) {
            setStatusText('Für die Stornierung ist eine Begründung erforderlich.');
            return;
        }

        try {
            await cancelDiaryEntry(incident.id, entry.id, {
                commandId: buildCommandId(),
                expectedRevision: entry.currentRevision,
                reason: edit.cancellationReason,
            });
            setStatusText('Eintrag storniert.');
            await loadData();
        } catch (error: any) {
            setStatusText(error?.response?.data?.title ?? 'Stornierung fehlgeschlagen.');
        }
    };

    return (
        <div className="incident-page">
            <section className="card incident-top">
                <h2>Einsatztagebuch</h2>
                <p>{statusText}</p>
                {incident ? (
                    <div className="incident-meta-line">
                        <strong>{incident.name}</strong>
                        <span>Status: {incident.status === 'Closed' ? 'Abgeschlossen' : 'Aktiv'}</span>
                        <span>Anzeige-Zeitzone: {displayTimezone}</span>
                        {canEditIncidentDiary && (
                            <a href={getDiaryPdfExportUrl(incident.id)} target="_blank" rel="noreferrer">PDF-Export</a>
                        )}
                    </div>
                ) : (
                    <span>Archiv ist weiterhin verfügbar.</span>
                )}
            </section>

            {incident && (
                <section className="card">
                    <h3>Neuer Eintrag</h3>
                    <div className="diary-grid">
                        <label>
                            Ereigniszeit (UTC)
                            <input type="datetime-local" value={draft.eventTimestampUtc} onChange={(e) => setDraft(prev => ({ ...prev, eventTimestampUtc: e.target.value }))} disabled={!canEditIncidentDiary} />
                        </label>
                        <label>
                            Kategorie
                            <select value={draft.categoryCode} onChange={(e) => setDraft(prev => ({ ...prev, categoryCode: e.target.value }))} disabled={!canEditIncidentDiary}>
                                {activeCategories.map(cat => <option key={cat.code} value={cat.code}>{cat.name}</option>)}
                            </select>
                        </label>
                        <label>
                            Absender
                            <input value={draft.sender} onChange={(e) => setDraft(prev => ({ ...prev, sender: e.target.value }))} disabled={!canEditIncidentDiary} />
                        </label>
                        <label>
                            Empfänger
                            <input value={draft.recipient} onChange={(e) => setDraft(prev => ({ ...prev, recipient: e.target.value }))} disabled={!canEditIncidentDiary} />
                        </label>
                        <label>
                            Übermittlungsweg
                            <input value={draft.transmissionType} onChange={(e) => setDraft(prev => ({ ...prev, transmissionType: e.target.value }))} disabled={!canEditIncidentDiary} />
                        </label>
                        <label className="diary-grid-full">
                            Text*
                            <textarea value={draft.text} onChange={(e) => setDraft(prev => ({ ...prev, text: e.target.value }))} disabled={!canEditIncidentDiary} />
                        </label>
                    </div>
                    <button type="button" onClick={submitNewEntry} disabled={!canEditIncidentDiary || incident.status === 'Closed'}>Eintrag speichern</button>
                </section>
            )}

            <section className="card">
                <h3>Tagebucheinträge</h3>
                <div className="log-table-wrap">
                    <table className="log-table">
                        <thead>
                            <tr>
                                <th>Nr.</th>
                                <th>Ereigniszeit (Europe/Berlin)</th>
                                <th>Kategorie</th>
                                <th>Text</th>
                                <th>Status</th>
                                <th>Version</th>
                                {canEditIncidentDiary && <th>Aktionen</th>}
                            </tr>
                        </thead>
                        <tbody>
                            {entries.map((entry) => {
                                const current = entry.currentRevisionData;
                                const edit = editDrafts[entry.id];
                                const isOpen = expandedEntryId === entry.id;

                                return (
                                    <Fragment key={entry.id}>
                                        <tr key={entry.id}>
                                            <td>{entry.entryNumber}</td>
                                            <td>{new Date(current.eventTimestampUtc).toLocaleString('de-DE', { timeZone: displayTimezone })}</td>
                                            <td>{current.categoryNameSnapshot}</td>
                                            <td>{current.text}</td>
                                            <td>{entry.isCanceled ? `Storniert: ${entry.cancellationReason}` : current.revisionType === 'Edited' ? 'Korrigiert' : 'Aktiv'}</td>
                                            <td>{entry.currentRevision}</td>
                                            {canEditIncidentDiary && (
                                                <td>
                                                    <button type="button" onClick={() => openEdit(entry)} disabled={incident?.status === 'Closed'}>Bearbeiten</button>
                                                </td>
                                            )}
                                        </tr>
                                        {isOpen && edit && canEditIncidentDiary && (
                                            <tr>
                                                <td colSpan={7}>
                                                    <div className="entry-editor">
                                                        <label>
                                                            Ereigniszeit (UTC)
                                                            <input type="datetime-local" value={edit.eventTimestampUtc} onChange={(e) => setEditDrafts(prev => ({ ...prev, [entry.id]: { ...edit, eventTimestampUtc: e.target.value } }))} />
                                                        </label>
                                                        <label>
                                                            Kategorie
                                                            <select value={edit.categoryCode} onChange={(e) => setEditDrafts(prev => ({ ...prev, [entry.id]: { ...edit, categoryCode: e.target.value } }))}>
                                                                {activeCategories.map(cat => <option key={cat.code} value={cat.code}>{cat.name}</option>)}
                                                            </select>
                                                        </label>
                                                        <label>
                                                            Text
                                                            <textarea value={edit.text} onChange={(e) => setEditDrafts(prev => ({ ...prev, [entry.id]: { ...edit, text: e.target.value } }))} />
                                                        </label>
                                                        <label>
                                                            Stornierungsbegründung
                                                            <input value={edit.cancellationReason} onChange={(e) => setEditDrafts(prev => ({ ...prev, [entry.id]: { ...edit, cancellationReason: e.target.value } }))} />
                                                        </label>
                                                        <div className="incident-actions">
                                                            <button type="button" onClick={() => submitEdit(entry)} disabled={incident?.status === 'Closed'}>Korrektur speichern</button>
                                                            <button type="button" onClick={() => submitCancel(entry)} disabled={incident?.status === 'Closed' || entry.isCanceled}>Stornieren</button>
                                                        </div>
                                                        <details>
                                                            <summary>Versionshistorie</summary>
                                                            <ul>
                                                                {entry.revisions.map((revision) => (
                                                                    <li key={revision.id}>v{revision.revisionNumber} ({revision.revisionType}) – {revision.categoryNameSnapshot}: {revision.text}</li>
                                                                ))}
                                                            </ul>
                                                        </details>
                                                    </div>
                                                </td>
                                            </tr>
                                        )}
                                    </Fragment>
                                );
                            })}
                            {entries.length === 0 && (
                                <tr><td colSpan={7}>Keine Einträge vorhanden.</td></tr>
                            )}
                        </tbody>
                    </table>
                </div>
            </section>

            <section className="card">
                <h3>Archiv (nur Lesen)</h3>
                <ul className="archive-list">
                    {archive.map((item: any) => (
                        <li key={item.id}>
                            <strong>{item.name}</strong>
                            <span>{item.closedAtUtc ? new Date(item.closedAtUtc).toLocaleString('de-DE') : '-'}</span>
                        </li>
                    ))}
                    {archive.length === 0 && <li>Keine abgeschlossenen Einsätze.</li>}
                </ul>
            </section>
        </div>
    );
}

export default IncidentDiaryPage;
