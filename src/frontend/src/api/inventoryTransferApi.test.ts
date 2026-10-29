import { beforeEach, expect, it, vi } from 'vitest';
import apiClient from './apiClient';
import { exportInventoryData, importInventoryData } from './inventoryTransferApi';

vi.mock('./apiClient', () => ({ default: { get: vi.fn(), post: vi.fn() } }));
beforeEach(() => vi.clearAllMocks());

it('sends the file as multipart with a browser-generated boundary', async () => {
    const result = { createdCount: 1, updatedCount: 1, errors: [] };
    vi.mocked(apiClient.post).mockResolvedValue({ data: result });
    const file = new File(['csv'], 'inventar.csv', { type: 'text/csv' });
    expect(await importInventoryData('equipment', file)).toEqual(result);
    const [url, form, config] = vi.mocked(apiClient.post).mock.calls[0];
    expect(url).toBe('/api/inventory/import'); expect((form as FormData).get('file')).toBe(file);
    expect(config?.headers?.['Content-Type']).toBeUndefined();
});

it('downloads category JSON as a blob from its dedicated endpoint', async () => {
    const blob = new Blob(['[]'], { type: 'application/json' }); vi.mocked(apiClient.get).mockResolvedValue({ data: blob });
    expect(await exportInventoryData('categories')).toBe(blob);
    expect(apiClient.get).toHaveBeenCalledWith('/api/inventory/categories/export', { responseType: 'blob' });
});
