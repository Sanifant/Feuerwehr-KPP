import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeAll, beforeEach, expect, it, vi } from 'vitest';
import VehiclesPage from './VehiclesPage';
import VehicleCalendar from './VehicleCalendar';
import { Dashboard } from './Dashboard';
import { useRole } from '../hooks/useRole';
import * as api from '../api/vehicleApi';
import type { Vehicle, VehicleReservation } from '../api/vehicleApi';

vi.mock('../hooks/useRole', () => ({ useRole: vi.fn() }));
vi.mock('../api/vehicleApi', async importOriginal => ({
    ...await importOriginal<typeof import('../api/vehicleApi')>(), getVehicles: vi.fn(), getVehicleAvailability: vi.fn(),
    getVehicleReservations: vi.fn(), getVehicleManagementOptions: vi.fn(), saveVehicle: vi.fn(),
    reserveVehicle: vi.fn(), decideVehicleReservation: vi.fn(), setVehicleActive: vi.fn(), getVehicleTypes: vi.fn(), getVehicleStatuses: vi.fn(),
}));

const vehicle: Vehicle = {
    id: 'vehicle-1', fireDepartmentId: 1, fireDepartmentName: 'Feuerwehr A', radioCallSign: 'Florian 19-01', name: 'MTW',
    licensePlate: null, equipmentOfficerId: 'officer', equipmentOfficerName: 'Ada', isActive: true, version: 1, canEdit: false, canBookDirectly: false,
    vehicleTypeId: 'type-1', vehicleTypeName: 'MTW',
    vehicleStatusId: 'ready', vehicleStatusName: 'Betriebsbereit', allowsBooking: true,
    statusChangedAtUtc: null,
};
const range = api.vehicleCalendarRange();
const date = (day: number) => range.days[day - 1];
const reservation: VehicleReservation = {
    id: 'reservation', vehicleId: vehicle.id, radioCallSign: vehicle.radioCallSign, fireDepartmentName: vehicle.fireDepartmentName,
    from: date(1), to: date(3), comment: 'Jugendfeuerwehr', status: 'Pending', requesterName: 'Ada', createdAtUtc: '',
    decidedAtUtc: null, isDirectBooking: false, version: 1, canDecide: false, canCancel: true,
};
function roles(...names: string[]) {
    vi.mocked(useRole).mockReturnValue({ hasRole: (role: string) => names.includes(role),
        canManageVehicles: names.includes('Admin') || names.includes('EquipmentOfficer'),
        canRequestVehicles: names.includes('Commander') || names.includes('Firefighter'),
    } as ReturnType<typeof useRole>);
}
beforeAll(() => {
    HTMLDialogElement.prototype.showModal = function () { this.setAttribute('open', ''); };
    HTMLDialogElement.prototype.close = function () { this.removeAttribute('open'); };
});
beforeEach(() => {
    vi.clearAllMocks(); roles('Firefighter');
    vi.mocked(api.getVehicles).mockResolvedValue([vehicle]);
    vi.mocked(api.getVehicleAvailability).mockResolvedValue([{ vehicle, days: [
        { date: date(1), status: 'Free' }, { date: date(2), status: 'Pending' }, { date: date(3), status: 'Booked' },
    ] }]);
    vi.mocked(api.getVehicleReservations).mockResolvedValue([]);
    vi.mocked(api.getVehicleManagementOptions).mockResolvedValue({ departments: [{ id: 1, name: 'Feuerwehr A' }], officers: [{ id: 'officer', name: 'Ada', fireDepartmentId: 1 }], defaultStatusId: 'ready' });
    vi.mocked(api.getVehicleStatuses).mockResolvedValue([
        { id: 'ready', name: 'Betriebsbereit', allowsBooking: true, isActive: true, version: 1, isDefault: true },
        { id: 'maintenance', name: 'Wartung', allowsBooking: false, isActive: true, version: 1, isDefault: false },
        { id: 'deregistered', name: 'Abgemeldet', allowsBooking: false, isActive: true, version: 1, isDefault: false },
    ]);
    vi.mocked(api.getVehicleTypes).mockResolvedValue([{ id: 'type-1', name: 'MTW', isActive: true, version: 1 }]);
    vi.mocked(api.reserveVehicle).mockResolvedValue(reservation);
});

it('renders the last three days, today and the next twenty days with a vehicle-type column and status colors', async () => {
    render(<VehiclesPage />);
    await screen.findByRole('button', { name: 'Florian 19-01 buchen' });
    const table = screen.getByRole('table');
    expect(api.getVehicleAvailability).toHaveBeenCalledWith(range.from, range.to, 'MTW');
    expect(within(table).getAllByRole('columnheader')).toHaveLength(26);
    expect(within(table).getByRole('columnheader', { name: api.formatVehicleDate(range.today) })).toHaveAttribute('aria-current', 'date');
    const cells = within(table).getAllByRole('cell');
    expect(cells[0]).toHaveTextContent('MTW');
    expect(cells[1]).toHaveClass('vehicle-day--Free');
    expect(cells[2]).toHaveClass('vehicle-day--Pending');
    expect(cells[3]).toHaveClass('vehicle-day--Booked');
    expect(within(table).getByRole('rowheader')).toHaveTextContent('Feuerwehr A');
});

it.each(['Commander', 'Firefighter'])('lets a %s open the vehicle modal and request several days across a year boundary with an optional comment', async role => {
    roles(role); const user = userEvent.setup(); render(<VehiclesPage />);
    await user.click(await screen.findByRole('button', { name: 'Florian 19-01 buchen' }));
    const dialog = screen.getByRole('dialog');
    expect(within(dialog).getByLabelText('Fahrzeug / Funkrufname')).toHaveValue('Florian 19-01');
    const year = new Date().getFullYear() + 1;
    fireEvent.change(within(dialog).getByLabelText('Von-Datum'), { target: { value: `${year}-12-30` } });
    fireEvent.change(within(dialog).getByLabelText('Bis-Datum (einschließlich)'), { target: { value: `${year + 1}-01-02` } });
    await user.type(within(dialog).getByLabelText('Kommentar / Buchungsgrund (optional)'), 'Fahrt zur Jugendfeuerwehr');
    await user.click(within(dialog).getByRole('button', { name: 'Reservierung anfragen' }));
    await waitFor(() => expect(api.reserveVehicle).toHaveBeenCalledWith(vehicle.id, expect.objectContaining({
        from: `${year}-12-30`, to: `${year + 1}-01-02`, comment: 'Fahrt zur Jugendfeuerwehr', commandId: expect.any(String),
    }), false));
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
});

it('preserves dates, comment and command ID after a conflicting or failed request', async () => {
    vi.mocked(api.reserveVehicle).mockRejectedValue(new Error('offline'));
    const user = userEvent.setup(); render(<VehiclesPage />);
    await user.click(await screen.findByRole('button', { name: 'Florian 19-01 buchen' }));
    await user.type(screen.getByLabelText('Kommentar / Buchungsgrund (optional)'), 'Ausbildung');
    await user.click(screen.getByRole('button', { name: 'Reservierung anfragen' }));
    expect(await screen.findByRole('alert')).toBeVisible();
    expect(screen.getByLabelText('Kommentar / Buchungsgrund (optional)')).toHaveValue('Ausbildung');
    await user.click(screen.getByRole('button', { name: 'Reservierung anfragen' }));
    await waitFor(() => expect(api.reserveVehicle).toHaveBeenCalledTimes(2));
    expect(vi.mocked(api.reserveVehicle).mock.calls[0][1]).toEqual(vi.mocked(api.reserveVehicle).mock.calls[1][1]);
});

it('allows the assigned equipment officer to book directly', async () => {
    roles('EquipmentOfficer');
    vi.mocked(api.getVehicles).mockResolvedValue([{ ...vehicle, canBookDirectly: true, canEdit: true }]);
    const user = userEvent.setup(); render(<VehiclesPage />);
    await user.click(await screen.findByRole('button', { name: 'Florian 19-01 buchen' }));
    await user.click(screen.getByRole('button', { name: 'Direkt buchen' }));
    await waitFor(() => expect(api.reserveVehicle).toHaveBeenCalledWith(vehicle.id, expect.any(Object), true));
});

it('does not offer booking to a viewer or administration to a firefighter', async () => {
    roles('Viewer'); const { unmount } = render(<VehiclesPage />);
    expect(await screen.findByRole('button', { name: 'Florian 19-01 buchen' })).toBeDisabled();
    unmount(); roles('Firefighter'); render(<VehiclesPage management />);
    expect(screen.getByRole('alert')).toHaveTextContent('Keine Berechtigung');
});

it('creates a vehicle with a required call sign and a department-scoped officer', async () => {
    roles('EquipmentOfficer'); vi.mocked(api.saveVehicle).mockResolvedValue(vehicle);
    const user = userEvent.setup(); render(<VehiclesPage management />);
    await waitFor(() => expect(screen.getByRole('button', { name: 'Fahrzeug anlegen' })).toBeEnabled());
    await user.click(screen.getByRole('button', { name: 'Fahrzeug anlegen' }));
    expect(screen.getByLabelText('Funkrufname')).toBeRequired();
    await user.selectOptions(screen.getByLabelText('Fahrzeugtyp'), 'type-1');
    await user.type(screen.getByLabelText('Funkrufname'), 'Florian 19-02');
    await user.click(screen.getByRole('button', { name: 'Fahrzeug speichern' }));
    await waitFor(() => expect(api.saveVehicle).toHaveBeenCalledWith(null, expect.objectContaining({ radioCallSign: 'Florian 19-02', fireDepartmentId: 1, equipmentOfficerId: 'officer', vehicleTypeId: 'type-1' })));
});

it('shows the request comment to the assigned officer and sends an approval decision', async () => {
    roles('EquipmentOfficer');
    vi.mocked(api.getVehicleReservations).mockImplementation(async scope => scope === 'assigned' ? [{ ...reservation, canDecide: true }] : []);
    const user = userEvent.setup(); render(<VehiclesPage />);
    expect(await screen.findByText('Jugendfeuerwehr')).toBeVisible();
    await user.click(screen.getByRole('button', { name: 'Bestätigen' }));
    await waitFor(() => expect(api.decideVehicleReservation).toHaveBeenCalledWith(expect.objectContaining({ id: reservation.id }), 'approve'));
});

it('shows only vehicle master data with a separate actions column in management, without loading a calendar or reservations', async () => {
    roles('EquipmentOfficer');
    vi.mocked(api.getVehicles).mockResolvedValue([{ ...vehicle, name: 'Mannschaftstransporter', licensePlate: 'OH-FF 19', canEdit: true }]);
    render(<VehiclesPage management />);
    await screen.findByText('Florian 19-01');
    const table = screen.getByRole('table', { name: 'Fahrzeuge' });
    expect(within(table).getAllByRole('columnheader').map(header => header.textContent)).toEqual(['Funkrufname', 'Name', 'Kennzeichen', 'Status', 'Aktionen']);
    const row = screen.getByText('Florian 19-01').closest('tr')!;
    expect(within(row).getByText('Mannschaftstransporter')).toBeVisible();
    expect(within(row).getByText('OH-FF 19')).toBeVisible();
    const actions = within(row).getAllByRole('cell')[3];
    expect(within(actions).getByRole('button', { name: 'Florian 19-01 bearbeiten' })).toBeVisible();
    expect(within(actions).getByRole('button', { name: 'Florian 19-01 deaktivieren' })).toBeVisible();
    expect(screen.queryByRole('region', { name: 'MTW-Verfügbarkeit' })).not.toBeInTheDocument();
    expect(screen.queryByRole('heading', { name: 'Meine Reservierungen' })).not.toBeInTheDocument();
    expect(api.getVehicleAvailability).not.toHaveBeenCalled();
    expect(api.getVehicleReservations).not.toHaveBeenCalled();
});

it('opens the existing vehicle editor from the management actions column', async () => {
    roles('EquipmentOfficer');
    vi.mocked(api.getVehicles).mockResolvedValue([{ ...vehicle, licensePlate: 'OH-FF 19', canEdit: true }]);
    vi.mocked(api.saveVehicle).mockResolvedValue(vehicle);
    const user = userEvent.setup(); render(<VehiclesPage management />);
    await user.click(await screen.findByRole('button', { name: 'Florian 19-01 bearbeiten' }));
    expect(screen.getByLabelText('Funkrufname')).toHaveValue(vehicle.radioCallSign);
    expect(screen.getByLabelText('Fahrzeugbezeichnung (optional)')).toHaveValue(vehicle.name);
    expect(screen.getByLabelText('Kennzeichen (optional)')).toHaveValue('OH-FF 19');
    await user.clear(screen.getByLabelText('Fahrzeugbezeichnung (optional)'));
    await user.type(screen.getByLabelText('Fahrzeugbezeichnung (optional)'), 'Neuer Name');
    await user.click(screen.getByRole('button', { name: 'Fahrzeug speichern' }));
    await waitFor(() => expect(api.saveVehicle).toHaveBeenCalledWith(vehicle.id, expect.objectContaining({ name: 'Neuer Name', licensePlate: 'OH-FF 19', expectedVersion: vehicle.version })));
});

it('deactivates a vehicle and lets its officer show and reactivate it using the actions column', async () => {
    roles('EquipmentOfficer');
    let current: Vehicle = { ...vehicle, canEdit: true };
    vi.mocked(api.getVehicles).mockImplementation(async includeInactive => includeInactive || current.isActive ? [current] : []);
    vi.mocked(api.setVehicleActive).mockImplementation(async (_, isActive) => {
        current = { ...current, isActive, version: current.version + 1 }; return current;
    });
    const user = userEvent.setup(); render(<VehiclesPage management />);
    await user.click(await screen.findByRole('button', { name: 'Florian 19-01 deaktivieren' }));
    await screen.findByText('Keine Fahrzeuge vorhanden.');
    await user.click(screen.getByRole('checkbox', { name: 'Deaktivierte Fahrzeuge anzeigen' }));
    const activate = await screen.findByRole('button', { name: 'Florian 19-01 aktivieren' });
    await waitFor(() => expect(activate).toBeEnabled());
    await user.click(activate);
    expect(await screen.findByRole('button', { name: 'Florian 19-01 deaktivieren' })).toBeVisible();
    expect(api.setVehicleActive).toHaveBeenNthCalledWith(1, expect.objectContaining({ id: vehicle.id, version: 1 }), false);
    expect(api.setVehicleActive).toHaveBeenNthCalledWith(2, expect.objectContaining({ id: vehicle.id, version: 2 }), true);
});

it('keeps other officers vehicles visible without offering maintenance actions', async () => {
    roles('EquipmentOfficer'); render(<VehiclesPage management />);
    const row = (await screen.findByText(vehicle.radioCallSign)).closest('tr')!;
    expect(within(row).queryByRole('button')).not.toBeInTheDocument();
    expect(within(row).getAllByRole('cell')[3]).toHaveTextContent('—');
});

it('keeps direct booking available through the assigned officers vehicle call sign without a calendar', async () => {
    roles('EquipmentOfficer');
    vi.mocked(api.getVehicles).mockResolvedValue([{ ...vehicle, canEdit: true, canBookDirectly: true }]);
    const user = userEvent.setup(); render(<VehiclesPage management />);
    await user.click(await screen.findByRole('button', { name: 'Florian 19-01 direkt buchen' }));
    await user.click(screen.getByRole('button', { name: 'Direkt buchen' }));
    await waitFor(() => expect(api.reserveVehicle).toHaveBeenCalledWith(vehicle.id, expect.any(Object), true));
    expect(api.getVehicleAvailability).not.toHaveBeenCalled();
});

it('refreshes the rolling calendar without changing its date range', async () => {
    const user = userEvent.setup(); render(<VehiclesPage />);
    await screen.findByRole('button', { name: 'Florian 19-01 buchen' });
    await waitFor(() => expect(screen.getByRole('button', { name: 'Aktualisieren' })).toBeEnabled());
    const calls = vi.mocked(api.getVehicles).mock.calls.length;
    await user.click(screen.getByRole('button', { name: 'Aktualisieren' }));
    await waitFor(() => expect(api.getVehicles).toHaveBeenCalledTimes(calls + 1));
    expect(api.getVehicleAvailability).toHaveBeenLastCalledWith(range.from, range.to, 'MTW');
});

it('loads the same reusable calendar on the dashboard and opens its booking modal', async () => {
    const user = userEvent.setup(); render(<Dashboard />);
    expect(screen.getByRole('heading', { name: 'Dashboard Klein Parin Pohnsdorf' })).toBeVisible();
    await user.click(await screen.findByRole('button', { name: 'Florian 19-01 buchen' }));
    await user.type(screen.getByLabelText('Kommentar / Buchungsgrund (optional)'), 'Ausbildung');
    await user.click(screen.getByRole('button', { name: 'Reservierung anfragen' }));
    await waitFor(() => expect(api.reserveVehicle).toHaveBeenCalledWith(vehicle.id, expect.objectContaining({ comment: 'Ausbildung' }), false));
    expect(api.getVehicles).toHaveBeenCalledWith(false);
});

it('supports the calendar on its own without loading the vehicle administration', async () => {
    render(<VehicleCalendar />);
    await screen.findByRole('button', { name: 'Florian 19-01 buchen' });
    expect(api.getVehicleManagementOptions).not.toHaveBeenCalled();
    expect(api.getVehicleReservations).not.toHaveBeenCalled();
});

it('filters the MTW overview by vehicle type while administration still lists all vehicles', async () => {
    const elw = { ...vehicle, id: 'elw', radioCallSign: 'ELW 01', vehicleTypeId: 'elw-type', vehicleTypeName: 'ELW' };
    vi.mocked(api.getVehicles).mockResolvedValue([vehicle, elw]);
    const { unmount } = render(<VehiclesPage />);
    await screen.findByRole('button', { name: 'Florian 19-01 buchen' });
    expect(screen.queryByRole('button', { name: 'ELW 01 buchen' })).not.toBeInTheDocument();
    unmount(); roles('EquipmentOfficer'); render(<VehiclesPage management />);
    expect(await screen.findByText('ELW 01')).toBeVisible();
    expect(screen.getByRole('heading', { name: 'Fahrzeug Verwaltung' })).toBeVisible();
});

it('edits the vehicle operational status and shows it in the vehicle table', async () => {
    roles('EquipmentOfficer'); vi.mocked(api.getVehicles).mockResolvedValue([{ ...vehicle, canEdit: true }]);
    vi.mocked(api.saveVehicle).mockResolvedValue(vehicle);
    const user = userEvent.setup(); render(<VehiclesPage management />);
    await user.click(await screen.findByRole('button', { name: 'Florian 19-01 bearbeiten' }));
    expect(screen.getByRole('combobox', { name: 'Status' })).toHaveValue('ready');
    await user.selectOptions(screen.getByRole('combobox', { name: 'Status' }), 'maintenance');
    await user.click(screen.getByRole('button', { name: 'Fahrzeug speichern' }));
    await waitFor(() => expect(api.saveVehicle).toHaveBeenCalledWith(vehicle.id, expect.objectContaining({ vehicleStatusId: 'maintenance' })));
});

it('shows non-bookable vehicles as unavailable and disables their booking entry point', async () => {
    const maintenance = { ...vehicle, vehicleStatusName: 'Wartung', allowsBooking: false };
    vi.mocked(api.getVehicleAvailability).mockResolvedValue([{ vehicle: maintenance, days: [{ date: range.from, status: 'Unavailable' }] }]);
    render(<VehicleCalendar />);
    expect(await screen.findByRole('button', { name: 'Florian 19-01 buchen' })).toBeDisabled();
    expect(screen.getByText('Wartung')).toBeVisible();
    expect(screen.getByRole('table').querySelector('.vehicle-day--Unavailable')).toBeInTheDocument();
});

it.each([
    [new Date(2026, 11, 30, 12), '2026-12-27', '2027-01-19'],
    [new Date(2024, 1, 28, 12), '2024-02-25', '2024-03-19'],
    [new Date(2026, 9, 25, 12), '2026-10-22', '2026-11-14'],
])('keeps 24 calendar dates across year, leap-day and daylight-saving boundaries (%s)', (reference, from, to) => {
    const result = api.vehicleCalendarRange(reference);
    expect(result.from).toBe(from); expect(result.to).toBe(to);
    expect(result.days).toHaveLength(24);
    expect(new Set(result.days).size).toBe(24);
    expect(result.days[3]).toBe(result.today);
});
