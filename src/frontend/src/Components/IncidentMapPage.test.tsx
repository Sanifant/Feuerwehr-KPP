import { act, cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, beforeEach, expect, it, vi } from 'vitest';
import Map from 'ol/Map';
import type View from 'ol/View';
import type BaseLayer from 'ol/layer/Base';
import type VectorLayer from 'ol/layer/Vector';
import type VectorSource from 'ol/source/Vector';
import type Feature from 'ol/Feature';
import type Geometry from 'ol/geom/Geometry';
import type Interaction from 'ol/interaction/Interaction';
import type Style from 'ol/style/Style';
import type MapBrowserEvent from 'ol/MapBrowserEvent';
import Point from 'ol/geom/Point';
import Translate from 'ol/interaction/Translate';
import Draw from 'ol/interaction/Draw';
import { fromLonLat, toLonLat } from 'ol/proj';
import IncidentMapPage from './IncidentMapPage';
import { useAuth } from '../AuthContext';
import { useRole } from '../hooks/useRole';
import { getMapSymbolCategories, getMapSymbols } from '../api/mapSymbolApi';
import { deleteMapElement, getActiveIncident, getMapAudit, getMapState, upsertMapElement } from '../api/incidentApi';
import type { EditorLease, IncidentSummary, MapElementDto, MapElementWrite } from '../api/incidentApi';
import { mapSymbolDragType } from './incidentMapDrag';

vi.mock('../AuthContext', () => ({ useAuth: vi.fn() }));
vi.mock('../hooks/useRole', () => ({ useRole: vi.fn() }));
vi.mock('../api/mapSymbolApi', async importOriginal => ({ ...await importOriginal<typeof import('../api/mapSymbolApi')>(), getMapSymbols: vi.fn(), getMapSymbolCategories: vi.fn() }));
vi.mock('../api/incidentLive', () => ({ ensureIncidentConnection: async () => ({ on: vi.fn(), off: vi.fn(), invoke: vi.fn() }) }));
vi.mock('../api/incidentApi', () => ({
    acquireMapLease: vi.fn(), buildCommandId: () => crypto.randomUUID(), deleteMapElement: vi.fn(), getActiveIncident: vi.fn(),
    getMapAudit: vi.fn(), getMapAuditExportUrl: () => '#', getMapPdfExportUrl: () => '#', getMapState: vi.fn(),
    heartbeatMapLease: vi.fn(), releaseMapLease: vi.fn(), upsertMapElement: vi.fn(),
}));

// Only the canvas renderer is replaced. Features, projections, interactions and browser-to-map pixel conversion are real OpenLayers.
vi.mock('ol/Map', async importOriginal => {
    const actual = await importOriginal<typeof import('ol/Map')>();
    const { default: Observable } = await import('ol/Observable');
    const { default: Collection } = await import('ol/Collection');
    return { default: vi.fn(function (options: { target: HTMLElement; view: View; layers: BaseLayer[] }) {
        const events = new Observable();
        const interactions = new Collection<Interaction>();
        const layer = options.layers[2] as VectorLayer<VectorSource<Feature<Geometry>>>;
        return {
            viewport_: options.target,
            getViewport: () => options.target,
            getSize: () => [800, 500], getView: () => options.view,
            getEventPixel: actual.default.prototype.getEventPixel,
            getEventCoordinate: actual.default.prototype.getEventCoordinate,
            getCoordinateFromPixel: (pixel: number[]) => {
                const center = options.view.getCenter()!; const resolution = options.view.getResolution()!;
                return [center[0] + (pixel[0] - 400) * resolution, center[1] - (pixel[1] - 250) * resolution];
            },
            getPixelFromCoordinate: (coordinate: number[]) => {
                const center = options.view.getCenter()!; const resolution = options.view.getResolution()!;
                return [400 + (coordinate[0] - center[0]) / resolution, 250 - (coordinate[1] - center[1]) / resolution];
            },
            addInteraction: (interaction: Interaction) => interactions.push(interaction), getInteractions: () => interactions,
            forEachFeatureAtPixel: (_pixel: number[], callback: (feature: Feature<Geometry>, layer: BaseLayer) => unknown) => {
                const feature = layer.getSource()!.getFeatures()[0]; return feature ? callback(feature, layer) : undefined;
            },
            hasFeatureAtPixel: () => false, on: events.on.bind(events), dispatchEvent: events.dispatchEvent.bind(events),
            setTarget: vi.fn(), dispose: vi.fn(() => events.dispose()),
        };
    }) };
});

const svg = '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 64 64" width="64" height="64"><rect width="64" height="64" fill="red"/></svg>';
const symbol = { id: 'symbol-1', name: 'Löschfahrzeug', category: 'Feuerwehr', svgContent: svg, isActive: true, isVehicle: true };
const active: IncidentSummary = { id: 'incident-1', name: 'Brand', status: 'Active', originLongitude: 10.25, originLatitude: 51.5, revision: 1, createdAtUtc: new Date().toISOString(), alarmedAtUtc: new Date().toISOString() };
let lease: EditorLease;
let stored: MapElementDto[];
let bounds: DOMRect;

beforeEach(() => {
    vi.clearAllMocks();
    vi.spyOn(console, 'error').mockImplementation(() => {});
    Object.defineProperty(HTMLDialogElement.prototype, 'showModal', {
        configurable: true, value(this: HTMLDialogElement) { this.setAttribute('open', ''); },
    });
    Object.defineProperty(HTMLDialogElement.prototype, 'close', {
        configurable: true, value(this: HTMLDialogElement) { this.removeAttribute('open'); },
    });
    sessionStorage.setItem('incident-session-id', 'session-1');
    lease = { incidentId: active.id, sessionId: 'session-1', userId: 'user-1', userDisplayName: 'Ada', leaseToken: 'lease-1', expiresAtUtc: new Date(Date.now() + 120000).toISOString(), lastHeartbeatAtUtc: new Date().toISOString(), isOwnedByCurrentSession: true, isExpired: false };
    stored = [];
    bounds = { left: 200, top: 100, right: 1000, bottom: 600, width: 800, height: 500 } as DOMRect;
    vi.spyOn(HTMLElement.prototype, 'getBoundingClientRect').mockImplementation(() => bounds);
    vi.mocked(useAuth).mockReturnValue({
        accessToken: 'token', refreshToken: 'refresh', user: { id: 'user-1', fullName: 'Ada', email: 'ada@example.org', roles: ['SituationMapEditor'] },
        login: vi.fn(), logout: vi.fn(), error: null, isAuthenticated: true, hasRole: () => true,
    });
    vi.mocked(useRole).mockReturnValue({ canEditSituationMap: true, canViewSituationMap: true } as ReturnType<typeof useRole>);
    vi.mocked(getMapSymbols).mockResolvedValue([symbol, { ...symbol, id: 'symbol-2', name: 'Patient', category: 'Personen', isVehicle: false }]);
    vi.mocked(getMapSymbolCategories).mockResolvedValue(['Feuerwehr', 'Rettungsdienst', 'Personen', 'Objekte']);
    vi.mocked(getActiveIncident).mockResolvedValue(active);
    vi.mocked(getMapAudit).mockImplementation(async () => stored.map((element, index) => ({
        id: `audit-${element.id}`, sequenceNumber: index + 1, occurredAtUtc: element.updatedAtUtc,
        action: 'create', userDisplayName: 'Ada', elementId: element.id,
    })));
    vi.mocked(getMapState).mockImplementation(async () => ({ incidentId: active.id, incidentStatus: 'Active', revision: 1, serverUtc: new Date().toISOString(), lease, elements: stored.map(element => ({ ...element })), hydrants: [] }));
    vi.mocked(upsertMapElement).mockImplementation(async (_id, payload) => {
        const element: MapElementDto = {
            id: payload.elementId ?? 'element-1', incidentId: active.id, elementType: payload.elementType, symbolId: payload.symbolId,
            symbolSvg: svg, category: payload.category, label: payload.label, geometryJson: payload.geometryJson,
            radioCallName: payload.radioCallName, strength: payload.strength, note: payload.note,
            isVehicle: payload.elementType === 'Symbol' && payload.symbolId === symbol.id,
            version: payload.expectedVersion ? payload.expectedVersion + 1 : 1, colorHex: payload.colorHex ?? '#d62828',
            updatedAtUtc: new Date().toISOString(), updatedByDisplayName: 'Ada',
        };
        stored = [element]; return { element };
    });
    vi.mocked(deleteMapElement).mockImplementation(async () => { stored = []; return { deleted: true }; });
});
afterEach(() => {
    cleanup();
    vi.restoreAllMocks();
    Reflect.deleteProperty(HTMLDialogElement.prototype, 'showModal');
    Reflect.deleteProperty(HTMLDialogElement.prototype, 'close');
});

function map() { return vi.mocked(Map).mock.results[0].value as Map; }
function source() { return ((vi.mocked(Map).mock.calls[0][0]!.layers as BaseLayer[])[2] as VectorLayer<VectorSource<Feature<Geometry>>>).getSource()!; }
function translate() { return map().getInteractions().getArray().find(interaction => interaction instanceof Translate) as Translate; }
function drop(id = symbol.id, x = 600, y = 350) {
    const event = new MouseEvent('drop', { bubbles: true, cancelable: true, clientX: x, clientY: y });
    Object.defineProperty(event, 'dataTransfer', { value: { types: [mapSymbolDragType], getData: () => id } });
    fireEvent(screen.getByLabelText('Lagekarte: Symbole und Flächen auswählen und verschieben'), event);
}
function pointer(type: string, coordinate: number[], x: number, y: number) {
    const originalEvent = Object.assign(new MouseEvent(type, { clientX: x, clientY: y, button: 0 }), { pointerId: 1, isPrimary: true });
    return { map: map(), originalEvent, coordinate, pixel: [400, 250], type: type === 'pointercancel' ? 'pointerup' : type,
        activePointers: type === 'pointerup' || type === 'pointercancel' ? [] : [originalEvent],
    } as unknown as MapBrowserEvent<PointerEvent>;
}
async function confirmPlacement(radioCallName = 'Florian 1') {
    const dialog = await screen.findByRole('dialog', { name: 'Symbol platzieren' });
    const user = userEvent.setup();
    await user.type(within(dialog).getByLabelText('Funkrufname *'), radioCallName);
    await user.click(within(dialog).getByRole('button', { name: 'Symbol platzieren' }));
}
async function showAndPlace() {
    render(<IncidentMapPage />);
    await waitFor(() => expect(screen.getByRole('button', { name: 'Löschfahrzeug' })).toBeEnabled());
    drop();
    await confirmPlacement();
    await waitFor(() => expect(source().getFeatures()).toHaveLength(1));
    await waitFor(() => expect(translate().getActive()).toBe(true));
    return source().getFeatures()[0];
}
function move(feature: Feature<Geometry>, destination: number[], x = 620, y = 360, endType = 'pointerup') {
    act(() => {
        const start = (feature.getGeometry() as Point).getCoordinates();
        expect(translate().handleEvent(pointer('pointerdown', start, 600, 350))).toBe(false);
        translate().handleEvent(pointer('pointerdrag', destination, x, y));
        translate().handleEvent(pointer(endType, destination, x, y));
    });
}

it('places the SVG at the drop position in WGS84 and keeps the map instance after filtering', async () => {
    const feature = await showAndPlace();
    const payload = vi.mocked(upsertMapElement).mock.calls[0][1];
    const point = JSON.parse(payload.geometryJson).coordinates;
    expect(point[0]).toBeCloseTo(10.25, 8); expect(point[1]).toBeCloseTo(51.5, 8);
    expect(feature.get('symbolSvg')).toBe(svg);
    expect(feature.getGeometry()).toBeInstanceOf(Point);
    await userEvent.setup().selectOptions(screen.getByLabelText('Symbolkategorie'), 'Personen');
    expect(screen.queryByRole('button', { name: 'Löschfahrzeug' })).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Patient' })).toBeVisible();
    expect(Map).toHaveBeenCalledTimes(1);
});

it('places a non-vehicle without a dialog or any detail inputs and can move it', async () => {
    render(<IncidentMapPage />);
    await waitFor(() => expect(screen.getByRole('button', { name: 'Löschfahrzeug' })).toBeEnabled());
    drop('symbol-2');
    await waitFor(() => expect(source().getFeatures()).toHaveLength(1));
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    const payload = vi.mocked(upsertMapElement).mock.calls[0][1];
    expect(payload.symbolId).toBe('symbol-2');
    expect(payload.radioCallName).toBeUndefined();
    expect(payload.strength).toBeUndefined(); expect(payload.note).toBeUndefined();
    await waitFor(() => expect(translate().getActive()).toBe(true));
    move(source().getFeatures()[0], fromLonLat([10.26, 51.51]));
    await waitFor(() => expect(upsertMapElement).toHaveBeenCalledTimes(2));
    expect(screen.getByRole('heading', { name: 'Elementeigenschaften' })).toBeVisible();
    expect(screen.queryByLabelText(/Funkrufname/)).not.toBeInTheDocument();
    expect(screen.queryByLabelText('Stärke')).not.toBeInTheDocument();
    expect(screen.queryByLabelText('Bemerkung')).not.toBeInTheDocument();
});

it('accounts for viewport offsets and CSS scaling when dropping a catalog symbol', async () => {
    render(<IncidentMapPage />);
    await waitFor(() => expect(screen.getByRole('button', { name: 'Löschfahrzeug' })).toBeEnabled());
    bounds = { left: 200, top: 100, right: 600, bottom: 350, width: 400, height: 250 } as DOMRect;
    const center = map().getView().getCenter()!; const resolution = map().getView().getResolution()!;
    const expected = toLonLat([center[0] + 100 * resolution, center[1] - 50 * resolution]);
    drop(symbol.id, 450, 250);
    await confirmPlacement();
    await waitFor(() => expect(upsertMapElement).toHaveBeenCalledOnce());
    const coordinate = JSON.parse(vi.mocked(upsertMapElement).mock.calls[0][1].geometryJson).coordinates;
    expect(coordinate[0]).toBeCloseTo(expected[0], 8); expect(coordinate[1]).toBeCloseTo(expected[1], 8);
});

it('keeps a completed polygon visible and displays its server audit entry', async () => {
    render(<IncidentMapPage />);
    const button = await screen.findByRole('button', { name: 'Fläche zeichnen' });
    await waitFor(() => expect(button).toBeEnabled());
    await userEvent.setup().click(button);
    const draw = map().getInteractions().getArray().find(interaction => interaction instanceof Draw) as Draw;
    vi.spyOn(draw, 'getMap').mockReturnValue(map());
    act(() => {
        draw.appendCoordinates([[10.25, 51.5], [10.251, 51.5], [10.251, 51.501]].map(coordinate => fromLonLat(coordinate)));
        draw.finishDrawing();
    });
    await waitFor(() => expect(source().getFeatures()).toHaveLength(1));
    expect(source().getFeatures()[0].getGeometry()?.getType()).toBe('Polygon');
    const payload = vi.mocked(upsertMapElement).mock.calls[0][1];
    expect(payload.elementType).toBe('Polygon');
    const ring = JSON.parse(payload.geometryJson).coordinates[0];
    expect(ring[0][0]).toBeCloseTo(10.25, 8);
    expect(ring[0][1]).toBeCloseTo(51.5, 8);
    expect(ring[ring.length - 1]).toEqual(ring[0]);
    expect(await screen.findByRole('cell', { name: 'create' })).toBeVisible();
    expect(screen.getByRole('cell', { name: 'element-1' })).toBeVisible();
});

it('starts catalog dragging with the symbol ID and a cursor centered on the SVG preview', async () => {
    render(<IncidentMapPage />);
    const button = await screen.findByRole('button', { name: 'Löschfahrzeug' });
    await waitFor(() => expect(button).toBeEnabled());
    expect(button).toHaveAttribute('draggable', 'true');
    const transfer = { setData: vi.fn(), setDragImage: vi.fn(), effectAllowed: 'none' };
    const event = new MouseEvent('dragstart', { bubbles: true, cancelable: true });
    Object.defineProperty(event, 'dataTransfer', { value: transfer });
    fireEvent(button, event);
    expect(transfer.setData).toHaveBeenCalledExactlyOnceWith(mapSymbolDragType, symbol.id);
    expect(transfer.setDragImage).toHaveBeenCalledWith(button.querySelector('img'), 24, 24);
    expect(transfer.effectAllowed).toBe('copy');
});

it('moves a placed SVG through the real Translate interaction and retains its feature identity', async () => {
    const feature = await showAndPlace();
    move(feature, fromLonLat([10.26, 51.51]));
    await waitFor(() => expect(upsertMapElement).toHaveBeenCalledTimes(2));
    await waitFor(() => expect(source().getFeatures()[0].get('version')).toBe(2));
    const payload: MapElementWrite = vi.mocked(upsertMapElement).mock.calls[1][1];
    expect(payload.expectedVersion).toBe(1); expect(payload.leaseToken).toBe('lease-1');
    const point = JSON.parse(payload.geometryJson).coordinates;
    expect(point[0]).toBeCloseTo(10.26, 8); expect(point[1]).toBeCloseTo(51.51, 8);
    expect(source().getFeatures()[0]).toBe(feature);
    expect(Map).toHaveBeenCalledTimes(1);
});

it('deletes an element only when the drag is released outside the map', async () => {
    const feature = await showAndPlace();
    move(feature, fromLonLat([10.24, 51.5]), 150, 350);
    await waitFor(() => expect(deleteMapElement).toHaveBeenCalledExactlyOnceWith(active.id, 'element-1', expect.objectContaining({ expectedVersion: 1, leaseToken: 'lease-1' })));
    await waitFor(() => expect(source().getFeatures()).toHaveLength(0));
    expect(upsertMapElement).toHaveBeenCalledTimes(1);
});

it('does not delete a canceled pointer drag even if it ends outside the map', async () => {
    const feature = await showAndPlace(); const original = (feature.getGeometry() as Point).getCoordinates();
    move(feature, fromLonLat([10.24, 51.5]), 150, 350, 'pointercancel');
    expect(deleteMapElement).not.toHaveBeenCalled();
    expect((feature.getGeometry() as Point).getCoordinates()).toEqual(original);
    expect(upsertMapElement).toHaveBeenCalledTimes(1);
});

it('restores the position when a move is rejected by the server', async () => {
    const feature = await showAndPlace(); const original = (feature.getGeometry() as Point).getCoordinates();
    vi.mocked(upsertMapElement).mockRejectedValueOnce({ response: { status: 409 } });
    move(feature, fromLonLat([10.26, 51.51]));
    expect(await screen.findByRole('alert')).toHaveTextContent('Änderung wurde nicht bestätigt und zurückgesetzt');
    expect((feature.getGeometry() as Point).getCoordinates()).toEqual(original);
    expect(deleteMapElement).not.toHaveBeenCalled();
});

it('restores an element when deletion fails and blocks unknown or out-of-map catalog drops', async () => {
    const feature = await showAndPlace(); const original = (feature.getGeometry() as Point).getCoordinates();
    vi.mocked(deleteMapElement).mockRejectedValueOnce({ response: { status: 409 } });
    move(feature, fromLonLat([10.24, 51.5]), 150, 350);
    expect(await screen.findByRole('alert')).toHaveTextContent('Element konnte nicht gelöscht werden');
    expect((feature.getGeometry() as Point).getCoordinates()).toEqual(original);
    drop('unknown'); drop(symbol.id, 50, 50);
    expect(upsertMapElement).toHaveBeenCalledTimes(1);
});

it('blocks placement without the editing lease', async () => {
    lease.sessionId = 'another-session';
    render(<IncidentMapPage />);
    expect(await screen.findByRole('button', { name: 'Löschfahrzeug' })).toBeDisabled();
    drop();
    expect(upsertMapElement).not.toHaveBeenCalled(); expect(deleteMapElement).not.toHaveBeenCalled();
});

it('shows the API validation error after a rejected drop and retains it after refreshing', async () => {
    vi.spyOn(console, 'error').mockImplementation(() => {});
    render(<IncidentMapPage />);
    await waitFor(() => expect(screen.getByRole('button', { name: 'Löschfahrzeug' })).toBeEnabled());
    vi.mocked(upsertMapElement).mockRejectedValueOnce({ isAxiosError: true, response: {
        status: 400, data: { errors: { geometryJson: ['Ungültige Kartenkoordinaten.'] } },
    } });
    drop();
    await confirmPlacement();
    expect(await screen.findByRole('alert')).toHaveTextContent('Ungültige Kartenkoordinaten.');
    expect(within(screen.getByRole('dialog')).getByLabelText('Funkrufname *')).toHaveValue('Florian 1');
    await userEvent.setup().click(screen.getByRole('button', { name: 'Abbrechen' }));
    await userEvent.setup().click(screen.getByRole('button', { name: 'Aktualisieren' }));
    expect(screen.getByRole('alert')).toHaveTextContent('Ungültige Kartenkoordinaten.');
    expect(source().getFeatures()).toHaveLength(0);
    expect(screen.getByText('Keine Einträge')).toBeVisible();
    expect(console.error).toHaveBeenCalledWith('[IncidentMap] mutation.failed', expect.objectContaining({ status: 400 }));
});

it('requires a call name before placing a symbol and stores its strength and note', async () => {
    const user = userEvent.setup();
    render(<IncidentMapPage />);
    await waitFor(() => expect(screen.getByRole('button', { name: 'Löschfahrzeug' })).toBeEnabled());
    drop();
    const dialog = await screen.findByRole('dialog', { name: 'Symbol platzieren' });
    const callName = within(dialog).getByLabelText('Funkrufname *');
    expect(callName).toHaveFocus();
    expect(callName).toBeRequired();
    expect(upsertMapElement).not.toHaveBeenCalled();
    expect(source().getFeatures()).toHaveLength(0);
    await user.type(callName, '   ');
    await user.click(within(dialog).getByRole('button', { name: 'Symbol platzieren' }));
    expect(within(dialog).getByRole('alert')).toHaveTextContent('Bitte einen Funkrufnamen angeben');
    expect(upsertMapElement).not.toHaveBeenCalled();
    await user.clear(callName);
    await user.type(callName, ' Florian 40/1 ');
    await user.type(within(dialog).getByLabelText('Stärke'), '1/8/9');
    await user.type(within(dialog).getByLabelText('Bemerkung'), 'Wasserversorgung');
    await user.click(within(dialog).getByRole('button', { name: 'Symbol platzieren' }));
    await waitFor(() => expect(source().getFeatures()).toHaveLength(1));
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    expect(upsertMapElement).toHaveBeenCalledExactlyOnceWith(active.id, expect.objectContaining({
        radioCallName: 'Florian 40/1', strength: '1/8/9', note: 'Wasserversorgung',
    }));
    const feature = source().getFeatures()[0];
    const text = (feature.getStyle() as Style).getText()!;
    expect(text.getText()).toBe('Florian 40/1');
    expect(text.getOffsetX()).toBe(-24);
    expect(text.getOffsetY()).toBe(28);
    expect(text.getTextAlign()).toBe('left');
    expect(text.getTextBaseline()).toBe('top');
    move(feature, fromLonLat([10.26, 51.51]));
    await waitFor(() => expect(upsertMapElement).toHaveBeenCalledTimes(2));
    expect(vi.mocked(upsertMapElement).mock.calls[1][1]).toMatchObject({
        radioCallName: 'Florian 40/1', strength: '1/8/9', note: 'Wasserversorgung',
    });
    expect(Map).toHaveBeenCalledTimes(1);
});

it.each(['button', 'escape'])('cancels symbol placement with %s without creating an element or audit entry', async method => {
    render(<IncidentMapPage />);
    await waitFor(() => expect(screen.getByRole('button', { name: 'Löschfahrzeug' })).toBeEnabled());
    drop();
    const dialog = await screen.findByRole('dialog');
    if (method === 'button') await userEvent.setup().click(within(dialog).getByRole('button', { name: 'Abbrechen' }));
    else fireEvent(dialog, new Event('cancel', { bubbles: false, cancelable: true }));
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    expect(upsertMapElement).not.toHaveBeenCalled();
    expect(source().getFeatures()).toHaveLength(0);
    expect(screen.getByText('Keine Einträge')).toBeVisible();
});

it('opens the same placement dialog when a selected symbol is placed with a map click', async () => {
    render(<IncidentMapPage />);
    const button = await screen.findByRole('button', { name: 'Löschfahrzeug' });
    await waitFor(() => expect(button).toBeEnabled());
    await userEvent.setup().click(button);
    act(() => map().dispatchEvent({ type: 'singleclick', coordinate: fromLonLat([10.25, 51.5]), pixel: [400, 250] } as MapBrowserEvent<PointerEvent>));
    expect(await screen.findByRole('dialog')).toBeVisible();
    expect(upsertMapElement).not.toHaveBeenCalled();
    await confirmPlacement();
    await waitFor(() => expect(source().getFeatures()).toHaveLength(1));
});

it('retains the entered details and blocks saving if the lease expires while the dialog is open', async () => {
    render(<IncidentMapPage />);
    await waitFor(() => expect(screen.getByRole('button', { name: 'Löschfahrzeug' })).toBeEnabled());
    drop();
    lease.expiresAtUtc = new Date(Date.now() - 1000).toISOString();
    await confirmPlacement();
    const dialog = screen.getByRole('dialog');
    expect(within(dialog).getByLabelText('Funkrufname *')).toHaveValue('Florian 1');
    expect(within(dialog).getByRole('button', { name: 'Symbol platzieren' })).toBeDisabled();
    expect(within(dialog).getByText(/Die Bearbeitungssperre ist nicht mehr gültig/)).toBeVisible();
    expect(upsertMapElement).not.toHaveBeenCalled();
    expect(source().getFeatures()).toHaveLength(0);
});
