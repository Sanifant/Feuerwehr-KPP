import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterAll, beforeAll, beforeEach, describe, expect, it, vi } from 'vitest';
import Map from 'ol/Map';
import type View from 'ol/View';
import type VectorSource from 'ol/source/Vector';
import type VectorLayer from 'ol/layer/Vector';
import type Feature from 'ol/Feature';
import type Point from 'ol/geom/Point';
import { fromLonLat } from 'ol/proj';
import HydrantManager from './HydrantManager';
import HydrantLocationMap from './HydrantLocationMap';
import type { Hydrant } from './HydrantManager';
import apiClient from '../api/apiClient';
import { useRole } from '../hooks/useRole';
import csvExample from '../assets/hydranten-import.csv?raw';

vi.mock('../api/apiClient', () => ({ default: { get: vi.fn(), post: vi.fn(), put: vi.fn(), delete: vi.fn() } }));
vi.mock('../hooks/useRole', () => ({ useRole: vi.fn() }));
// Keep real OpenLayers features, projection, layers and view; only skip the canvas renderer in jsdom.
vi.mock('ol/Map', () => ({ default: vi.fn(function (options: { view: View }) {
    return {
        getView: () => options.view,
        updateSize: vi.fn(),
        setTarget: vi.fn(),
        dispose: vi.fn(),
    };
}) }));

const hydrant: Hydrant = {
    id: 42,
    address: { street: 'Dorfstraße', houseNumber: '12', postalCode: '23611', city: 'Bad Schwartau', additionalInfo: '' },
    latitude: 53.929134,
    longitude: 10.638436,
    nominalDiameter: 80,
    type: 1,
    waterSource: 1,
    status: 1,
    notes: 'Beim Gerätehaus',
};

beforeAll(() => {
    // jsdom does not implement the native dialog API.
    Object.defineProperty(HTMLDialogElement.prototype, 'showModal', {
        configurable: true,
        value(this: HTMLDialogElement) { this.setAttribute('open', ''); },
    });
    Object.defineProperty(HTMLDialogElement.prototype, 'close', {
        configurable: true,
        value(this: HTMLDialogElement) { this.removeAttribute('open'); },
    });
});

afterAll(() => {
    Reflect.deleteProperty(HTMLDialogElement.prototype, 'showModal');
    Reflect.deleteProperty(HTMLDialogElement.prototype, 'close');
});

beforeEach(() => {
    vi.mocked(useRole).mockReturnValue({ isAdmin: false } as ReturnType<typeof useRole>);
    vi.mocked(apiClient.get).mockResolvedValue({ data: [] });
    vi.mocked(apiClient.post).mockReset().mockResolvedValue({ data: {} });
    vi.mocked(apiClient.put).mockResolvedValue({ data: {} });
});

describe('Hydrant CSV import', () => {
    async function openImport() {
        vi.mocked(useRole).mockReturnValue({ isAdmin: true } as ReturnType<typeof useRole>);
        const user = userEvent.setup();
        render(<HydrantManager />);
        const trigger = await screen.findByRole('button', { name: 'CSV-Import' });
        await user.click(trigger);
        return { user, trigger };
    }

    it('only exposes import to an admin', async () => {
        const { rerender } = render(<HydrantManager />);
        expect(screen.queryByRole('button', { name: 'CSV-Import' })).not.toBeInTheDocument();
        vi.mocked(useRole).mockReturnValue({ isAdmin: true } as ReturnType<typeof useRole>);
        rerender(<HydrantManager />);
        expect(screen.getByRole('button', { name: 'CSV-Import' })).toBeInTheDocument();
        await waitFor(() => expect(apiClient.get).toHaveBeenCalled());
    });

    it('shows the defined CSV example and a download link in the file selection modal', async () => {
        await openImport();
        expect(screen.getByRole('dialog', { name: 'Hydranten aus CSV importieren' })).toBeInTheDocument();
        expect(screen.getByText(/^street;houseNumber/, { selector: 'code' }).textContent).toBe(csvExample);
        expect(screen.getByRole('link', { name: 'CSV-Beispiel herunterladen' })).toHaveAttribute('download', 'hydranten-import.csv');
        expect(screen.getByRole('button', { name: 'CSV importieren' })).toBeDisabled();
    });

    it('uploads multipart data, shows the imported count and reloads the directory', async () => {
        vi.mocked(apiClient.post).mockResolvedValueOnce({ data: { importedCount: 2, errors: [] } });
        const { user } = await openImport();
        const file = new File([csvExample], 'hydranten.csv', { type: 'text/csv' });
        await user.upload(screen.getByLabelText('CSV-Datei auswählen'), file);
        await user.click(screen.getByRole('button', { name: 'CSV importieren' }));

        expect(await screen.findByRole('status')).toHaveTextContent('2 Hydranten wurden importiert.');
        expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
        const [url, data, config] = vi.mocked(apiClient.post).mock.calls[0];
        expect(url).toBe('/api/hydrant/import');
        expect(data).toBeInstanceOf(FormData);
        expect((data as FormData).get('file')).toBe(file);
        expect(config?.headers).toEqual({ 'Content-Type': undefined });
        expect(apiClient.get).toHaveBeenCalledTimes(2);
    });

    it('shows backend errors with line numbers and keeps the file selected for correction', async () => {
        vi.mocked(apiClient.post).mockRejectedValueOnce({
            isAxiosError: true,
            response: { data: { importedCount: 0, errors: [{ lineNumber: 3, description: 'Pflichtfelder fehlen: city.' }] } },
        });
        const { user } = await openImport();
        const file = new File([csvExample], 'hydranten.csv', { type: 'text/csv' });
        await user.upload(screen.getByLabelText('CSV-Datei auswählen'), file);
        await user.click(screen.getByRole('button', { name: 'CSV importieren' }));
        expect(await screen.findByRole('alert')).toHaveTextContent('Zeile 3: Pflichtfelder fehlen: city.');
        expect(screen.getByRole('dialog')).toBeInTheDocument();
        expect((screen.getByLabelText('CSV-Datei auswählen') as HTMLInputElement).files?.[0]).toBe(file);
        expect(apiClient.get).toHaveBeenCalledTimes(1);
    });

    it('cancels without importing and starts with no selected file when reopened', async () => {
        const { user, trigger } = await openImport();
        await user.upload(screen.getByLabelText('CSV-Datei auswählen'), new File([csvExample], 'data.csv', { type: 'text/csv' }));
        await user.click(screen.getByRole('button', { name: 'Abbrechen' }));
        expect(apiClient.post).not.toHaveBeenCalled();
        expect(trigger).toHaveFocus();
        await user.click(trigger);
        expect(screen.getByRole('button', { name: 'CSV importieren' })).toBeDisabled();
    });

    it('rejects an empty file before uploading', async () => {
        const { user } = await openImport();
        await user.upload(screen.getByLabelText('CSV-Datei auswählen'), new File([], 'empty.csv', { type: 'text/csv' }));
        await user.click(screen.getByRole('button', { name: 'CSV importieren' }));
        expect(await screen.findByRole('alert')).toHaveTextContent('nicht leere CSV-Datei');
        expect(apiClient.post).not.toHaveBeenCalled();
    });
});

function mapSource() {
    const calls = vi.mocked(Map).mock.calls;
    const layers = calls[calls.length - 1][0]?.layers;
    if (!Array.isArray(layers)) throw new Error('Expected an array of preview layers');
    const source = (layers[1] as VectorLayer<VectorSource<Feature<Point>>>).getSource();
    if (!source) throw new Error('Expected a marker source');
    return source;
}

async function openForm() {
    const user = userEvent.setup();
    render(<HydrantManager />);
    const trigger = await screen.findByRole('button', { name: '+ Neuen Hydranten erfassen' });
    await user.click(trigger);
    return { user, trigger };
}

async function fillForm() {
    const { user } = await openForm();
    await user.type(screen.getByLabelText('Straße & Hausnummer'), hydrant.address.street);
    await user.type(screen.getByLabelText('Hausnummer'), hydrant.address.houseNumber);
    await user.type(screen.getByLabelText('PLZ'), hydrant.address.postalCode);
    await user.type(screen.getByLabelText('Ort'), hydrant.address.city);
    await user.type(screen.getByLabelText('Breitengrad (Lat)'), String(hydrant.latitude));
    await user.type(screen.getByLabelText('Längengrad (Lng)'), String(hydrant.longitude));
    return user;
}

describe('Hydrant modal', () => {
    it('opens from Form-title and restores focus and resets the form on cancellation', async () => {
        const user = userEvent.setup();
        render(<HydrantManager />);
        expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
        const trigger = screen.getByRole('button', { name: '+ Neuen Hydranten erfassen' });
        expect(trigger).toHaveAttribute('id', 'Form-title');
        await user.click(trigger);
        expect(screen.getByRole('dialog', { name: /Neuen Hydranten erfassen/ })).toBeInTheDocument();
        expect(document.body.style.overflow).toBe('hidden');
        await user.type(screen.getByLabelText('Ort'), 'Testort');
        await user.click(screen.getByRole('button', { name: 'Abbrechen' }));
        expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
        expect(document.body.style.overflow).toBe('');
        expect(trigger).toHaveFocus();
        await user.click(trigger);
        expect(screen.getByLabelText('Ort')).toHaveValue('');
        expect(screen.getByLabelText('Breitengrad (Lat)')).toHaveValue(null);
    });

    it('handles native Escape cancellation and backdrop clicks', async () => {
        const { user, trigger } = await openForm();
        fireEvent(screen.getByRole('dialog'), new Event('cancel', { cancelable: true }));
        expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
        await user.click(trigger);
        await user.click(screen.getByRole('dialog'));
        expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    });

    it('updates one marker from both coordinate inputs and removes it when an input is cleared', async () => {
        await openForm();
        const source = mapSource();
        expect(source.getFeatures()).toHaveLength(0);
        fireEvent.change(screen.getByLabelText('Breitengrad (Lat)'), { target: { value: '53.929134' } });
        expect(source.getFeatures()).toHaveLength(0);
        fireEvent.change(screen.getByLabelText('Längengrad (Lng)'), { target: { value: '10.638436' } });
        expect(source.getFeatures()).toHaveLength(1);
        expect(source.getFeatures()[0].getGeometry()?.getCoordinates()).toEqual(fromLonLat([10.638436, 53.929134]));
        fireEvent.change(screen.getByLabelText('Längengrad (Lng)'), { target: { value: '11' } });
        expect(source.getFeatures()).toHaveLength(1);
        expect(source.getFeatures()[0].getGeometry()?.getCoordinates()).toEqual(fromLonLat([11, 53.929134]));
        expect(Map).toHaveBeenCalledTimes(1);
        fireEvent.change(screen.getByLabelText('Breitengrad (Lat)'), { target: { value: '' } });
        expect(source.getFeatures()).toHaveLength(0);
    });

    it('posts numeric coordinates and closes the dialog only after saving succeeds', async () => {
        const user = await fillForm();
        await user.click(screen.getByRole('button', { name: 'Speichern' }));
        await waitFor(() => expect(apiClient.post).toHaveBeenCalledWith('/api/hydrant', {
            ...hydrant, id: 0, notes: '',
        }));
        await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
    });

    it('keeps the form and values when saving fails', async () => {
        const alert = vi.spyOn(window, 'alert').mockImplementation(() => {});
        const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {});
        vi.mocked(apiClient.post).mockRejectedValueOnce(new Error('API unavailable'));
        const user = await fillForm();
        await user.click(screen.getByRole('button', { name: 'Speichern' }));
        await waitFor(() => expect(alert).toHaveBeenCalled());
        expect(screen.getByRole('dialog')).toBeInTheDocument();
        expect(screen.getByLabelText('Breitengrad (Lat)')).toHaveValue(hydrant.latitude);
        alert.mockRestore();
        consoleError.mockRestore();
    });

    it('opens editing in the same dialog with a marker and retains the PUT endpoint', async () => {
        vi.mocked(apiClient.get).mockResolvedValue({ data: [hydrant] });
        const user = userEvent.setup();
        render(<HydrantManager />);
        await user.click(await screen.findByRole('button', { name: 'Edit' }));
        expect(screen.getByRole('dialog', { name: /Hydrant bearbeiten/ })).toBeInTheDocument();
        expect(mapSource().getFeatures()[0].getGeometry()?.getCoordinates())
            .toEqual(fromLonLat([hydrant.longitude, hydrant.latitude]));
        await user.click(screen.getByRole('button', { name: 'Aktualisieren' }));
        await waitFor(() => expect(apiClient.put).toHaveBeenCalledWith('/api/hydrant/42', hydrant));
    });

    it('does not save out-of-range coordinates or display a misleading marker', async () => {
        await fillForm();
        fireEvent.change(screen.getByLabelText('Breitengrad (Lat)'), { target: { value: '91' } });
        fireEvent.submit(screen.getByRole('form', { name: 'Hydrantendaten' }));
        expect(apiClient.post).not.toHaveBeenCalled();
        expect(mapSource().getFeatures()).toHaveLength(0);
        expect(screen.getByRole('status')).toHaveTextContent('Bitte gültige Koordinaten');
    });
});

describe('Hydrant location map', () => {
    it('accepts zero coordinates and explains coordinates outside the tile projection', () => {
        const { rerender, unmount } = render(<HydrantLocationMap latitude={0} longitude={0} />);
        expect(mapSource().getFeatures()[0].getGeometry()?.getCoordinates()).toEqual(fromLonLat([0, 0]));
        rerender(<HydrantLocationMap latitude={90} longitude={0} />);
        expect(mapSource().getFeatures()).toHaveLength(0);
        expect(screen.getByRole('status')).toHaveTextContent('außerhalb des darstellbaren Kartenbereichs');
        const results = vi.mocked(Map).mock.results;
        const map = results[results.length - 1].value;
        unmount();
        expect(map.setTarget).toHaveBeenCalledWith(undefined);
        expect(map.dispose).toHaveBeenCalledOnce();
    });
});
