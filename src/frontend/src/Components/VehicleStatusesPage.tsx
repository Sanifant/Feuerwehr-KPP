import { useCallback, useEffect, useRef, useState } from 'react';
import type { FormEvent } from 'react';
import { useRole } from '../hooks/useRole';
import { getApiErrorMessage } from '../api/errors';
import { getVehicleStatuses, saveVehicleStatus } from '../api/vehicleApi';
import type { VehicleStatus } from '../api/vehicleApi';
import VehicleModal from './VehicleModal';
import './VehiclesPage.css';

export default function VehicleStatusesPage() {
    const { isAdmin } = useRole();
    const [statuses, setStatuses] = useState<VehicleStatus[]>([]);
    const [editor, setEditor] = useState<{ id: string | null; name: string; allowsBooking: boolean; isActive: boolean; isDefault: boolean; expectedVersion: number } | null>(null);
    const [loading, setLoading] = useState(true);
    const [busy, setBusy] = useState(false);
    const [error, setError] = useState('');
    const [modalError, setModalError] = useState('');
    const running = useRef(false);
    const load = useCallback(async () => {
        if (!isAdmin) return;
        setLoading(true);
        try { setStatuses(await getVehicleStatuses(true)); setError(''); }
        catch (err) { setError(getApiErrorMessage(err, 'Fahrzeugstatus konnten nicht geladen werden.')); }
        finally { setLoading(false); }
    }, [isAdmin]);
    useEffect(() => { void load(); }, [load]);
    if (!isAdmin) return <section className="card"><h1>Fahrzeugstatus</h1><p role="alert">Nur Administratoren dürfen Fahrzeugstatus verwalten.</p></section>;
    const submit = async (event: FormEvent) => {
        event.preventDefault(); if (!editor || running.current) return;
        if (!editor.name.trim()) { setModalError('Bitte einen Namen angeben.'); return; }
        running.current = true; setBusy(true); setModalError('');
        try {
            await saveVehicleStatus(editor.id, { name: editor.name.trim(), allowsBooking: editor.allowsBooking, isActive: editor.isActive, expectedVersion: editor.expectedVersion });
            setEditor(null); await load();
        } catch (err) { setModalError(getApiErrorMessage(err, 'Status konnte nicht gespeichert werden.')); }
        finally { running.current = false; setBusy(false); }
    };
    return <div className="vehicles-page"><header className="page-header"><div><h1>Fahrzeugstatus</h1>
        <p>Statusliste erweitern und festlegen, ob Fahrzeuge mit diesem Status gebucht werden können. Bestehende Buchungen bleiben erhalten.</p></div>
        <button disabled={busy || loading} onClick={() => { setModalError(''); setEditor({ id: null, name: '', allowsBooking: false, isActive: true, isDefault: false, expectedVersion: 1 }); }}>Status anlegen</button></header>
        {error && <p role="alert" className="alert alert-error">{error}</p>}
        <section className="card"><button className="btn-secondary" disabled={busy || loading} onClick={() => void load()}>Aktualisieren</button>
            {loading && <p role="status">Status werden geladen …</p>}
            <table><thead><tr><th>Status</th><th>Buchungen</th><th>Aktiv</th><th>Aktionen</th></tr></thead><tbody>{statuses.map(status => <tr key={status.id}>
                <td>{status.name}{status.isDefault && ' (Standard)'}</td><td>{status.allowsBooking ? 'Erlaubt' : 'Gesperrt'}</td><td>{status.isActive ? 'Ja' : 'Nein'}</td>
                <td><button className="btn-secondary" disabled={busy || loading} aria-label={`${status.name} bearbeiten`} onClick={() => {
                    setModalError(''); setEditor({ ...status, expectedVersion: status.version });
                }}>Bearbeiten</button></td></tr>)}</tbody></table>
        </section>
        {editor && <VehicleModal title={editor.id ? 'Fahrzeugstatus bearbeiten' : 'Fahrzeugstatus anlegen'} busy={busy} onClose={() => setEditor(null)}>
            <form onSubmit={event => void submit(event)}>{modalError && <p role="alert" className="alert alert-error">{modalError}</p>}
                <label>Name des Status<input required maxLength={100} disabled={busy} value={editor.name} onChange={event => setEditor({ ...editor, name: event.target.value })} /></label>
                <label><input type="checkbox" disabled={busy} checked={editor.allowsBooking} onChange={event => setEditor({ ...editor, allowsBooking: event.target.checked })} /> Buchungen erlauben</label>
                <label><input type="checkbox" disabled={busy || editor.isDefault} checked={editor.isActive} onChange={event => setEditor({ ...editor, isActive: event.target.checked })} /> Für neue Zuordnungen verfügbar</label>
                <footer><button type="button" className="btn-secondary" disabled={busy} onClick={() => setEditor(null)}>Abbrechen</button><button disabled={busy}>Status speichern</button></footer>
            </form>
        </VehicleModal>}
    </div>;
}
