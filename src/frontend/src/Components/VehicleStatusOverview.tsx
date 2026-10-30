import { useCallback, useEffect, useRef, useState } from 'react';
import { getApiErrorMessage } from '../api/errors';
import { getVehicles } from '../api/vehicleApi';
import type { Vehicle } from '../api/vehicleApi';
import './VehicleStatusOverview.css';

const statusClasses: Record<string, string> = {
    abgemeldet: 'vehicle-status-overview__row--deregistered',
    wartung: 'vehicle-status-overview__row--maintenance',
};
const statusOrder: Record<string, number> = { abgemeldet: 0, wartung: 1, betriebsbereit: 3 };
const dateFormat = new Intl.DateTimeFormat('de-DE', {
    day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit',
});

export default function VehicleStatusOverview() {
    const [vehicles, setVehicles] = useState<Vehicle[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState('');
    const [department, setDepartment] = useState('');
    const [vehicleType, setVehicleType] = useState('');
    const [sortBy, setSortBy] = useState<'status' | 'callSign'>('status');
    const sequence = useRef(0);
    const invalidate = useCallback(() => { sequence.current++; }, []);
    const load = useCallback(async () => {
        const request = ++sequence.current;
        setLoading(true);
        try {
            const result = await getVehicles(false);
            if (request === sequence.current) { setVehicles(result); setError(''); }
        } catch (err) {
            if (request === sequence.current) setError(getApiErrorMessage(err, 'Der Fahrzeugstatus konnte nicht geladen werden.'));
        } finally { if (request === sequence.current) setLoading(false); }
    }, []);
    useEffect(() => {
        void load();
        const refresh = () => { if (document.visibilityState === 'visible') void load(); };
        window.addEventListener('focus', refresh);
        document.addEventListener('visibilitychange', refresh);
        const timer = window.setInterval(refresh, 60_000);
        return () => {
            invalidate(); window.clearInterval(timer);
            window.removeEventListener('focus', refresh); document.removeEventListener('visibilitychange', refresh);
        };
    }, [load, invalidate]);
    const departments = [...new Map(vehicles.map(vehicle => [vehicle.fireDepartmentId, vehicle.fireDepartmentName])).entries()]
        .sort((a, b) => a[1].localeCompare(b[1], 'de'));
    const types = [...new Map(vehicles.filter(vehicle => vehicle.vehicleTypeId !== null)
        .map(vehicle => [vehicle.vehicleTypeId!, vehicle.vehicleTypeName ?? 'Nicht zugeordnet'])).entries()]
        .sort((a, b) => a[1].localeCompare(b[1], 'de'));
    const hasUnassignedType = vehicles.some(vehicle => vehicle.vehicleTypeId === null);
    const rows = vehicles.filter(vehicle => (!department || String(vehicle.fireDepartmentId) === department) &&
        (!vehicleType || (vehicleType === 'unassigned' ? vehicle.vehicleTypeId === null : vehicle.vehicleTypeId === vehicleType)))
        .sort((a, b) => {
            if (sortBy === 'status') {
                const first = a.vehicleStatusName.trim().toLowerCase();
                const second = b.vehicleStatusName.trim().toLowerCase();
                // Additional status values appear before operational vehicles.
                const priority = (statusOrder[first] ?? 2) - (statusOrder[second] ?? 2);
                if (priority) return priority;
                const name = first.localeCompare(second, 'de');
                if (name) return name;
            }
            return a.radioCallSign.localeCompare(b.radioCallSign, 'de', { numeric: true });
        });
    return <section className="card vehicle-status-overview">
        <header><h2>Fahrzeugstatus</h2><button className="btn-secondary" disabled={loading} onClick={() => void load()}>Fahrzeugstatus aktualisieren</button></header>
        <div className="vehicle-status-overview__filters" role="group" aria-label="Fahrzeugfilter">
            <label>Feuerwehr<select value={department} disabled={loading} onChange={event => setDepartment(event.target.value)}>
                <option value="">Alle Feuerwehren</option>{departments.map(([id, name]) => <option key={id} value={id}>{name}</option>)}
            </select></label>
            <label>Fahrzeugtyp<select value={vehicleType} disabled={loading} onChange={event => setVehicleType(event.target.value)}>
                <option value="">Alle Fahrzeugtypen</option>{types.map(([id, name]) => <option key={id} value={id}>{name}</option>)}
                {hasUnassignedType && <option value="unassigned">Nicht zugeordnet</option>}
            </select></label>
            <label>Sortierung<select value={sortBy} onChange={event => setSortBy(event.target.value === 'callSign' ? 'callSign' : 'status')}>
                <option value="status">Status: Abgemeldet → Wartung → Betriebsbereit</option><option value="callSign">Funkrufname</option>
            </select></label>
            <button className="btn-secondary" disabled={!department && !vehicleType} onClick={() => { setDepartment(''); setVehicleType(''); }}>Filter zurücksetzen</button>
        </div>
        {error && <p className="alert alert-error" role="alert">{error}</p>}
        {loading && <p role="status">Fahrzeugstatus werden geladen …</p>}
        <div className="vehicle-status-overview__table"><table aria-label="Fahrzeugstatus" aria-busy={loading}>
            <thead><tr><th scope="col" aria-sort={sortBy === 'callSign' ? 'ascending' : undefined}>Funkrufname</th><th scope="col">Fahrzeugtyp</th><th scope="col">Feuerwehr</th>
                <th scope="col" aria-sort={sortBy === 'status' ? 'ascending' : undefined}>Status</th><th scope="col">Letzte Statusänderung</th></tr></thead>
            <tbody>{rows.map(vehicle => <tr key={vehicle.id} className={statusClasses[vehicle.vehicleStatusName.trim().toLowerCase()]}>
                <th scope="row">{vehicle.radioCallSign}</th><td>{vehicle.vehicleTypeName ?? 'Nicht zugeordnet'}</td><td>{vehicle.fireDepartmentName}</td>
                <td>{vehicle.vehicleStatusName}</td><td>{vehicle.statusChangedAtUtc
                    ? <time dateTime={vehicle.statusChangedAtUtc}>{dateFormat.format(new Date(vehicle.statusChangedAtUtc))}</time>
                    : 'Nicht erfasst'}</td>
            </tr>)}</tbody>
        </table></div>
        {!loading && !error && rows.length === 0 && <p>{vehicles.length ? 'Keine Fahrzeuge für die gewählten Filter.' : 'Keine Fahrzeuge vorhanden.'}</p>}
    </section>;
}
