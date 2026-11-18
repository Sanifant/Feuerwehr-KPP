import { beforeEach, expect, it, vi } from 'vitest';
import apiClient from '../../../api/apiClient';
import { createHydrantInspection, getHydrantInspections } from './hydrantInspectionApi';

vi.mock('../../../api/apiClient', () => ({ default: { get: vi.fn(), post: vi.fn() } }));
beforeEach(() => { vi.resetAllMocks(); });

it('uses the authenticated API client and sends only editable fields', async () => {
  vi.mocked(apiClient.get).mockResolvedValue({ data: [] });
  vi.mocked(apiClient.post).mockResolvedValue({ data: { id: 'inspection' } });
  expect(await getHydrantInspections(42)).toEqual([]);
  expect(await createHydrantInspection(42, { comment: null, newStatus: 1 })).toEqual({ id: 'inspection' });
  expect(apiClient.get).toHaveBeenCalledWith('/api/hydrant/42/inspections');
  expect(apiClient.post).toHaveBeenCalledWith('/api/hydrant/42/inspections', { comment: null, newStatus: 1 });
});
