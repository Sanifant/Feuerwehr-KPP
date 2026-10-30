import apiClient from './apiClient';

export type VehicleTransferKind = 'vehicles' | 'types';
export interface VehicleImportError { lineNumber: number | null; description: string }
export interface VehicleImportResponse { createdCount: number; updatedCount: number; errors: VehicleImportError[] }
const paths = { vehicles: '/api/vehicles', types: '/api/vehicles/types' };
export async function importVehicleData(kind: VehicleTransferKind, file: File): Promise<VehicleImportResponse> {
    const form = new FormData(); form.append('file', file);
    return (await apiClient.post<VehicleImportResponse>(`${paths[kind]}/import`, form, { headers: { 'Content-Type': undefined } })).data;
}
export const exportVehicleData = async (kind: VehicleTransferKind) =>
    (await apiClient.get<Blob>(`${paths[kind]}/export`, { responseType: 'blob' })).data;
