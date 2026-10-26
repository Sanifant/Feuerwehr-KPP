import { afterEach, beforeEach, expect, it, vi } from 'vitest';
import apiClient from './apiClient';
import { createIncident, downloadArchivedIncidentPdf, getActiveIncident, getArchivedDiaryEntries, getIncidentArchive, getMapState } from './incidentApi';

vi.mock('./apiClient', () => ({ default: { get: vi.fn(), post: vi.fn() } }));

const wireIncident = {
    id: 'incident-1', name: 'Wohnungsbrand', keyword: 'B3', status: 1,
    originLongitude: 10, originLatitude: 51, createdAtUtc: '2026-10-26T10:00:00Z',
    alarmedAtUtc: '2026-10-26T08:15:00Z', revision: 1,
};

beforeEach(() => { vi.clearAllMocks(); });
afterEach(() => { vi.restoreAllMocks(); });

it.each([1, 'Active'])('recognizes the active incident when its status arrives as %s', async status => {
    vi.mocked(apiClient.get).mockResolvedValue({ data: { ...wireIncident, status } });
    expect(await getActiveIncident()).toEqual({ ...wireIncident, status: 'Active' });
});

it('returns the created incident from the actual numeric-status API response', async () => {
    vi.mocked(apiClient.post).mockResolvedValue({ data: { incident: wireIncident, lease: {}, serverUtc: wireIncident.createdAtUtc } });
    const payload = {
        name: 'Wohnungsbrand', alarmedAtUtc: '2026-10-26T08:15:00.000Z',
        originLongitude: 10, originLatitude: 51, sessionId: 'session-1', commandId: 'command-1',
    };
    expect((await createIncident(payload)).incident).toEqual({ ...wireIncident, status: 'Active' });
    expect(apiClient.post).toHaveBeenCalledWith('/api/incidents', payload);
});

it('normalizes numeric states for the map and archived incidents too', async () => {
    vi.mocked(apiClient.get).mockResolvedValueOnce({ data: { incidentStatus: 1, incidentId: wireIncident.id } });
    expect((await getMapState(wireIncident.id)).incidentStatus).toBe('Active');
    vi.mocked(apiClient.get).mockResolvedValueOnce({ data: [{ ...wireIncident, status: 2 }] });
    expect((await getIncidentArchive())[0].status).toBe('Closed');
});

it('uses creation time for older saved command responses without an alarm time', async () => {
    vi.mocked(apiClient.get).mockResolvedValue({ data: { ...wireIncident, alarmedAtUtc: undefined } });
    expect((await getActiveIncident()).alarmedAtUtc).toBe(wireIncident.createdAtUtc);
});

it('requests further archive pages and normalizes numeric diary revision types', async () => {
    vi.mocked(apiClient.get).mockResolvedValueOnce({ data: [] });
    await getIncidentArchive(2, 25);
    expect(apiClient.get).toHaveBeenLastCalledWith('/api/incidents/archive', { params: { page: 2, pageSize: 25 } });
    vi.mocked(apiClient.get).mockResolvedValueOnce({ data: [{
        currentRevisionData: { revisionType: 3 }, revisions: [{ revisionType: 1 }, { revisionType: 2 }, { revisionType: 3 }],
    }] });
    const [entry] = await getArchivedDiaryEntries('incident-1');
    expect(apiClient.get).toHaveBeenLastCalledWith('/api/incidents/archive/incident-1/diary');
    expect(entry.currentRevisionData.revisionType).toBe('Canceled');
    expect(entry.revisions.map(revision => revision.revisionType)).toEqual(['Created', 'Edited', 'Canceled']);
});

it('downloads the PDF through the authenticated API client and cleans up the temporary URL', async () => {
    const createUrl = vi.fn(() => 'blob:archive-pdf');
    const revokeUrl = vi.fn();
    Object.defineProperty(URL, 'createObjectURL', { configurable: true, value: createUrl });
    Object.defineProperty(URL, 'revokeObjectURL', { configurable: true, value: revokeUrl });
    const click = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(() => {});
    const pdf = new Blob(['%PDF-1.4'], { type: 'application/pdf' });
    vi.mocked(apiClient.get).mockResolvedValueOnce({ data: pdf });
    try {
        await downloadArchivedIncidentPdf('incident-1');
        expect(apiClient.get).toHaveBeenCalledWith('/api/incidents/archive/incident-1/export.pdf', { responseType: 'blob' });
        expect(createUrl).toHaveBeenCalledWith(pdf);
        const anchor = click.mock.instances[0] as HTMLAnchorElement;
        expect(anchor.download).toBe('einsatztagebuch-incident1.pdf');
        expect(anchor.isConnected).toBe(false);
        expect(revokeUrl).toHaveBeenCalledExactlyOnceWith('blob:archive-pdf');
    } finally {
        Reflect.deleteProperty(URL, 'createObjectURL');
        Reflect.deleteProperty(URL, 'revokeObjectURL');
    }
});
