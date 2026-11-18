import apiClient from '../../api/apiClient';

export type SettingType = 0 | 1 | 2;
export const settingTypeLabels: Record<SettingType, string> = { 0: 'Text', 1: 'Zahl', 2: 'E-Mail' };

export interface Setting {
  id: string;
  name: string;
  value: string;
  type: SettingType;
  modifiedOn: string;
  modifiedBy: string | null;
  modifiedByName: string;
}

export interface CreateSetting {
  name: string;
  value: string;
  type: SettingType;
}

export const getSettings = async () => (await apiClient.get<Setting[]>('/api/Settings')).data;
export const createSetting = async (request: CreateSetting) =>
  (await apiClient.post<Setting>('/api/Settings', request)).data;
export const updateSetting = async (id: string, value: string) =>
  (await apiClient.put<Setting>(`/api/Settings/${id}`, { value })).data;
export const deleteSetting = async (id: string) => { await apiClient.delete(`/api/Settings/${id}`); };
