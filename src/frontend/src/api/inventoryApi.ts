import apiClient from './apiClient';

export type EquipmentStatus = 'Ready' | 'Defective' | 'InRepair';
export const equipmentStatusLabels: Record<EquipmentStatus, string> = { Ready: 'Einsatzklar', Defective: 'Defekt', InRepair: 'In Reparatur' };
export interface InventoryCategory { id: string; name: string }
export interface EquipmentType {
    id: string; categoryId: string; categoryName: string; name: string;
    inspectionIntervalMonths: number; inspectionInstructions: string; version: number;
}
export interface Equipment {
    id: string; barcode: string; equipmentTypeId: string; equipmentTypeName: string; categoryName: string;
    serialNumber: string; manufacturer: string; status: EquipmentStatus; isActive: boolean;
    createdOn: string; lastInspectedOn: string | null; nextInspectionOn: string;
    inspectionIntervalMonths: number; inspectionInstructions: string; version: number;
}
export interface EquipmentInspection {
    id: string; inspectedOn: string; recordedAtUtc: string; inspectorName: string; comment: string | null;
    status: EquipmentStatus; equipmentTypeName: string; inspectionIntervalMonths: number; inspectionInstructions: string;
}
export interface EquipmentDetail { equipment: Equipment; inspections: EquipmentInspection[] }
export interface SaveEquipment {
    equipmentTypeId: string; serialNumber: string; manufacturer: string; status: EquipmentStatus; expectedVersion: number;
}
export interface SaveEquipmentType {
    categoryId: string; name: string; inspectionIntervalMonths: number; inspectionInstructions: string; expectedVersion: number;
}
export interface RecordInspection {
    commandId: string; inspectedOn: string; status: EquipmentStatus; comment: string; expectedVersion: number;
}

export const getInventory = async (includeInactive: boolean) => (await apiClient.get<Equipment[]>('/api/inventory', { params: { includeInactive } })).data;
export const getInventoryCategories = async () => (await apiClient.get<InventoryCategory[]>('/api/inventory/categories')).data;
export const createInventoryCategory = async (name: string) => (await apiClient.post<InventoryCategory>('/api/inventory/categories', { name })).data;
export const getEquipmentTypes = async () => (await apiClient.get<EquipmentType[]>('/api/inventory/types')).data;
export const saveEquipmentType = async (id: string | null, request: SaveEquipmentType) =>
    (id ? await apiClient.put<EquipmentType>(`/api/inventory/types/${id}`, request) : await apiClient.post<EquipmentType>('/api/inventory/types', request)).data;
export const getEquipment = async (id: string) => (await apiClient.get<EquipmentDetail>(`/api/inventory/${id}`)).data;
export const getEquipmentBarcodeSvg = async (id: string) => (await apiClient.get<string>(`/api/inventory/${id}/barcode.svg`, { responseType: 'text' })).data;
export const saveEquipment = async (id: string | null, request: SaveEquipment) =>
    (id ? await apiClient.put<Equipment>(`/api/inventory/${id}`, request) : await apiClient.post<Equipment>('/api/inventory', request)).data;
export const setEquipmentActive = async (item: Equipment, isActive: boolean) =>
    (await apiClient.put<EquipmentDetail>(`/api/inventory/${item.id}/active`, { isActive, expectedVersion: item.version })).data;
export const recordEquipmentInspection = async (id: string, request: RecordInspection) =>
    (await apiClient.post<EquipmentDetail>(`/api/inventory/${id}/inspections`, request)).data;

export const inventoryDate = (value: string) => new Date(`${value}T12:00:00`).toLocaleDateString('de-DE');
export const todayDate = () => new Date().toISOString().slice(0, 10);
