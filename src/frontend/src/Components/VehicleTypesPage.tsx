import { useCallback, useEffect, useRef, useState } from 'react';
import type { FormEvent } from 'react';
import { useRole } from '../hooks/useRole';
import { getApiErrorMessage } from '../api/errors';
import { getVehicleTypes, saveVehicleType, setVehicleTypeActive } from '../api/vehicleApi';
import type { VehicleType } from '../api/vehicleApi';
import VehicleModal from './VehicleModal';
import VehicleTransferTools from './VehicleTransferTools';
import './VehiclesPage.css';

export default function VehicleTypesPage() {
    const { isAdmin } = useRole();
    const [types, setTypes] = useState<VehicleType[]>([]);
    const [editor, setEditor] = useState<{ id: string | null; name: string; expectedVersion: number } | null>(null);
    const [loading, setLoading] = useState(true);
    const [busy, setBusy] = useState(false);
    const [error, setError] = useState('');
    const [modalError, setModalError] = useState('');
    const [notice, setNotice] = useState('');
    const running = useRef(false);
    const load = useCallback(async () => {
        if (!isAdmin) return;
        setLoading(true);
        try { setTypes(await getVehicleTypes(true)); setError(''); }
        catch (err) { setError(getApiErrorMessage(err, 'Fahrzeugtypen konnten nicht geladen werden.')); }
        finally { setLoading(false); }
    }, [isAdmin]);
    useEffect(() => { void load(); }, [load]);
    if (!isAdmin) return <section className="card"><h1>Fahrzeugtypen</h1><p role="alert">Nur Administratoren dürfen Fahrzeugtypen verwalten.</p></section>;
    const submit = async (event: FormEvent) => {
        event.preventDefault(); if (!editor || running.current) return;
        if (!editor.name.trim()) { setModalError('Bitte einen Namen angeben.'); return; }
        running.current = true; setBusy(true); setModalError('');
        try {
            await saveVehicleType(editor.id, { name: editor.name.trim(), expectedVersion: editor.expectedVersion });
            setEditor(null); setNotice('Fahrzeugtyp gespeichert.'); await load();
        } catch (err) { setModalError(getApiErrorMessage(err, 'Fahrzeugtyp konnte nicht gespeichert werden.')); }
        finally { running.current = false; setBusy(false); }
    };
    const toggle = async (type: VehicleType) => {
        if (running.current) return;
        running.current = true; setBusy(true); setError(''); setNotice('');
        try { await setVehicleTypeActive(type, !type.isActive); await load(); setNotice('Fahrzeugtyp aktualisiert.'); }
        catch (err) { setError(getApiErrorMessage(err, 'Fahrzeugtyp konnte nicht geändert werden. Bitte neu laden.')); }
        finally { running.current = false; setBusy(false); }
    };
    return <div className="vehicles-page"><header className="page-header"><div><h1>Fahrzeugtypen</h1>
        <p>Typen für die Fahrzeugauswahl anlegen, umbenennen und deaktivieren. Bestehende Zuordnungen bleiben erhalten.</p></div>
        <button disabled={busy || loading} onClick={() => { setModalError(''); setEditor({ id: null, name: '', expectedVersion: 1 }); }}>Fahrzeugtyp anlegen</button></header>
        {error && <p className="alert alert-error" role="alert">{error}</p>}{notice && <p role="status">{notice}</p>}
        <VehicleTransferTools kind="types" disabled={busy || loading} onBusyChange={setBusy} onImported={load} />
        <section className="card"><button className="btn-secondary" disabled={busy || loading} onClick={() => void load()}>Aktualisieren</button>
            {loading && <p role="status">Fahrzeugtypen werden geladen …</p>}
            <table><thead><tr><th>Fahrzeugtyp</th><th>Status</th><th>Aktionen</th></tr></thead><tbody>{types.map(type => <tr key={type.id}>
                <td>{type.name}</td><td>{type.isActive ? 'Aktiv' : 'Deaktiviert'}</td><td><div className="vehicle-row-actions">
                    <button className="btn-secondary" disabled={busy || loading} aria-label={`${type.name} bearbeiten`}
                        onClick={() => { setModalError(''); setEditor({ id: type.id, name: type.name, expectedVersion: type.version }); }}>Bearbeiten</button>
                    <button className="btn-secondary" disabled={busy || loading} aria-label={`${type.name} ${type.isActive ? 'deaktivieren' : 'aktivieren'}`}
                        onClick={() => void toggle(type)}>{type.isActive ? 'Deaktivieren' : 'Aktivieren'}</button>
                </div></td></tr>)}</tbody></table>
            {!loading && !types.length && <p>Keine Fahrzeugtypen vorhanden.</p>}
        </section>
        {editor && <VehicleModal title={editor.id ? 'Fahrzeugtyp bearbeiten' : 'Fahrzeugtyp anlegen'} busy={busy} onClose={() => setEditor(null)}>
            <form onSubmit={event => void submit(event)}>{modalError && <p className="alert alert-error" role="alert">{modalError}</p>}
                <label>Name des Fahrzeugtyps<input required maxLength={100} disabled={busy} value={editor.name}
                    onChange={event => setEditor({ ...editor, name: event.target.value })} /></label>
                <footer><button type="button" className="btn-secondary" disabled={busy} onClick={() => setEditor(null)}>Abbrechen</button>
                    <button disabled={busy}>Fahrzeugtyp speichern</button></footer>
            </form>
        </VehicleModal>}
    </div>;
}
