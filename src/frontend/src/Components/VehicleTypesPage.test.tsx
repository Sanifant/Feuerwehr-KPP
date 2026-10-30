import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeAll, beforeEach, expect, it, vi } from 'vitest';
import VehicleTypesPage from './VehicleTypesPage';
import { useRole } from '../hooks/useRole';
import * as api from '../api/vehicleApi';

vi.mock('../hooks/useRole', () => ({ useRole: vi.fn() }));
vi.mock('../api/vehicleApi', () => ({ getVehicleTypes: vi.fn(), saveVehicleType: vi.fn(), setVehicleTypeActive: vi.fn() }));
const type = { id: 'type-1', name: 'MTW', isActive: true, version: 2 };
beforeAll(() => {
    HTMLDialogElement.prototype.showModal = function () { this.setAttribute('open', ''); };
    HTMLDialogElement.prototype.close = function () { this.removeAttribute('open'); };
});
beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(useRole).mockReturnValue({ isAdmin: true } as ReturnType<typeof useRole>);
    vi.mocked(api.getVehicleTypes).mockResolvedValue([type]);
    vi.mocked(api.saveVehicleType).mockResolvedValue(type);
    vi.mocked(api.setVehicleTypeActive).mockResolvedValue({ ...type, isActive: false });
});

it('allows an administrator to create a vehicle type', async () => {
    const user = userEvent.setup(); render(<VehicleTypesPage />);
    await screen.findByText('MTW');
    await user.click(screen.getByRole('button', { name: 'Fahrzeugtyp anlegen' }));
    await user.type(screen.getByLabelText('Name des Fahrzeugtyps'), 'ELW');
    await user.click(screen.getByRole('button', { name: 'Fahrzeugtyp speichern' }));
    await waitFor(() => expect(api.saveVehicleType).toHaveBeenCalledWith(null, { name: 'ELW', expectedVersion: 1 }));
});

it('renames and deactivates a type using its current version', async () => {
    const user = userEvent.setup(); render(<VehicleTypesPage />);
    await user.click(await screen.findByRole('button', { name: 'MTW bearbeiten' }));
    await user.clear(screen.getByLabelText('Name des Fahrzeugtyps'));
    await user.type(screen.getByLabelText('Name des Fahrzeugtyps'), 'Mannschaftstransportwagen');
    await user.click(screen.getByRole('button', { name: 'Fahrzeugtyp speichern' }));
    await waitFor(() => expect(api.saveVehicleType).toHaveBeenCalledWith(type.id, { name: 'Mannschaftstransportwagen', expectedVersion: 2 }));
    await user.click(screen.getByRole('button', { name: 'MTW deaktivieren' }));
    await waitFor(() => expect(api.setVehicleTypeActive).toHaveBeenCalledWith(type, false));
});

it('keeps the entered name when a duplicate type is rejected', async () => {
    vi.mocked(api.saveVehicleType).mockRejectedValue({ isAxiosError: true, response: { data: { message: 'Fahrzeugtyp existiert bereits.' } } });
    const user = userEvent.setup(); render(<VehicleTypesPage />);
    await screen.findByText('MTW');
    await user.click(screen.getByRole('button', { name: 'Fahrzeugtyp anlegen' }));
    await user.type(screen.getByLabelText('Name des Fahrzeugtyps'), 'MTW');
    await user.click(screen.getByRole('button', { name: 'Fahrzeugtyp speichern' }));
    expect(await screen.findByRole('alert')).toHaveTextContent('existiert bereits');
    expect(screen.getByLabelText('Name des Fahrzeugtyps')).toHaveValue('MTW');
});

it('blocks type administration for non-administrators without loading data', () => {
    vi.mocked(useRole).mockReturnValue({ isAdmin: false } as ReturnType<typeof useRole>);
    render(<VehicleTypesPage />);
    expect(screen.getByRole('alert')).toHaveTextContent('Nur Administratoren');
    expect(api.getVehicleTypes).not.toHaveBeenCalled();
});
