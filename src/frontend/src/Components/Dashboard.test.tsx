import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, expect, it, vi } from 'vitest';
import { Dashboard } from './Dashboard';
import { getVehicles } from '../api/vehicleApi';
import type { Vehicle } from '../api/vehicleApi';

vi.mock('../api/vehicleApi', () => ({ getVehicles: vi.fn() }));
vi.mock('./VehicleCalendar', () => ({ default: () => <p>MTW-Kalender</p> }));

const ready: Vehicle = {
    id: 'ready-vehicle', radioCallSign: 'Florian 19-01', fireDepartmentId: 1, fireDepartmentName: 'Feuerwehr A',
    name: 'Mannschaftstransporter', licensePlate: null, equipmentOfficerId: 'officer', equipmentOfficerName: 'Ada',
    isActive: true, version: 1, canEdit: false, canBookDirectly: false, vehicleTypeId: 'mtw', vehicleTypeName: 'MTW',
    vehicleStatusId: 'ready', vehicleStatusName: 'Betriebsbereit', allowsBooking: true, statusChangedAtUtc: '2026-10-30T10:15:00Z',
};
const maintenance: Vehicle = { ...ready, id: 'maintenance-vehicle', radioCallSign: 'Florian 11-01', vehicleTypeId: 'elw', vehicleTypeName: 'ELW',
    fireDepartmentId: 2, fireDepartmentName: 'Feuerwehr B', vehicleStatusId: 'maintenance', vehicleStatusName: 'Wartung', allowsBooking: false };
const deregistered: Vehicle = { ...ready, id: 'deregistered-vehicle', radioCallSign: 'Florian 19-02', vehicleStatusId: 'deregistered', vehicleStatusName: 'Abgemeldet', allowsBooking: false };
beforeEach(() => {
    vi.resetAllMocks(); vi.mocked(getVehicles).mockResolvedValue([ready, maintenance, deregistered]);
});

it('shows call sign, type, department, status and last status change for every vehicle type alongside the MTW calendar', async () => {
    render(<Dashboard />);
    await screen.findByRole('rowheader', { name: ready.radioCallSign });
    const table = screen.getByRole('table', { name: 'Fahrzeugstatus' });
    expect(within(table).getAllByRole('columnheader').map(header => header.textContent)).toEqual([
        'Funkrufname', 'Fahrzeugtyp', 'Feuerwehr', 'Status', 'Letzte Statusänderung',
    ]);
    const row = within(table).getByRole('rowheader', { name: maintenance.radioCallSign }).closest('tr')!;
    expect(within(row).getByText('ELW')).toBeVisible();
    expect(within(row).getByText('Feuerwehr B')).toBeVisible();
    expect(within(row).getByText('Wartung')).toBeVisible();
    expect(row.querySelector('time')).toHaveAttribute('datetime', maintenance.statusChangedAtUtc);
    expect(row.querySelector('time')).toHaveTextContent('30.10.2026');
    expect(screen.getByText('MTW-Kalender')).toBeVisible();
    expect(getVehicles).toHaveBeenCalledWith(false);
});

it('highlights deregistered vehicles in red and maintenance vehicles in yellow, leaving ready vehicles unmarked', async () => {
    render(<Dashboard />);
    const readyRow = (await screen.findByRole('rowheader', { name: ready.radioCallSign })).closest('tr')!;
    const maintenanceRow = screen.getByRole('rowheader', { name: maintenance.radioCallSign }).closest('tr')!;
    const deregisteredRow = screen.getByRole('rowheader', { name: deregistered.radioCallSign }).closest('tr')!;
    expect(readyRow).not.toHaveAttribute('class');
    expect(maintenanceRow).toHaveClass('vehicle-status-overview__row--maintenance');
    expect(deregisteredRow).toHaveClass('vehicle-status-overview__row--deregistered');
});

it('does not invent timestamps or type assignments for existing vehicles with unknown values', async () => {
    vi.mocked(getVehicles).mockResolvedValue([{ ...ready, statusChangedAtUtc: null, vehicleTypeId: null, vehicleTypeName: null }]);
    render(<Dashboard />);
    const row = (await screen.findByRole('rowheader', { name: ready.radioCallSign })).closest('tr')!;
    expect(within(row).getByText('Nicht erfasst')).toBeVisible();
    expect(within(row).getByText('Nicht zugeordnet')).toBeVisible();
    expect(row.querySelector('time')).not.toBeInTheDocument();
});

it('refreshes the status and its timestamp when returning to the dashboard', async () => {
    vi.mocked(getVehicles).mockResolvedValue([ready]); render(<Dashboard />);
    await screen.findByRole('rowheader', { name: ready.radioCallSign });
    vi.mocked(getVehicles).mockResolvedValue([{ ...ready, vehicleStatusName: 'Wartung', statusChangedAtUtc: '2026-10-30T11:20:00Z' }]);
    fireEvent(window, new Event('focus'));
    await waitFor(() => expect(screen.getByText('Wartung')).toBeVisible());
    const row = screen.getByRole('rowheader', { name: ready.radioCallSign }).closest('tr')!;
    expect(row.querySelector('time')).toHaveAttribute('datetime', '2026-10-30T11:20:00Z');
    expect(row).toHaveClass('vehicle-status-overview__row--maintenance');
});

it('keeps the dashboard calendar visible if the status overview fails to load', async () => {
    vi.mocked(getVehicles).mockRejectedValue(new Error('offline')); render(<Dashboard />);
    expect(await screen.findByRole('alert')).toHaveTextContent('Fahrzeugstatus konnte nicht geladen werden');
    expect(screen.getByText('MTW-Kalender')).toBeVisible();
});

it('sorts by status priority by default and offers call-sign sorting', async () => {
    const user = userEvent.setup(); render(<Dashboard />);
    await screen.findByRole('rowheader', { name: ready.radioCallSign });
    const callSigns = () => within(screen.getByRole('table', { name: 'Fahrzeugstatus' })).getAllByRole('rowheader').map(header => header.textContent);
    expect(callSigns()).toEqual([deregistered.radioCallSign, maintenance.radioCallSign, ready.radioCallSign]);
    expect(screen.getByRole('columnheader', { name: 'Status' })).toHaveAttribute('aria-sort', 'ascending');
    await user.selectOptions(screen.getByRole('combobox', { name: 'Sortierung' }), 'callSign');
    expect(callSigns()).toEqual([maintenance.radioCallSign, ready.radioCallSign, deregistered.radioCallSign]);
    expect(screen.getByRole('columnheader', { name: 'Funkrufname' })).toHaveAttribute('aria-sort', 'ascending');
    expect(screen.getByRole('columnheader', { name: 'Status' })).not.toHaveAttribute('aria-sort');
});

it('combines department and vehicle-type filters and can reset both', async () => {
    const user = userEvent.setup(); render(<Dashboard />);
    await screen.findByRole('rowheader', { name: ready.radioCallSign });
    await user.selectOptions(screen.getByRole('combobox', { name: 'Feuerwehr' }), '1');
    expect(screen.queryByRole('rowheader', { name: maintenance.radioCallSign })).not.toBeInTheDocument();
    expect(screen.getAllByRole('rowheader')).toHaveLength(2);
    await user.selectOptions(screen.getByRole('combobox', { name: 'Fahrzeugtyp' }), 'elw');
    expect(screen.queryByRole('rowheader')).not.toBeInTheDocument();
    expect(screen.getByText('Keine Fahrzeuge für die gewählten Filter.')).toBeVisible();
    await user.click(screen.getByRole('button', { name: 'Filter zurücksetzen' }));
    expect(screen.getAllByRole('rowheader')).toHaveLength(3);
    expect(screen.getByRole('combobox', { name: 'Feuerwehr' })).toHaveValue('');
    expect(screen.getByRole('combobox', { name: 'Fahrzeugtyp' })).toHaveValue('');
});

it('can filter specifically for vehicles without a vehicle-type assignment', async () => {
    const unassigned = { ...ready, id: 'unassigned', radioCallSign: 'Florian 19-03', vehicleTypeId: null, vehicleTypeName: null };
    vi.mocked(getVehicles).mockResolvedValue([ready, maintenance, unassigned]);
    const user = userEvent.setup(); render(<Dashboard />);
    await screen.findByRole('rowheader', { name: ready.radioCallSign });
    await user.selectOptions(screen.getByRole('combobox', { name: 'Fahrzeugtyp' }), 'unassigned');
    expect(screen.getAllByRole('rowheader').map(header => header.textContent)).toEqual([unassigned.radioCallSign]);
});

it('retains both filters when status data is refreshed', async () => {
    const user = userEvent.setup(); render(<Dashboard />);
    await screen.findByRole('rowheader', { name: ready.radioCallSign });
    await user.selectOptions(screen.getByRole('combobox', { name: 'Feuerwehr' }), '2');
    await user.selectOptions(screen.getByRole('combobox', { name: 'Fahrzeugtyp' }), 'elw');
    vi.mocked(getVehicles).mockResolvedValue([ready, { ...maintenance, radioCallSign: 'Florian 11-02' }, deregistered]);
    fireEvent(window, new Event('focus'));
    await screen.findByRole('rowheader', { name: 'Florian 11-02' });
    expect(screen.getAllByRole('rowheader')).toHaveLength(1);
    expect(screen.getByRole('combobox', { name: 'Feuerwehr' })).toHaveValue('2');
    expect(screen.getByRole('combobox', { name: 'Fahrzeugtyp' })).toHaveValue('elw');
});

it('keeps additional status values before operational vehicles and sorts call signs within a status', async () => {
    const custom = { ...ready, id: 'custom', radioCallSign: 'Florian 12-01', vehicleStatusName: 'Ausbildung' };
    const laterReady = { ...ready, id: 'later-ready', radioCallSign: 'Florian 19-10' };
    vi.mocked(getVehicles).mockResolvedValue([laterReady, custom, ready, maintenance, deregistered]);
    render(<Dashboard />); await screen.findByRole('rowheader', { name: ready.radioCallSign });
    expect(screen.getAllByRole('rowheader').map(header => header.textContent)).toEqual([
        deregistered.radioCallSign, maintenance.radioCallSign, custom.radioCallSign, ready.radioCallSign, laterReady.radioCallSign,
    ]);
});
