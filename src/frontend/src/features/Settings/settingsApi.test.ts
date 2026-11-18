import { beforeEach, expect, it, vi } from 'vitest';
import apiClient from '../../api/apiClient';
import { createSetting, deleteSetting, getSettingDefinitions, getSettings, getSettingsModuleStatuses, updateSetting } from './settingsApi';

vi.mock('../../api/apiClient', () => ({ default: { get: vi.fn(), post: vi.fn(), put: vi.fn(), delete: vi.fn() } }));
beforeEach(() => { vi.resetAllMocks(); });

it('uses the settings endpoints and sends only the value for updates', async () => {
  vi.mocked(apiClient.get).mockResolvedValue({ data: [] });
  vi.mocked(apiClient.post).mockResolvedValue({ data: { id: 'setting-1' } });
  vi.mocked(apiClient.put).mockResolvedValue({ data: { id: 'setting-1' } });
  vi.mocked(apiClient.delete).mockResolvedValue({});
  expect(await getSettings()).toEqual([]);
  expect(await getSettingDefinitions()).toEqual([]);
  expect(await getSettingsModuleStatuses()).toEqual([]);
  await createSetting({ key: 'TEST_NUMBER', value: '1' });
  await updateSetting('setting-1', '2');
  await deleteSetting('setting-1');
  expect(apiClient.get).toHaveBeenCalledWith('/api/Settings');
  expect(apiClient.get).toHaveBeenCalledWith('/api/Settings/definitions');
  expect(apiClient.get).toHaveBeenCalledWith('/api/Settings/modules');
  expect(apiClient.post).toHaveBeenCalledWith('/api/Settings', { key: 'TEST_NUMBER', value: '1' });
  expect(apiClient.put).toHaveBeenCalledWith('/api/Settings/setting-1', { value: '2' });
  expect(apiClient.delete).toHaveBeenCalledWith('/api/Settings/setting-1');
});
