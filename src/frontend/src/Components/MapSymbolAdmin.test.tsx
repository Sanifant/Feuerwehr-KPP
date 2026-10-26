import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, beforeEach, expect, it, vi } from 'vitest';
import MapSymbolAdmin from './MapSymbolAdmin';
import { useRole } from '../hooks/useRole';
import { createMapSymbolCategory, getMapSymbolCategories, getMapSymbols, saveMapSymbol } from '../api/mapSymbolApi';
import type { MapSymbol } from '../api/mapSymbolApi';
import { clearMapSymbolDiagnostics, getMapSymbolDiagnostics } from '../api/mapSymbolDiagnostics';
import MapSymbolErrorBoundary from './MapSymbolErrorBoundary';
import MapSymbolDiagnostics from './MapSymbolDiagnostics';

vi.mock('../hooks/useRole', () => ({ useRole: vi.fn() }));
vi.mock('../api/mapSymbolApi', async importOriginal => ({ ...await importOriginal<typeof import('../api/mapSymbolApi')>(), createMapSymbolCategory: vi.fn(), getMapSymbolCategories: vi.fn(), getMapSymbols: vi.fn(), saveMapSymbol: vi.fn() }));
const svg = '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 64 64"><path d="M0 0L64 64"/></svg>';
let symbols: MapSymbol[];

beforeEach(() => {
    vi.clearAllMocks();
    clearMapSymbolDiagnostics();
    vi.spyOn(console, 'info').mockImplementation(() => {});
    vi.spyOn(console, 'warn').mockImplementation(() => {});
    vi.spyOn(console, 'error').mockImplementation(() => {});
    symbols = [];
    vi.mocked(useRole).mockReturnValue({ isAdmin: true } as ReturnType<typeof useRole>);
    vi.mocked(getMapSymbols).mockImplementation(async () => symbols);
    vi.mocked(getMapSymbolCategories).mockResolvedValue(['Feuerwehr', 'Rettungsdienst', 'Personen', 'Objekte']);
    vi.mocked(saveMapSymbol).mockImplementation(async (id, payload) => { const symbol = { id: id ?? 'symbol-1', ...payload }; symbols = [symbol]; return symbol; });
    Object.defineProperty(HTMLElement.prototype, 'scrollIntoView', { configurable: true, value: vi.fn() });
});
afterEach(() => { vi.restoreAllMocks(); Reflect.deleteProperty(HTMLElement.prototype, 'scrollIntoView'); });

function svgFile(size?: number) {
    const file = new File([size ? 'x'.repeat(size) : svg], 'lf10.svg', { type: 'image/svg+xml' });
    Object.defineProperty(file, 'text', { value: async () => svg });
    return file;
}

it('blocks access without admin rights and does not fetch the catalog', () => {
    vi.mocked(useRole).mockReturnValue({ isAdmin: false } as ReturnType<typeof useRole>);
    render(<MapSymbolAdmin />);
    expect(screen.getByRole('alert')).toHaveTextContent('Nur für Administratoren');
    expect(getMapSymbols).not.toHaveBeenCalled();
});

it('uploads an SVG into the selected category and displays the saved image', async () => {
    const user = userEvent.setup(); render(<MapSymbolAdmin />);
    await screen.findByRole('heading', { name: 'Feuerwehr' });
    await user.upload(screen.getByLabelText('SVG-Datei *'), svgFile());
    await user.selectOptions(screen.getByLabelText('Kategorie *'), 'Rettungsdienst');
    await user.click(screen.getByRole('button', { name: 'Symbol speichern' }));
    expect(await screen.findByRole('status')).toHaveTextContent('Symbol gespeichert');
    expect(saveMapSymbol).toHaveBeenCalledExactlyOnceWith(null, { name: 'lf10', category: 'Rettungsdienst', svgContent: svg, isActive: true, isVehicle: false });
    await waitFor(() => expect(screen.getByRole('button', { name: 'lf10 bearbeiten' })).toBeVisible());
    expect(document.querySelector('img')?.src).toMatch(/^data:image\/svg\+xml/);
});

it('lets an admin rename, recategorize and deactivate a symbol without uploading a replacement', async () => {
    symbols = [{ id: 'symbol-1', name: 'LF 10', category: 'Feuerwehr', svgContent: svg, isActive: true, isVehicle: true }];
    const user = userEvent.setup(); render(<MapSymbolAdmin />);
    await user.click(await screen.findByRole('button', { name: 'LF 10 bearbeiten' }));
    expect(screen.getByRole('checkbox', { name: 'Fahrzeug (Funkrufname erforderlich)' })).toBeChecked();
    await user.click(screen.getByRole('checkbox', { name: 'Fahrzeug (Funkrufname erforderlich)' }));
    await user.clear(screen.getByLabelText('Name *')); await user.type(screen.getByLabelText('Name *'), 'Objekt');
    await user.selectOptions(screen.getByLabelText('Kategorie *'), 'Objekte');
    await user.click(screen.getByRole('checkbox', { name: 'Aktiv' }));
    await user.click(screen.getByRole('button', { name: 'Symbol speichern' }));
    expect(await screen.findByRole('status')).toHaveTextContent('Symbol gespeichert');
    expect(saveMapSymbol).toHaveBeenCalledWith('symbol-1', { name: 'Objekt', category: 'Objekte', svgContent: svg, isActive: false, isVehicle: false });
    expect(await screen.findByText('Deaktiviert')).toBeVisible();
});

it('creates a category and immediately uses it for a vehicle symbol', async () => {
    vi.mocked(createMapSymbolCategory).mockResolvedValue('Technische Hilfe');
    const user = userEvent.setup(); render(<MapSymbolAdmin />);
    await screen.findByRole('heading', { name: 'Feuerwehr' });
    await user.type(screen.getByLabelText('Kategoriename *'), ' Technische Hilfe ');
    await user.click(screen.getByRole('button', { name: 'Kategorie hinzufügen' }));
    expect(await screen.findByRole('status')).toHaveTextContent('Kategorie hinzugefügt');
    expect(createMapSymbolCategory).toHaveBeenCalledExactlyOnceWith('Technische Hilfe');
    expect(screen.getByLabelText('Kategorie *')).toHaveValue('Technische Hilfe');
    await user.upload(screen.getByLabelText('SVG-Datei *'), svgFile());
    await user.click(screen.getByRole('checkbox', { name: 'Fahrzeug (Funkrufname erforderlich)' }));
    await user.click(screen.getByRole('button', { name: 'Symbol speichern' }));
    await waitFor(() => expect(saveMapSymbol).toHaveBeenCalledWith(null, expect.objectContaining({ category: 'Technische Hilfe', isVehicle: true })));
});

it('validates category names and keeps input after a duplicate-category error', async () => {
    const user = userEvent.setup(); render(<MapSymbolAdmin />);
    await screen.findByRole('heading', { name: 'Feuerwehr' });
    await user.click(screen.getByRole('button', { name: 'Kategorie hinzufügen' }));
    expect(screen.getByRole('alert')).toHaveTextContent('Kategorienamen');
    expect(createMapSymbolCategory).not.toHaveBeenCalled();
    vi.mocked(createMapSymbolCategory).mockRejectedValueOnce({ isAxiosError: true, response: { data: { title: 'Diese Symbolkategorie existiert bereits.' } } });
    await user.type(screen.getByLabelText('Kategoriename *'), 'Feuerwehr');
    await user.click(screen.getByRole('button', { name: 'Kategorie hinzufügen' }));
    expect(await screen.findByRole('alert')).toHaveTextContent('existiert bereits');
    expect(screen.getByLabelText('Kategoriename *')).toHaveValue('Feuerwehr');
});

it('rejects oversized files before uploading and retains input when the server rejects an SVG', async () => {
    const user = userEvent.setup(); render(<MapSymbolAdmin />);
    await screen.findByRole('heading', { name: 'Feuerwehr' });
    await user.upload(screen.getByLabelText('SVG-Datei *'), svgFile(262145));
    expect(await screen.findByRole('alert')).toHaveTextContent('höchstens 256 KB');
    expect(saveMapSymbol).not.toHaveBeenCalled();
    await user.upload(screen.getByLabelText('SVG-Datei *'), svgFile());
    vi.mocked(saveMapSymbol).mockRejectedValueOnce({ isAxiosError: true, response: { data: { title: 'SVG enthält nicht unterstützte aktive Inhalte.' } } });
    await user.click(screen.getByRole('button', { name: 'Symbol speichern' }));
    expect(await screen.findByRole('alert')).toHaveTextContent('aktive Inhalte');
    expect(screen.getByLabelText('Name *')).toHaveValue('lf10');
});

it('rejects an explicitly selected PNG immediately without reading or uploading it and keeps the page usable', async () => {
    const user = userEvent.setup({ applyAccept: false }); render(<MapSymbolAdmin />);
    await screen.findByRole('heading', { name: 'Feuerwehr' });
    const png = new File([new Uint8Array([137, 80, 78, 71])], 'fahrzeug.png', { type: 'image/png' });
    const read = vi.fn(); Object.defineProperty(png, 'text', { value: read });
    await user.upload(screen.getByLabelText('SVG-Datei *'), png);
    expect(screen.getByRole('alert')).toHaveTextContent('PNG-, JPG- und andere Bilddateien werden nicht unterstützt');
    expect(screen.getByRole('heading', { name: 'Lagekartensymbole verwalten' })).toBeVisible();
    expect(read).not.toHaveBeenCalled(); expect(saveMapSymbol).not.toHaveBeenCalled();
    expect((screen.getByLabelText('SVG-Datei *') as HTMLInputElement).files).toHaveLength(0);
    expect(getMapSymbolDiagnostics()).toContainEqual(expect.objectContaining({ step: 'file.rejected', details: expect.objectContaining({ reason: 'format', type: 'image/png' }) }));
    await user.upload(screen.getByLabelText('SVG-Datei *'), svgFile());
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: 'Symbol speichern' }));
    expect(await screen.findByRole('status')).toHaveTextContent('Symbol gespeichert');
});

it('rejects a PNG renamed to .svg using its MIME type', async () => {
    const user = userEvent.setup({ applyAccept: false }); render(<MapSymbolAdmin />);
    await screen.findByRole('heading', { name: 'Feuerwehr' });
    await user.upload(screen.getByLabelText('SVG-Datei *'), new File(['PNG'], 'fake.svg', { type: 'image/png' }));
    expect(screen.getByRole('alert')).toHaveTextContent('Bitte eine SVG-Datei auswählen');
    expect(saveMapSymbol).not.toHaveBeenCalled();
});

it('records a file-read failure and shows an error without starting an upload', async () => {
    const user = userEvent.setup(); render(<MapSymbolAdmin />);
    await screen.findByRole('heading', { name: 'Feuerwehr' });
    const file = new File([svg], 'broken.svg', { type: 'image/svg+xml' });
    Object.defineProperty(file, 'text', { value: async () => { throw new Error('Read failed'); } });
    await user.upload(screen.getByLabelText('SVG-Datei *'), file);
    await user.click(screen.getByRole('button', { name: 'Symbol speichern' }));
    expect(await screen.findByRole('alert')).toHaveTextContent('Datei konnte nicht gelesen werden');
    expect(saveMapSymbol).not.toHaveBeenCalled();
    expect(getMapSymbolDiagnostics()).toContainEqual(expect.objectContaining({ step: 'file.read.failed', details: expect.objectContaining({ message: 'Read failed' }) }));
    expect(screen.getByRole('button', { name: 'Symbol speichern' })).toBeEnabled();
});

it('reports a stopped API after returning from the file picker', async () => {
    render(<MapSymbolAdmin />);
    await screen.findByRole('heading', { name: 'Feuerwehr' });
    vi.mocked(getMapSymbolCategories).mockRejectedValueOnce(new Error('Network Error'));
    fireEvent(window, new Event('focus'));
    expect(await screen.findByRole('alert')).toHaveTextContent('Debug-Lauf in Visual Studio');
    expect(getMapSymbolDiagnostics()).toContainEqual(expect.objectContaining({ step: 'api.connection.failed' }));
});

it('keeps diagnostic output available after a rendering failure and allows retrying the component', async () => {
    let broken = true;
    function Child() { if (broken) throw new Error('Render failed'); return <p>Recovered</p>; }
    const user = userEvent.setup();
    render(<><MapSymbolErrorBoundary><Child /></MapSymbolErrorBoundary><MapSymbolDiagnostics /></>);
    expect(screen.getByRole('alert')).toHaveTextContent('unerwarteten Fehlers');
    expect(screen.getByText(/Debug-Diagnose \(/)).toBeVisible();
    expect(getMapSymbolDiagnostics()).toContainEqual(expect.objectContaining({ step: 'render.failed' }));
    broken = false;
    await user.click(screen.getByRole('button', { name: 'Erneut versuchen' }));
    expect(screen.getByText('Recovered')).toBeVisible();
});

it('records upload status and trace ID without recording the SVG, tokens or Axios request config', async () => {
    const user = userEvent.setup(); render(<MapSymbolAdmin />);
    await screen.findByRole('heading', { name: 'Feuerwehr' });
    await user.upload(screen.getByLabelText('SVG-Datei *'), svgFile());
    vi.mocked(saveMapSymbol).mockRejectedValueOnce({
        isAxiosError: true, message: 'Request failed', config: { headers: { Authorization: 'Bearer secret-token' }, data: svg },
        response: { status: 500, data: { title: 'Speichern fehlgeschlagen', traceId: 'trace-1' } },
    });
    await user.click(screen.getByRole('button', { name: 'Symbol speichern' }));
    expect(await screen.findByRole('alert')).toHaveTextContent('Speichern fehlgeschlagen');
    const log = getMapSymbolDiagnostics();
    expect(log).toContainEqual(expect.objectContaining({ step: 'upload.failed', details: expect.objectContaining({ status: 500, traceId: 'trace-1' }) }));
    expect(JSON.stringify(log)).not.toContain('secret-token'); expect(JSON.stringify(log)).not.toContain('<svg');
    expect(localStorage.getItem('feuerwehr.map-symbol-diagnostics')).toContain('upload.failed');
});
