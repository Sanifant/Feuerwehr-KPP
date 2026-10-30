import { useCallback, useEffect, useRef, useState } from 'react';
import { useRole } from '../hooks/useRole';
import { getApiErrorMessage } from '../api/errors';
import { formatVehicleDate, getVehicleAvailability, vehicleCalendarRange, vehicleDate, vehicleDayLabels } from '../api/vehicleApi';
import type { Vehicle, VehicleAvailability } from '../api/vehicleApi';
import VehicleBookingModal from './VehicleBookingModal';
import './VehicleCalendar.css';

export default function VehicleCalendar({ vehicles, loading = false, busy = false, onChanged, vehicleType = 'MTW' }: {
    vehicles?: Vehicle[]; loading?: boolean; busy?: boolean; onChanged?: () => Promise<void>;
    vehicleType?: string;
}) {
    const { canRequestVehicles } = useRole();
    const [today, setToday] = useState(() => vehicleDate(new Date()));
    const { from, to, days } = vehicleCalendarRange(new Date(`${today}T12:00:00`));
    const [availability, setAvailability] = useState<VehicleAvailability[]>([]);
    const [calendarLoading, setCalendarLoading] = useState(true);
    const [error, setError] = useState('');
    const [notice, setNotice] = useState('');
    const [department, setDepartment] = useState('');
    const [selected, setSelected] = useState<Vehicle | null>(null);
    const sequence = useRef(0);
    const invalidate = useCallback(() => { sequence.current++; }, []);
    const load = useCallback(async () => {
        const request = ++sequence.current;
        setCalendarLoading(true);
        try {
            const result = await getVehicleAvailability(from, to, vehicleType);
            if (request === sequence.current) { setAvailability(result); setError(''); }
        } catch (err) {
            if (request === sequence.current) setError(getApiErrorMessage(err, 'Die Fahrzeugverfügbarkeit konnte nicht geladen werden.'));
        } finally { if (request === sequence.current) setCalendarLoading(false); }
    }, [from, to, vehicleType]);
    useEffect(() => { void load(); return invalidate; }, [load, invalidate, vehicles]);
    const refresh = useCallback(async () => {
        const currentDay = vehicleDate(new Date());
        if (currentDay !== today) setToday(currentDay); else await load();
    }, [today, load]);
    useEffect(() => {
        const focus = () => { if (document.visibilityState === 'visible') void refresh(); };
        const timer = window.setInterval(() => setToday(vehicleDate(new Date())), 60_000);
        window.addEventListener('focus', focus); document.addEventListener('visibilitychange', focus);
        return () => { window.clearInterval(timer); window.removeEventListener('focus', focus); document.removeEventListener('visibilitychange', focus); };
    }, [refresh]);
    const list = (vehicles ?? availability.map(item => item.vehicle)).filter(vehicle =>
        !vehicleType || vehicle.vehicleTypeName?.trim().toUpperCase() === vehicleType.trim().toUpperCase());
    const departments = [...new Map(list.map(vehicle => [vehicle.fireDepartmentId, vehicle.fireDepartmentName])).entries()]
        .sort((a, b) => a[1].localeCompare(b[1], 'de'));
    const rows = list.filter(vehicle => !department || String(vehicle.fireDepartmentId) === department)
        .sort((a, b) => a.fireDepartmentName.localeCompare(b.fireDepartmentName, 'de') || a.radioCallSign.localeCompare(b.radioCallSign, 'de'));
    const isLoading = loading || calendarLoading;
    return <section className="card vehicle-calendar-card">
        <div className="vehicle-toolbar"><div><h2>MTW-Verfügbarkeit</h2><p>{formatVehicleDate(from)} bis {formatVehicleDate(to)}</p></div>
            <label>Feuerwehr<select value={department} onChange={event => setDepartment(event.target.value)}><option value="">Alle Feuerwehren</option>
                {departments.map(([id, name]) => <option key={id} value={id}>{name}</option>)}</select></label>
            <button className="btn-secondary" disabled={busy || isLoading} onClick={() => void (async () => {
                if (onChanged) await onChanged();
                await refresh();
            })()}>Aktualisieren</button>
        </div>
        <div className="vehicle-legend" aria-label="Kalenderlegende"><span><i className="vehicle-day--Free" /> Frei</span>
            <span><i className="vehicle-day--Pending" /> Reserviert – Anfrage offen</span><span><i className="vehicle-day--Booked" /> Gebucht</span>
            <span><i className="vehicle-day--Unavailable" /> Nicht verfügbar</span></div>
        <p className="vehicle-hint">Die vergangenen drei Tage, heute und die folgenden 20 Tage. Fahrzeug anklicken, um eine Buchung anzufragen.
            Orange Anfragen sind unverbindlich; erst die Bestätigung bucht das Fahrzeug.</p>
        {error && <p className="alert alert-error" role="alert">{error}</p>}
        {notice && <p role="status">{notice}</p>}
        {isLoading && <p role="status">MTW-Daten werden geladen …</p>}
        <div className="vehicle-calendar-scroll" role="region" aria-label="MTW-Verfügbarkeit" tabIndex={0} aria-busy={isLoading}>
            <table className="vehicle-calendar"><caption className="vehicle-sr-only">Tagesverfügbarkeit der MTW vom {formatVehicleDate(from)} bis {formatVehicleDate(to)}</caption>
                <thead><tr><th scope="col">Fahrzeug / Funkrufname</th><th scope="col" className="vehicle-type-column">Fahrzeugtyp</th>
                    {days.map(day => <th key={day} scope="col" aria-label={formatVehicleDate(day)} aria-current={day === today ? 'date' : undefined}>
                        <time dateTime={day}>{new Date(`${day}T12:00:00`).toLocaleDateString('de-DE', { day: '2-digit', month: '2-digit' })}</time>
                        <small>{new Date(`${day}T12:00:00`).toLocaleDateString('de-DE', { weekday: 'short' })}</small>
                    </th>)}</tr></thead>
                <tbody>{rows.map(vehicle => {
                    const statuses = new Map(availability.find(item => item.vehicle.id === vehicle.id)?.days.map(day => [day.date, day.status]) ?? []);
                    return <tr key={vehicle.id}><th scope="row"><button className="vehicle-name"
                        disabled={busy || isLoading || !vehicle.isActive || !vehicle.allowsBooking || (!canRequestVehicles && !vehicle.canBookDirectly)}
                        aria-label={`${vehicle.radioCallSign} buchen`} onClick={() => setSelected(vehicle)}>{vehicle.radioCallSign}</button>
                        <small>{vehicle.fireDepartmentName}</small><small>{[vehicle.name, vehicle.licensePlate].filter(Boolean).join(' · ')}</small>
                        <small>{vehicle.vehicleStatusName}</small>
                        {!vehicle.isActive && <small>Deaktiviert – bestehende Buchungen bleiben erhalten</small>}
                    </th><td className="vehicle-type-column">{vehicle.vehicleTypeName ?? 'Nicht zugeordnet'}</td>
                        {days.map(day => {
                            const status = statuses.get(day);
                            const label = status ? vehicleDayLabels[status] : vehicle.isActive ? 'Verfügbarkeit nicht verfügbar' : 'Deaktiviert';
                            return <td key={day} className={`vehicle-day vehicle-day--${status ?? 'Inactive'}${day === today ? ' vehicle-day--today' : ''}`}
                                title={`${vehicle.radioCallSign}, ${formatVehicleDate(day)}: ${label}`}>
                                <span className="vehicle-sr-only">{formatVehicleDate(day)}: {label}</span>
                                <span aria-hidden="true">{status === 'Booked' ? '×' : status === 'Pending' ? '•' : status === 'Free' ? '✓' : '–'}</span>
                            </td>;
                        })}</tr>;
                })}</tbody>
            </table>
            {!isLoading && !rows.length && <p>Keine Fahrzeuge vorhanden.</p>}
        </div>
        {selected && <VehicleBookingModal vehicle={selected} onClose={() => setSelected(null)} onRefresh={refresh} onSaved={async direct => {
            setNotice(direct ? 'Das Fahrzeug wurde verbindlich gebucht.' : 'Die Anfrage wurde gespeichert. Der Gerätewart wird per E-Mail benachrichtigt.');
            await refresh(); if (onChanged) await onChanged();
        }} />}
    </section>;
}
