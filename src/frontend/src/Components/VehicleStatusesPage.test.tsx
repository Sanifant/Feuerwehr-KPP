import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeAll, beforeEach, expect, it, vi } from 'vitest';
import VehicleStatusesPage from './VehicleStatusesPage';
import { useRole } from '../hooks/useRole';
import * as api from '../api/vehicleApi';

vi.mock('../hooks/useRole', () => ({ useRole: vi.fn() }));
vi.mock('../api/vehicleApi', () => ({ getVehicleStatuses: vi.fn(), saveVehicleStatus: vi.fn() }));
const ready = { id: 'ready', name: 'Betriebsbereit', allowsBooking: true, isActive: true, version: 1, isDefault: true };
beforeAll(() => {
    HTMLDialogElement.prototype.showModal = function () { this.setAttribute('open', ''); };
    HTMLDialogElement.prototype.close = function () { this.removeAttribute('open'); };
});
beforeEach(() => {
    vi.clearAllMocks(); vi.mocked(useRole).mockReturnValue({ isAdmin: true } as ReturnType<typeof useRole>);
    vi.mocked(api.getVehicleStatuses).mockResolvedValue([ready]);
    vi.mocked(api.saveVehicleStatus).mockResolvedValue(ready);
});
it('lets an administrator extend the status list and choose whether bookings are permitted', async () => {
    const user = userEvent.setup(); render(<VehicleStatusesPage />); await screen.findByText('Betriebsbereit (Standard)');
    await user.click(screen.getByRole('button', { name: 'Status anlegen' }));
    await user.type(screen.getByLabelText('Name des Status'), 'Reserviert für Ausbildung');
    await user.click(screen.getByRole('checkbox', { name: 'Buchungen erlauben' }));
    await user.click(screen.getByRole('button', { name: 'Status speichern' }));
    await waitFor(() => expect(api.saveVehicleStatus).toHaveBeenCalledWith(null, { name: 'Reserviert für Ausbildung', allowsBooking: true, isActive: true, expectedVersion: 1 }));
});
it('protects the default status from deactivation in the editor', async () => {
    const user = userEvent.setup(); render(<VehicleStatusesPage />);
    await user.click(await screen.findByRole('button', { name: 'Betriebsbereit bearbeiten' }));
    expect(screen.getByRole('checkbox', { name: 'Für neue Zuordnungen verfügbar' })).toBeDisabled();
});
it('denies status administration to non-administrators', () => {
    vi.mocked(useRole).mockReturnValue({ isAdmin: false } as ReturnType<typeof useRole>); render(<VehicleStatusesPage />);
    expect(screen.getByRole('alert')).toHaveTextContent('Nur Administratoren'); expect(api.getVehicleStatuses).not.toHaveBeenCalled();
});
