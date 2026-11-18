import apiClient from '../../../api/apiClient';
import type { HydrantStatus } from '../models/HydrantModels';

export interface HydrantInspection {
  id: string;
  hydrantId: number;
  inspectedOn: string;
  inspectedAtUtc: string;
  inspectorId: string;
  inspectorName: string;
  comment: string | null;
  previousStatus: HydrantStatus;
  newStatus: HydrantStatus;
  photoStorageKey: string | null;
  photoContentType: string | null;
}

export interface CreateHydrantInspection {
  comment: string | null;
  newStatus: HydrantStatus;
}

export const getHydrantInspections = async (hydrantId: number) =>
  (await apiClient.get<HydrantInspection[]>(`/api/hydrant/${hydrantId}/inspections`)).data;

export const createHydrantInspection = async (hydrantId: number, request: CreateHydrantInspection) =>
  (await apiClient.post<HydrantInspection>(`/api/hydrant/${hydrantId}/inspections`, request)).data;
