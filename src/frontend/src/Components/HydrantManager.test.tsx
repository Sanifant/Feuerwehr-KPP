import { act, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterAll, afterEach, beforeAll, beforeEach, describe, expect, it, vi } from 'vitest';
import Map from 'ol/Map';
import type View from 'ol/View';
import type VectorSource from 'ol/source/Vector';
import type VectorLayer from 'ol/layer/Vector';
import type Feature from 'ol/Feature';
import type Point from 'ol/geom/Point';
import { fromLonLat } from 'ol/proj';
import { containsCoordinate } from 'ol/extent';
import BaseEvent from 'ol/events/Event';
import type Style from 'ol/style/Style';
import type CircleStyle from 'ol/style/Circle';
import HydrantManager from './HydrantManager';
import HydrantLocationMap from './HydrantLocationMap';
import HydrantOverviewMap from './HydrantOverviewMap';
import type { Hydrant } from './HydrantManager';
import apiClient from '../api/apiClient';
import { useRole } from '../hooks/useRole';
import csvExample from '../assets/hydranten-import.csv?raw';

vi.mock('../api/apiClient', () => ({ default: { get: vi.fn(), post: vi.fn(), put: vi.fn(), delete: vi.fn() } }));
vi.mock('../hooks/useRole', () => ({ useRole: vi.fn() }));
// Keep real OpenLayers features, projection, layers and view; only skip the canvas renderer in jsdom.
vi.mock('ol/Map', async () => {
    const { default: Observable } = await import('ol/Observable');
    return { default: vi.fn(function (options: { view: View }) {
        const events = new Observable();
        return {
            getView: () => options.view,
            getSize: () => [800, 600],
            getFeaturesAtPixel: vi.fn(() => []),
            hasFeatureAtPixel: vi.fn(() => false),
            on: events.on.bind(events),
            dispatchEvent: events.dispatchEvent.bind(events),
            updateSize: vi.fn(),
            setTarget: vi.fn(),
            dispose: vi.fn(() => events.dispose()),
        };
    }) };
});

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

const otherHydrant: Hydrant = {
    ...hydrant, id: 77,
    address: { street: 'Bahnhofstraße', houseNumber: '8', postalCode: '23552', city: 'Lübeck', additionalInfo: 'Hinter dem Tor' },
    latitude: 53.86, longitude: 10.68, nominalDiameter: 100, type: 3, waterSource: 4, status: 2,
    notes: 'Wartung erforderlich',
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
    Reflect.deleteProperty(HTMLElement.prototype, 'scrollIntoView');
});

beforeEach(() => {
    vi.mocked(useRole).mockReturnValue({ isAdmin: false, canManageHydrants: true } as ReturnType<typeof useRole>);
    vi.mocked(apiClient.get).mockResolvedValue({ data: [] });
    vi.mocked(apiClient.post).mockReset().mockResolvedValue({ data: {} });
    vi.mocked(apiClient.put).mockResolvedValue({ data: {} });
    vi.mocked(apiClient.delete).mockResolvedValue({ data: {} });
    Object.defineProperty(HTMLElement.prototype, 'scrollIntoView', { configurable: true, value: vi.fn() });
});

afterEach(() => {
    vi.unstubAllGlobals();
    vi.unstubAllEnvs();
    vi.restoreAllMocks();
});

describe('Hydrant CSV export', () => {
    it('only exposes export to an admin', async () => {
        const { rerender } = render(<HydrantManager />);
        expect(screen.queryByRole('button', { name: 'CSV-Export' })).not.toBeInTheDocument();
        vi.mocked(useRole).mockReturnValue({ isAdmin: true } as ReturnType<typeof useRole>);
        rerender(<HydrantManager />);
        expect(screen.getByRole('button', { name: 'CSV-Export' })).toBeInTheDocument();
        await waitFor(() => expect(apiClient.get).toHaveBeenCalled());
    });

    it('downloads the complete export as a CSV file and prevents duplicate requests', async () => {
        vi.mocked(useRole).mockReturnValue({ isAdmin: true } as ReturnType<typeof useRole>);
        const createObjectURL = vi.fn(() => 'blob:hydranten');
        const revokeObjectURL = vi.fn();
        vi.stubGlobal('URL', class extends URL {
            static createObjectURL = createObjectURL;
            static revokeObjectURL = revokeObjectURL;
        });
        const download = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(function (this: HTMLAnchorElement) {
            expect(this.download).toBe('hydranten.csv');
            expect(this.href).toBe('blob:hydranten');
            expect(this).toBeInTheDocument();
        });
        const blob = new Blob(['id;street\r\n42;Dorfstraße'], { type: 'text/csv' });
        let finishExport!: (value: { data: Blob }) => void;
        vi.mocked(apiClient.get).mockResolvedValueOnce({ data: [] })
            .mockReturnValueOnce(new Promise(resolve => { finishExport = resolve; }));
        const user = userEvent.setup();
        render(<HydrantManager />);
        await user.click(screen.getByRole('button', { name: 'CSV-Export' }));
        expect(apiClient.get).toHaveBeenLastCalledWith('/api/hydrant/export', { responseType: 'blob' });
        expect(screen.getByRole('button', { name: 'CSV wird exportiert…' })).toBeDisabled();
        finishExport({ data: blob });
        await waitFor(() => expect(download).toHaveBeenCalledOnce());
        expect(createObjectURL).toHaveBeenCalledWith(blob);
        expect(document.querySelector('a[download="hydranten.csv"]')).toBeNull();
        expect(screen.getByRole('button', { name: 'CSV-Export' })).toBeEnabled();
        await waitFor(() => expect(revokeObjectURL).toHaveBeenCalledWith('blob:hydranten'), { timeout: 2000 });
    });

    it('shows an export error and allows retrying', async () => {
        vi.mocked(useRole).mockReturnValue({ isAdmin: true } as ReturnType<typeof useRole>);
        vi.mocked(apiClient.get).mockResolvedValueOnce({ data: [] }).mockRejectedValueOnce(new Error('unavailable'));
        const user = userEvent.setup();
        render(<HydrantManager />);
        await user.click(screen.getByRole('button', { name: 'CSV-Export' }));
        expect(await screen.findByRole('alert')).toHaveTextContent('Der CSV-Export ist fehlgeschlagen. Bitte erneut versuchen.');
        expect(screen.getByRole('button', { name: 'CSV-Export' })).toBeEnabled();
    });
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

function mapLayer(index = vi.mocked(Map).mock.calls.length - 1) {
    const calls = vi.mocked(Map).mock.calls;
    const layers = calls[index][0]?.layers;
    if (!Array.isArray(layers)) throw new Error('Expected an array of preview layers');
    return layers[1] as VectorLayer<VectorSource<Feature<Point>>>;
}

function mapSource(index?: number) {
    const source = mapLayer(index).getSource();
    if (!source) throw new Error('Expected a marker source');
    return source;
}

function clickMarkers(ids: number[]) {
    const map = vi.mocked(Map).mock.results[0].value as Map;
    vi.mocked(map.getFeaturesAtPixel).mockReturnValue(ids.map(id => mapSource(0).getFeatureById(id)!));
    act(() => { map.dispatchEvent(Object.assign(new BaseEvent('singleclick'), { pixel: [100, 100] })); });
}

describe('Hydrant directory and overview selection', () => {
    beforeEach(() => {
        vi.mocked(apiClient.get).mockResolvedValue({ data: [hydrant, otherHydrant] });
    });

    it('initially shows only address, nominal diameter and type in each collapsed row', async () => {
        render(<HydrantManager />);
        const summary = await screen.findByRole('button', { name: /Dorfstraße/ });
        expect(summary).toHaveAttribute('aria-expanded', 'false');
        expect(summary).toHaveTextContent('Dorfstraße 12, 23611 Bad Schwartau');
        expect(summary).toHaveTextContent('DN 80');
        expect(summary).toHaveTextContent('Unterflurhydrant');
        const list = screen.getByRole('list', { name: 'Hydranten' });
        expect(within(list).getAllByRole('listitem')).toHaveLength(2);
        expect(within(list).queryByText('Betriebsbereit')).not.toBeInTheDocument();
        expect(within(list).queryByText('Beim Gerätehaus')).not.toBeInTheDocument();
        expect(within(list).queryByText('ID')).not.toBeInTheDocument();
        expect(within(list).queryByRole('button', { name: 'Bearbeiten' })).not.toBeInTheDocument();
        expect(within(list).queryByRole('button', { name: 'Löschen' })).not.toBeInTheDocument();
    });

    it('supports keyboard expansion and collapse, showing only the selected row details', async () => {
        const user = userEvent.setup();
        render(<HydrantManager />);
        const first = await screen.findByRole('button', { name: /Dorfstraße/ });
        first.focus();
        await user.keyboard('{Enter}');
        expect(first).toHaveAttribute('aria-expanded', 'true');
        expect(screen.getByText('Beim Gerätehaus')).toBeVisible();
        const second = screen.getByRole('button', { name: /Bahnhofstraße/ });
        await user.click(second);
        expect(first).toHaveAttribute('aria-expanded', 'false');
        expect(screen.queryByText('Beim Gerätehaus')).not.toBeInTheDocument();
        const details = screen.getByRole('region', { name: /^Details zu Bahnhofstraße/ });
        expect(details).toHaveAttribute('id', second.getAttribute('aria-controls'));
        for (const value of ['77', 'Hinter dem Tor', '53.86', '10.68', 'Zisterne', 'Defekt', 'Wartung erforderlich']) {
            expect(within(details).getByText(value)).toBeVisible();
        }
        expect(within(details).getByRole('button', { name: 'Bearbeiten' })).toBeVisible();
        expect(within(details).getByRole('button', { name: 'Löschen' })).toBeVisible();
        await user.keyboard(' ');
        expect(second).toHaveAttribute('aria-expanded', 'false');
        expect(screen.queryByRole('region', { name: /^Details zu/ })).not.toBeInTheDocument();
    });

    it('opens and scrolls the row selected on the map while retaining all list rows and markers', async () => {
        render(<HydrantManager />);
        const summary = await screen.findByRole('button', { name: /Bahnhofstraße/ });
        clickMarkers([otherHydrant.id]);
        expect(summary).toHaveAttribute('aria-expanded', 'true');
        expect(screen.getByText('Wartung erforderlich')).toBeVisible();
        const scroll = vi.mocked(HTMLElement.prototype.scrollIntoView);
        expect(scroll).toHaveBeenCalledWith({ block: 'nearest', inline: 'nearest' });
        expect(scroll.mock.contexts[scroll.mock.contexts.length - 1]).toBe(summary);
        expect(screen.getAllByRole('listitem')).toHaveLength(2);
        expect(mapSource(0).getFeatures()).toHaveLength(2);
        clickMarkers([]);
        expect(summary).toHaveAttribute('aria-expanded', 'true');
    });

    it('highlights and centers the list selection on the map and clears the highlight on collapse', async () => {
        const user = userEvent.setup();
        render(<HydrantManager />);
        const first = await screen.findByRole('button', { name: /Dorfstraße/ });
        const feature = mapSource(0).getFeatureById(hydrant.id)!;
        expect(feature.getStyle()).toBeNull();
        await user.click(first);
        const style = feature.getStyle() as Style;
        expect((style.getImage() as CircleStyle).getRadius()).toBeGreaterThan(6);
        const map = vi.mocked(Map).mock.results[0].value as Map;
        expect(map.getView().getCenter()).toEqual(fromLonLat([hydrant.longitude, hydrant.latitude]));
        await user.click(screen.getByRole('button', { name: /Bahnhofstraße/ }));
        expect(feature.getStyle()).toBeNull();
        expect(mapSource(0).getFeatureById(otherHydrant.id)!.getStyle()).toBeDefined();
        await user.keyboard(' ');
        expect(mapSource(0).getFeatures().every(marker => marker.getStyle() === null)).toBe(true);
        expect(apiClient.get).toHaveBeenCalledTimes(1);
        expect(Map).toHaveBeenCalledTimes(1);
    });

    it('cycles through overlapping markers on repeated map clicks', async () => {
        vi.mocked(apiClient.get).mockResolvedValue({ data: [hydrant, { ...otherHydrant, latitude: hydrant.latitude, longitude: hydrant.longitude }] });
        render(<HydrantManager />);
        await screen.findByRole('button', { name: /Dorfstraße/ });
        clickMarkers([otherHydrant.id, hydrant.id]);
        expect(screen.getByRole('button', { name: /Dorfstraße/ })).toHaveAttribute('aria-expanded', 'true');
        clickMarkers([otherHydrant.id, hydrant.id]);
        expect(screen.getByRole('button', { name: /Bahnhofstraße/ })).toHaveAttribute('aria-expanded', 'true');
        clickMarkers([otherHydrant.id, hydrant.id]);
        expect(screen.getByRole('button', { name: /Dorfstraße/ })).toHaveAttribute('aria-expanded', 'true');
    });

    it.each([
        { role: 'Admin', canManage: true }, { role: 'Commander', canManage: true },
        { role: 'Firefighter', canManage: false }, { role: 'Viewer', canManage: false },
    ])('shows management actions for $role according to its permissions', async ({ role, canManage }) => {
        vi.mocked(useRole).mockReturnValue({ isAdmin: role === 'Admin', canManageHydrants: canManage } as ReturnType<typeof useRole>);
        const user = userEvent.setup();
        render(<HydrantManager />);
        await user.click(await screen.findByRole('button', { name: /Dorfstraße/ }));
        expect(screen.getByText('Beim Gerätehaus')).toBeVisible();
        for (const name of ['Bearbeiten', 'Löschen', '+ Neuen Hydranten erfassen']) {
            expect(screen.queryByRole('button', { name }) !== null).toBe(canManage);
        }
    });

    it('keeps hydrants with invalid coordinates selectable in the list', async () => {
        vi.mocked(apiClient.get).mockResolvedValue({ data: [hydrant, { ...otherHydrant, latitude: 123, longitude: 456 }] });
        const user = userEvent.setup();
        render(<HydrantManager />);
        await user.click(await screen.findByRole('button', { name: /Bahnhofstraße/ }));
        expect(mapSource(0).getFeatures()).toHaveLength(1);
        expect(mapSource(0).getFeatureById(otherHydrant.id)).toBeNull();
        const details = screen.getByRole('region', { name: /^Details zu Bahnhofstraße/ });
        expect(within(details).getByText(/nicht auf der Karte dargestellt/)).toBeVisible();
        expect(within(details).getByRole('button', { name: 'Bearbeiten' })).toBeEnabled();
        expect(screen.getByText(/^1 Hydranten können/)).toBeVisible();
    });

    it('confirms deletion and removes the selected row and marker after reloading', async () => {
        const confirm = vi.spyOn(window, 'confirm').mockReturnValueOnce(false).mockReturnValueOnce(true);
        vi.mocked(apiClient.get).mockResolvedValueOnce({ data: [hydrant, otherHydrant] }).mockResolvedValueOnce({ data: [otherHydrant] });
        const user = userEvent.setup();
        render(<HydrantManager />);
        await user.click(await screen.findByRole('button', { name: /Dorfstraße/ }));
        await user.click(screen.getByRole('button', { name: 'Löschen' }));
        expect(apiClient.delete).not.toHaveBeenCalled();
        await user.click(screen.getByRole('button', { name: 'Löschen' }));
        await waitFor(() => expect(screen.queryByRole('button', { name: /Dorfstraße/ })).not.toBeInTheDocument());
        expect(confirm).toHaveBeenCalledTimes(2);
        expect(apiClient.delete).toHaveBeenCalledWith('/api/hydrant/42');
        expect(mapSource(0).getFeatures()).toHaveLength(1);
        expect(mapSource(0).getFeatureById(hydrant.id)).toBeNull();
        expect(screen.queryByRole('region', { name: /^Details zu/ })).not.toBeInTheDocument();
    });

    it('keeps selection and updates the map position and details after editing', async () => {
        const updated = { ...hydrant, latitude: 54, notes: 'Neu geprüft' };
        vi.mocked(apiClient.get).mockResolvedValueOnce({ data: [hydrant] }).mockResolvedValueOnce({ data: [updated] });
        const user = userEvent.setup();
        render(<HydrantManager />);
        await user.click(await screen.findByRole('button', { name: /Dorfstraße/ }));
        await user.click(screen.getByRole('button', { name: 'Bearbeiten' }));
        fireEvent.change(screen.getByLabelText('Breitengrad (Lat)'), { target: { value: '54' } });
        fireEvent.change(screen.getByLabelText('Notizen'), { target: { value: 'Neu geprüft' } });
        await user.click(screen.getByRole('button', { name: 'Aktualisieren' }));
        await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
        expect(apiClient.put).toHaveBeenCalledWith('/api/hydrant/42', updated);
        expect(screen.getByRole('button', { name: /Dorfstraße/ })).toHaveAttribute('aria-expanded', 'true');
        expect(screen.getByText('Neu geprüft')).toBeVisible();
        expect(mapSource(0).getFeatureById(hydrant.id)!.getGeometry()!.getCoordinates()).toEqual(fromLonLat([hydrant.longitude, 54]));
    });
});

describe('Hydrant overview map', () => {
    it('uses OpenStreetMap, draws blue dots and initially fits all hydrants', () => {
        render(<HydrantOverviewMap hydrants={[hydrant, otherHydrant]} selectedHydrantId={null} onSelectHydrant={vi.fn()} />);
        const options = vi.mocked(Map).mock.calls[0][0]!;
        if (!Array.isArray(options.layers)) throw new Error('Expected layers');
        const osm = (options.layers[0] as import('ol/layer/Tile').default).getSource() as import('ol/source/OSM').default;
        expect(osm.getUrls()).toEqual(['https://tile.openstreetmap.org/{z}/{x}/{y}.png']);
        expect(osm.getAttributions()).toBeDefined();
        const marker = (mapLayer(0).getStyle() as Style).getImage() as CircleStyle;
        expect(marker.getFill()!.getColor()).toBe('#2563eb');
        expect(mapSource(0).getFeatures()).toHaveLength(2);
        const map = vi.mocked(Map).mock.results[0].value as Map;
        const extent = map.getView().calculateExtent([800, 600]);
        for (const h of [hydrant, otherHydrant]) {
            expect(containsCoordinate(extent, fromLonLat([h.longitude, h.latitude]))).toBe(true);
        }
    });

    it('accepts zero coordinates, excludes invalid positions and updates data without recreating the map', () => {
        vi.stubEnv('VITE_OSM_TILE_URL', 'https://tiles.example.org/{z}/{x}/{y}.png');
        const data = [
            { ...hydrant, latitude: 0, longitude: 0 },
            { ...otherHydrant, latitude: 90 },
            { ...hydrant, id: 78, longitude: 181 },
            { ...hydrant, id: 79, latitude: Number.NaN },
        ];
        const onSelect = vi.fn();
        const { rerender } = render(<HydrantOverviewMap hydrants={data} selectedHydrantId={null} onSelectHydrant={onSelect} />);
        expect(mapSource(0).getFeatures()).toHaveLength(1);
        expect(mapSource(0).getFeatureById(hydrant.id)!.getGeometry()!.getCoordinates()).toEqual(fromLonLat([0, 0]));
        expect(screen.getByText(/^3 Hydranten können/)).toBeVisible();
        const options = vi.mocked(Map).mock.calls[0][0]!;
        if (!Array.isArray(options.layers)) throw new Error('Expected layers');
        const osm = (options.layers[0] as import('ol/layer/Tile').default).getSource() as import('ol/source/OSM').default;
        expect(osm.getUrls()).toEqual(['https://tiles.example.org/{z}/{x}/{y}.png']);
        rerender(<HydrantOverviewMap hydrants={[otherHydrant]} selectedHydrantId={otherHydrant.id} onSelectHydrant={onSelect} />);
        expect(mapSource(0).getFeatureById(hydrant.id)).toBeNull();
        expect(mapSource(0).getFeatureById(otherHydrant.id)!.getStyle()).toBeDefined();
        expect(screen.queryByText(/Hydranten können wegen/)).not.toBeInTheDocument();
        expect(Map).toHaveBeenCalledTimes(1);
        rerender(<HydrantOverviewMap hydrants={[]} selectedHydrantId={null} onSelectHydrant={onSelect} />);
        expect(mapSource(0).getFeatures()).toHaveLength(0);
    });

    it('resizes the map with its container and disposes map resources on unmount', () => {
        const observe = vi.fn();
        const disconnect = vi.fn();
        let resized = () => {};
        vi.stubGlobal('ResizeObserver', vi.fn(function (callback: () => void) {
            resized = callback;
            return { observe, disconnect };
        }));
        const { unmount } = render(<HydrantOverviewMap hydrants={[]} selectedHydrantId={null} onSelectHydrant={vi.fn()} />);
        expect(observe).toHaveBeenCalledWith(screen.getByRole('region', { name: 'Karte der Hydranten' }));
        const map = vi.mocked(Map).mock.results[0].value as Map;
        act(() => resized());
        expect(map.updateSize).toHaveBeenCalled();
        unmount();
        expect(disconnect).toHaveBeenCalledOnce();
        expect(map.setTarget).toHaveBeenCalledWith(undefined);
        expect(map.dispose).toHaveBeenCalledOnce();
    });
});

async function openForm() {
    const user = userEvent.setup();
    render(<HydrantManager />);
    const trigger = await screen.findByRole('button', { name: '+ Neuen Hydranten erfassen' });
    await user.click(trigger);
    return { user, trigger };
}

async function fillForm() {
    const { user } = await openForm();
    await user.type(screen.getByLabelText('Straße'), hydrant.address.street);
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
        expect(Map).toHaveBeenCalledTimes(2); // Overview and the form's location preview.
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
        await user.click(await screen.findByRole('button', { name: /Dorfstraße/ }));
        await user.click(screen.getByRole('button', { name: 'Bearbeiten' }));
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
