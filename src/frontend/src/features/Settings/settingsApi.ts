import apiClient from '../../api/apiClient';

export type SettingType = 0 | 1 | 2;
export const settingTypeLabels: Record<SettingType, string> = { 0: 'Text', 1: 'Zahl', 2: 'E-Mail' };

export interface Setting {
  id: string;
  key: string;
  name: string;
  value: string;
  type: SettingType;
  modifiedOn: string;
  modifiedBy: string | null;
  modifiedByName: string;
}

export interface CreateSetting {
  key: string;
  value: string;
}

export interface SettingDefinition {
  key: string;
  name: string;
  moduleId: string;
  moduleName: string;
  type: SettingType;
  mandatory: boolean;
}

export interface SettingsModuleStatus {
  id: string;
  name: string;
  pagePaths: string[];
  isAvailable: boolean;
  missingParameters: string[];
}

export const getSettings = async () => (await apiClient.get<Setting[]>('/api/Settings')).data;
export const getSettingDefinitions = async () => (await apiClient.get<SettingDefinition[]>('/api/Settings/definitions')).data;
export const getSettingsModuleStatuses = async () => (await apiClient.get<SettingsModuleStatus[]>('/api/Settings/modules')).data;
export const createSetting = async (request: CreateSetting) =>
  (await apiClient.post<Setting>('/api/Settings', request)).data;
export const updateSetting = async (id: string, value: string) =>
  (await apiClient.put<Setting>(`/api/Settings/${id}`, { value })).data;
export const deleteSetting = async (id: string) => { await apiClient.delete(`/api/Settings/${id}`); };
