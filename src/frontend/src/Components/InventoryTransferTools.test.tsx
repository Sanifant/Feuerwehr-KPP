import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, beforeEach, expect, it, vi } from 'vitest';
import InventoryTransferTools from './InventoryTransferTools';
import { exportInventoryData, importInventoryData } from '../api/inventoryTransferApi';
import type { InventoryTransferKind } from '../api/inventoryTransferApi';

vi.mock('../api/inventoryTransferApi', () => ({ exportInventoryData: vi.fn(), importInventoryData: vi.fn() }));
const refresh = vi.fn().mockResolvedValue(undefined);
const busy = vi.fn();
const createUrl = vi.fn().mockReturnValue('blob:inventory-export');
let downloadFilename = '';

beforeEach(() => {
    vi.clearAllMocks(); downloadFilename = '';
    vi.mocked(exportInventoryData).mockResolvedValue(new Blob(['data']));
    vi.mocked(importInventoryData).mockResolvedValue({ createdCount: 1, updatedCount: 2, errors: [] });
    Object.defineProperty(URL, 'createObjectURL', { configurable: true, value: createUrl });
    Object.defineProperty(URL, 'revokeObjectURL', { configurable: true, value: vi.fn() });
    vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(function (this: HTMLAnchorElement) { downloadFilename = this.download; });
});
afterEach(() => vi.restoreAllMocks());
function show(kind: InventoryTransferKind = 'equipment', canImport = true) {
    render(<InventoryTransferTools kind={kind} canImport={canImport} onImported={refresh} onBusyChange={busy} />);
    return userEvent.setup();
}

it('lets readers export inventory as CSV without offering import controls', async () => {
    const user = show('equipment', false);
    expect(screen.queryByText('Inventar importieren')).not.toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: 'Inventar als CSV exportieren' }));
    await waitFor(() => expect(exportInventoryData).toHaveBeenCalledWith('equipment'));
    expect(downloadFilename).toBe('inventar.csv'); expect(createUrl).toHaveBeenCalled();
});

it.each([['types', 'Gerätetypen', 'geraetetypen.json'], ['categories', 'Inventarkategorien', 'inventarkategorien.json']] as const)('exports %s as JSON', async (kind, label, filename) => {
    const user = show(kind); await user.click(screen.getByRole('button', { name: `${label} als JSON exportieren` }));
    await waitFor(() => expect(downloadFilename).toBe(filename)); expect(exportInventoryData).toHaveBeenCalledWith(kind);
});

it('imports CSV, reports created and replaced records and refreshes the parent page', async () => {
    const user = show(); await user.click(screen.getByText('Inventar importieren', { selector: 'summary' }));
    const file = new File(['barcode;manufacturer\nKPP-001;Test'], 'inventar.csv', { type: 'text/csv' });
    await user.upload(screen.getByLabelText('Importdatei'), file); await user.click(screen.getByRole('button', { name: 'Datei importieren' }));
    await waitFor(() => expect(importInventoryData).toHaveBeenCalledWith('equipment', file));
    expect(await screen.findByRole('status')).toHaveTextContent('1 neu angelegt, 2 aktualisiert'); expect(refresh).toHaveBeenCalledOnce();
});

it('shows validation errors with their CSV line and does not refresh failed imports', async () => {
    vi.mocked(importInventoryData).mockRejectedValue({ isAxiosError: true, response: { data: { createdCount: 0, updatedCount: 0, errors: [{ lineNumber: 3, description: 'Barcode doppelt.' }] } } });
    const user = show(); await user.click(screen.getByText('Inventar importieren', { selector: 'summary' }));
    await user.upload(screen.getByLabelText('Importdatei'), new File(['csv'], 'bad.csv', { type: 'text/csv' }));
    await user.click(screen.getByRole('button', { name: 'Datei importieren' }));
    expect(await screen.findByRole('alert')).toHaveTextContent('Zeile 3: Barcode doppelt.'); expect(refresh).not.toHaveBeenCalled();
});

it('imports category JSON and shows entry numbers for JSON errors', async () => {
    vi.mocked(importInventoryData).mockRejectedValue({ isAxiosError: true, response: { data: { errors: [{ lineNumber: 2, description: 'Kategorie ungültig.' }] } } });
    const user = show('categories'); await user.click(screen.getByText('Inventarkategorien importieren', { selector: 'summary' }));
    const file = new File(['[]'], 'categories.json', { type: 'application/json' });
    await user.upload(screen.getByLabelText('Importdatei'), file); await user.click(screen.getByRole('button', { name: 'Datei importieren' }));
    expect(await screen.findByRole('alert')).toHaveTextContent('Eintrag 2: Kategorie ungültig.'); expect(importInventoryData).toHaveBeenCalledWith('categories', file);
});

it('distinguishes a successful import from a failed page refresh', async () => {
    refresh.mockRejectedValueOnce(new Error('reload failed'));
    const user = show(); await user.click(screen.getByText('Inventar importieren', { selector: 'summary' }));
    await user.upload(screen.getByLabelText('Importdatei'), new File(['csv'], 'ok.csv', { type: 'text/csv' }));
    await user.click(screen.getByRole('button', { name: 'Datei importieren' }));
    expect(await screen.findByRole('status')).toHaveTextContent('Import abgeschlossen');
    expect(await screen.findByRole('alert')).toHaveTextContent('Daten wurden importiert');
});
