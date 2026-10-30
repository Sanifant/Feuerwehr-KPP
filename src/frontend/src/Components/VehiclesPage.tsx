import { useCallback, useEffect, useRef, useState } from 'react';
import type { FormEvent } from 'react';
import { isAxiosError } from 'axios';
import { useRole } from '../hooks/useRole';
import { getApiErrorMessage } from '../api/errors';
import { decideVehicleReservation, formatVehicleDate, getVehicleManagementOptions,
    getVehicleReservations, getVehicles, getVehicleTypes, getVehicleStatuses, reservationStatusLabels, saveVehicle, setVehicleActive } from '../api/vehicleApi';
import type { SaveVehicle, Vehicle, VehicleManagementOptions, VehicleReservation, VehicleType, VehicleStatus } from '../api/vehicleApi';
import VehicleModal from './VehicleModal';
import VehicleCalendar from './VehicleCalendar';
import VehicleBookingModal from './VehicleBookingModal';
import VehicleTransferTools from './VehicleTransferTools';
import './VehiclesPage.css';

export default function VehiclesPage({ management = false }: { management?: boolean }) {
    const { canManageVehicles, hasRole } = useRole();
    const isOfficer = hasRole('EquipmentOfficer');
    const [vehicles, setVehicles] = useState<Vehicle[]>([]);
    const [types, setTypes] = useState<VehicleType[]>([]);
    const [statuses, setStatuses] = useState<VehicleStatus[]>([]);
    const [mine, setMine] = useState<VehicleReservation[]>([]);
    const [assigned, setAssigned] = useState<VehicleReservation[]>([]);
    const [options, setOptions] = useState<VehicleManagementOptions>({ departments: [], officers: [], defaultStatusId: '' });
    const [includeInactive, setIncludeInactive] = useState(false);
    const [loading, setLoading] = useState(true);
    const [busy, setBusy] = useState(false);
    const [error, setError] = useState('');
    const [notice, setNotice] = useState('');
    const [modalError, setModalError] = useState('');
    const [editor, setEditor] = useState<{ id: string | null; draft: SaveVehicle } | null>(null);
    const [bookingVehicle, setBookingVehicle] = useState<Vehicle | null>(null);
    const sequence = useRef(0);
    const invalidate = useCallback(() => { sequence.current++; }, []);
    const mutation = useRef(false);

    const load = useCallback(async () => {
        const request = ++sequence.current;
        if (management && !canManageVehicles) { setLoading(false); return; }
        setLoading(true);
        try {
            const [list, own, responsible, managementOptions, typeList, statusList] = await Promise.all([
                getVehicles(management && includeInactive), management ? Promise.resolve([]) : getVehicleReservations('mine'),
                !management && isOfficer ? getVehicleReservations('assigned') : Promise.resolve([]),
                management && canManageVehicles ? getVehicleManagementOptions() : Promise.resolve(null),
                management && canManageVehicles ? getVehicleTypes(true) : Promise.resolve([]),
                management && canManageVehicles ? getVehicleStatuses(true) : Promise.resolve([]),
            ]);
            if (request !== sequence.current) return;
            setVehicles(list); setMine(own); setAssigned(responsible); setTypes(typeList); setStatuses(statusList);
            if (managementOptions) setOptions(managementOptions);
            setError('');
        } catch (err) {
            if (request === sequence.current) setError(getApiErrorMessage(err, 'MTW-Daten konnten nicht geladen werden.'));
        } finally { if (request === sequence.current) setLoading(false); }
    }, [management, canManageVehicles, includeInactive, isOfficer]);

    useEffect(() => {
        void load();
        const refresh = () => { if (document.visibilityState === 'visible') void load(); };
        window.addEventListener('focus', refresh);
        document.addEventListener('visibilitychange', refresh);
        return () => { invalidate(); window.removeEventListener('focus', refresh); document.removeEventListener('visibilitychange', refresh); };
    }, [load, invalidate]);

    if (management && !canManageVehicles) return <section className="card"><h1>Fahrzeug Verwaltung</h1><p role="alert">Keine Berechtigung für die Fahrzeugverwaltung.</p></section>;

    const run = async (action: () => Promise<void>, message: string, modal = false) => {
        if (mutation.current) return;
        mutation.current = true; setBusy(true); setError(''); setModalError(''); setNotice('');
        try { await action(); setNotice(message); }
        catch (err) {
            const text = getApiErrorMessage(err, 'Die Änderung konnte nicht gespeichert werden. Bitte erneut versuchen.');
            if (isAxiosError(err) && err.response?.status === 409) await load();
            if (modal) setModalError(text); else setError(text);
        } finally { mutation.current = false; setBusy(false); }
    };
    const openEditor = (vehicle?: Vehicle) => {
        setModalError('');
        const firstDepartment = options.departments[0]?.id ?? 0;
        setEditor({ id: vehicle?.id ?? null, draft: vehicle ? {
            fireDepartmentId: vehicle.fireDepartmentId, radioCallSign: vehicle.radioCallSign, name: vehicle.name ?? '', licensePlate: vehicle.licensePlate ?? '',
            equipmentOfficerId: vehicle.equipmentOfficerId, expectedVersion: vehicle.version,
            vehicleTypeId: vehicle.vehicleTypeId,
            vehicleStatusId: vehicle.vehicleStatusId,
        } : { fireDepartmentId: firstDepartment, radioCallSign: '', name: '', licensePlate: '',
            equipmentOfficerId: options.officers.find(x => x.fireDepartmentId === firstDepartment)?.id ?? '', expectedVersion: 1, vehicleTypeId: null,
            vehicleStatusId: options.defaultStatusId || statuses.find(status => status.isDefault)?.id || null } });
    };
    const submitVehicle = (event: FormEvent) => {
        event.preventDefault(); if (!editor) return;
        if (!editor.draft.radioCallSign.trim()) { setModalError('Bitte einen Funkrufnamen angeben.'); return; }
        void run(async () => { await saveVehicle(editor.id, editor.draft); setEditor(null); await load(); }, 'Fahrzeug gespeichert.', true);
    };
    const decide = (reservation: VehicleReservation, decision: 'approve' | 'reject' | 'cancel') => void run(async () => {
        await decideVehicleReservation(reservation, decision); await load();
    }, decision === 'approve' ? 'Buchung bestätigt.' : decision === 'reject' ? 'Anfrage abgelehnt.' : 'Reservierung storniert.');

    return <div className="vehicles-page">
        <header className="page-header"><div><h1>{management ? 'Fahrzeug Verwaltung' : 'MTW-Übersicht'}</h1>
            <p>{management ? 'Fahrzeuge anlegen, bearbeiten und ihren Aktivstatus verwalten.' : 'Fahrzeug anklicken und einen Tag oder einen mehrtägigen Zeitraum anfragen.'}</p></div>
            {management && <button disabled={busy || loading || !options.departments.length} onClick={() => openEditor()}>Fahrzeug anlegen</button>}
        </header>
        {error && <p className="alert alert-error" role="alert">{error}</p>}
        {notice && <p role="status">{notice}</p>}
        {management && <VehicleTransferTools kind="vehicles" disabled={busy || loading} onBusyChange={setBusy} onImported={load} />}
        {management ? <section className="card">
            <div className="vehicle-management-toolbar"><label><input type="checkbox" checked={includeInactive}
                onChange={event => setIncludeInactive(event.target.checked)} /> Deaktivierte Fahrzeuge anzeigen</label>
                <button className="btn-secondary" disabled={busy || loading} onClick={() => void load()}>Aktualisieren</button></div>
            {loading && <p role="status">Fahrzeuge werden geladen …</p>}
            <div className="vehicle-management-table"><table aria-label="Fahrzeuge" aria-busy={loading}>
                <thead><tr><th scope="col">Funkrufname</th><th scope="col">Name</th><th scope="col">Kennzeichen</th><th scope="col">Status</th><th scope="col">Aktionen</th></tr></thead>
                <tbody>{vehicles.map(vehicle => <tr key={vehicle.id} className={!vehicle.isActive ? 'vehicle-management-inactive' : undefined}>
                    <th scope="row">{vehicle.canBookDirectly && vehicle.isActive && vehicle.allowsBooking ? <button className="vehicle-management-book" disabled={busy || loading}
                        aria-label={`${vehicle.radioCallSign} direkt buchen`} onClick={() => setBookingVehicle(vehicle)}>{vehicle.radioCallSign}</button> : vehicle.radioCallSign}
                        {!vehicle.isActive && <small>Deaktiviert</small>}</th>
                    <td>{vehicle.name || '—'}</td><td>{vehicle.licensePlate || '—'}</td><td>{vehicle.vehicleStatusName}</td>
                    <td>{vehicle.canEdit ? <div className="vehicle-row-actions"><button className="btn-secondary" disabled={busy || loading}
                        aria-label={`${vehicle.radioCallSign} bearbeiten`} onClick={() => openEditor(vehicle)}>Bearbeiten</button>
                        <button className="btn-secondary" disabled={busy || loading}
                            aria-label={`${vehicle.radioCallSign} ${vehicle.isActive ? 'deaktivieren' : 'aktivieren'}`}
                            onClick={() => void run(async () => { await setVehicleActive(vehicle, !vehicle.isActive); await load(); }, 'Fahrzeugstatus geändert.')}>
                            {vehicle.isActive ? 'Deaktivieren' : 'Aktivieren'}</button></div> : '—'}</td>
                </tr>)}</tbody>
            </table></div>
            {!loading && !vehicles.length && <p>Keine Fahrzeuge vorhanden.</p>}
        </section> : <>
            <VehicleCalendar vehicles={vehicles} loading={loading} busy={busy} onChanged={load} />
            {isOfficer && <ReservationList title="Anfragen und Buchungen meiner Fahrzeuge" reservations={assigned} busy={busy} onDecision={decide} />}
            <ReservationList title="Meine Reservierungen" reservations={mine} busy={busy} onDecision={decide} />
        </>}
        {bookingVehicle && <VehicleBookingModal vehicle={bookingVehicle} onClose={() => setBookingVehicle(null)} onRefresh={load}
            onSaved={async () => { setNotice('Das Fahrzeug wurde verbindlich gebucht.'); await load(); }} />}
        {editor && <VehicleModal title={editor.id ? 'Fahrzeug bearbeiten' : 'Fahrzeug anlegen'} busy={busy} onClose={() => setEditor(null)}>
            <form onSubmit={submitVehicle}>
                {modalError && <p className="alert alert-error" role="alert">{modalError}</p>}
                <fieldset disabled={busy}>
                <label>Feuerwehr<select required value={editor.draft.fireDepartmentId || ''} onChange={event => {
                    const id = Number(event.target.value);
                    setEditor({ ...editor, draft: { ...editor.draft, fireDepartmentId: id, equipmentOfficerId: options.officers.find(x => x.fireDepartmentId === id)?.id ?? '' } });
                }}><option value="" disabled>Bitte wählen</option>{options.departments.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}</select></label>
                <label>Funkrufname<input required maxLength={100} value={editor.draft.radioCallSign} onChange={event => setEditor({ ...editor, draft: { ...editor.draft, radioCallSign: event.target.value } })} /></label>
                <label>Fahrzeugtyp<select value={editor.draft.vehicleTypeId ?? ''} onChange={event => setEditor({ ...editor, draft: { ...editor.draft, vehicleTypeId: event.target.value || null } })}>
                    <option value="">Nicht zugeordnet</option>{types.filter(type => type.isActive || type.id === editor.draft.vehicleTypeId)
                        .map(type => <option key={type.id} value={type.id}>{type.name}{!type.isActive && ' (deaktiviert)'}</option>)}</select></label>
                {!types.some(type => type.isActive) && <small>Administratoren können Fahrzeugtypen unter Verwaltung → Fahrzeugtypen anlegen.</small>}
                <label>Status<select required value={editor.draft.vehicleStatusId ?? ''} onChange={event => setEditor({ ...editor, draft: { ...editor.draft, vehicleStatusId: event.target.value || null } })}>
                    <option value="" disabled>Bitte wählen</option>{statuses.filter(status => status.isActive || status.id === editor.draft.vehicleStatusId)
                        .map(status => <option key={status.id} value={status.id}>{status.name}{!status.isActive && ' (deaktiviert)'}</option>)}</select></label>
                <label>Fahrzeugbezeichnung (optional)<input maxLength={150} value={editor.draft.name} onChange={event => setEditor({ ...editor, draft: { ...editor.draft, name: event.target.value } })} /></label>
                <label>Kennzeichen (optional)<input maxLength={30} value={editor.draft.licensePlate} onChange={event => setEditor({ ...editor, draft: { ...editor.draft, licensePlate: event.target.value } })} /></label>
                <label>Zuständiger Gerätewart<select required value={editor.draft.equipmentOfficerId} onChange={event => setEditor({ ...editor, draft: { ...editor.draft, equipmentOfficerId: event.target.value } })}>
                    <option value="" disabled>Bitte wählen</option>{options.officers.filter(x => x.fireDepartmentId === editor.draft.fireDepartmentId).map(item => <option key={item.id} value={item.id}>{item.name}</option>)}</select></label>
                </fieldset>
                <footer><button type="button" className="btn-secondary" disabled={busy} onClick={() => setEditor(null)}>Abbrechen</button><button disabled={busy}>Fahrzeug speichern</button></footer>
            </form>
        </VehicleModal>}
    </div>;
}

function ReservationList({ title, reservations, busy, onDecision }: {
    title: string; reservations: VehicleReservation[]; busy: boolean;
    onDecision: (reservation: VehicleReservation, decision: 'approve' | 'reject' | 'cancel') => void;
}) {
    return <section className="card"><h2>{title}</h2>{!reservations.length && <p>Keine Reservierungen vorhanden.</p>}
        <div className="vehicle-reservations">{reservations.map(item => <article key={item.id} className="vehicle-reservation">
            <h3>{item.radioCallSign}</h3><p>{formatVehicleDate(item.from)} bis einschließlich {formatVehicleDate(item.to)}</p>
            <p><strong>{reservationStatusLabels[item.status]}</strong>{item.isDirectBooking && ' · Direktbuchung'}</p>
            <p>{item.requesterName} · {item.fireDepartmentName}</p>
            {item.comment && <p className="vehicle-comment"><strong>Kommentar / Buchungsgrund:</strong><br />{item.comment}</p>}
            <div className="vehicle-row-actions">{item.canDecide && <><button disabled={busy} onClick={() => onDecision(item, 'approve')}>Bestätigen</button>
                <button className="btn-secondary" disabled={busy} onClick={() => onDecision(item, 'reject')}>Ablehnen</button></>}
                {item.canCancel && <button className="btn-secondary" disabled={busy} onClick={() => onDecision(item, 'cancel')}>{item.status === 'Pending' ? 'Anfrage zurückziehen' : 'Buchung stornieren'}</button>}
            </div>
        </article>)}</div>
    </section>;
}
