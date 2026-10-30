import { beforeEach, expect, it, vi } from 'vitest';
import apiClient from './apiClient';
import { exportVehicleData, importVehicleData } from './vehicleTransferApi';

vi.mock('./apiClient', () => ({ default: { get: vi.fn(), post: vi.fn() } }));
beforeEach(() => { vi.clearAllMocks(); });
it('uploads vehicles as multipart form data without forcing a JSON content type', async () => {
    vi.mocked(apiClient.post).mockResolvedValue({ data: { createdCount: 1, updatedCount: 0, errors: [] } });
    const file = new File(['CSV'], 'fahrzeuge.csv');
    await importVehicleData('vehicles', file);
    const [path, data, options] = vi.mocked(apiClient.post).mock.calls[0];
    expect(path).toBe('/api/vehicles/import'); expect((data as FormData).get('file')).toBe(file);
    expect(options?.headers?.['Content-Type']).toBeUndefined();
});
it('downloads the vehicle-type export as a blob', async () => {
    const blob = new Blob(['[]']); vi.mocked(apiClient.get).mockResolvedValue({ data: blob });
    expect(await exportVehicleData('types')).toBe(blob);
    expect(apiClient.get).toHaveBeenCalledWith('/api/vehicles/types/export', { responseType: 'blob' });
});
