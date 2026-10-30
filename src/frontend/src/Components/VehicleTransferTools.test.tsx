import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, expect, it, vi } from 'vitest';
import VehicleTransferTools from './VehicleTransferTools';
import * as api from '../api/vehicleTransferApi';

vi.mock('../api/vehicleTransferApi', () => ({ exportVehicleData: vi.fn(), importVehicleData: vi.fn() }));
let downloaded: string[] = [];
beforeEach(() => {
    vi.restoreAllMocks(); vi.clearAllMocks(); downloaded = [];
    Object.defineProperty(URL, 'createObjectURL', { configurable: true, value: vi.fn(() => 'blob:vehicles') });
    Object.defineProperty(URL, 'revokeObjectURL', { configurable: true, value: vi.fn() });
    vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(function (this: HTMLAnchorElement) { downloaded.push(this.download); });
    vi.mocked(api.exportVehicleData).mockResolvedValue(new Blob(['CSV']));
    vi.mocked(api.importVehicleData).mockResolvedValue({ createdCount: 1, updatedCount: 2, errors: [] });
});

it.each([['vehicles', 'Fahrzeuge als CSV exportieren', 'fahrzeuge.csv'], ['types', 'Fahrzeugtypen als JSON exportieren', 'fahrzeugtypen.json']] as const)(
    'downloads %s in its portable format', async (kind, label, filename) => {
        const user = userEvent.setup(); render(<VehicleTransferTools kind={kind} disabled={false} onImported={vi.fn()} onBusyChange={vi.fn()} />);
        await user.click(screen.getByRole('button', { name: label }));
        await waitFor(() => expect(api.exportVehicleData).toHaveBeenCalledWith(kind));
        expect(downloaded).toEqual([filename]);
    });

it('uploads the selected vehicle CSV and refreshes the list after success', async () => {
    const refreshed = vi.fn().mockResolvedValue(undefined); const user = userEvent.setup();
    render(<VehicleTransferTools kind="vehicles" disabled={false} onImported={refreshed} onBusyChange={vi.fn()} />);
    await user.click(screen.getByText('Fahrzeuge importieren', { selector: 'summary' }));
    const file = new File(['radioCallSign;fireDepartment;equipmentOfficerEmail'], 'fahrzeuge.csv', { type: 'text/csv' });
    await user.upload(screen.getByLabelText('Importdatei'), file);
    await user.click(screen.getByRole('button', { name: 'Datei importieren' }));
    await waitFor(() => expect(api.importVehicleData).toHaveBeenCalledWith('vehicles', file));
    expect(await screen.findByRole('status')).toHaveTextContent('1 neu angelegt, 2 aktualisiert');
    expect(refreshed).toHaveBeenCalledOnce();
});

it('keeps the file and displays the CSV line when an officer email cannot be resolved', async () => {
    vi.mocked(api.importVehicleData).mockRejectedValue({ isAxiosError: true, response: { data: { errors: [{ lineNumber: 3, description: 'Gerätewart-E-Mail nicht gefunden.' }] } } });
    const refreshed = vi.fn(); const user = userEvent.setup();
    render(<VehicleTransferTools kind="vehicles" disabled={false} onImported={refreshed} onBusyChange={vi.fn()} />);
    await user.click(screen.getByText('Fahrzeuge importieren', { selector: 'summary' }));
    const file = new File(['CSV'], 'fahrzeuge.csv', { type: 'text/csv' });
    const input = screen.getByLabelText('Importdatei') as HTMLInputElement;
    await user.upload(input, file); await user.click(screen.getByRole('button', { name: 'Datei importieren' }));
    expect(await screen.findByRole('alert')).toHaveTextContent('Zeile 3: Gerätewart-E-Mail nicht gefunden.');
    expect(input.files?.[0]).toBe(file); expect(refreshed).not.toHaveBeenCalled();
});

it('rejects oversized files before uploading', async () => {
    const user = userEvent.setup();
    render(<VehicleTransferTools kind="vehicles" disabled={false} onImported={vi.fn()} onBusyChange={vi.fn()} />);
    await user.click(screen.getByText('Fahrzeuge importieren', { selector: 'summary' }));
    const file = new File(['CSV'], 'fahrzeuge.csv', { type: 'text/csv' });
    Object.defineProperty(file, 'size', { value: 6 * 1024 * 1024 });
    await user.upload(screen.getByLabelText('Importdatei'), file); await user.click(screen.getByRole('button', { name: 'Datei importieren' }));
    expect(await screen.findByRole('alert')).toHaveTextContent('höchstens 5 MB');
    expect(api.importVehicleData).not.toHaveBeenCalled();
});
