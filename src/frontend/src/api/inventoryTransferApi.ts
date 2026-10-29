import apiClient from './apiClient';

export type InventoryTransferKind = 'equipment' | 'types' | 'categories';
export interface InventoryImportError { lineNumber: number | null; description: string }
export interface InventoryImportResponse { createdCount: number; updatedCount: number; errors: InventoryImportError[] }

const paths: Record<InventoryTransferKind, string> = { equipment: '/api/inventory', types: '/api/inventory/types', categories: '/api/inventory/categories' };

export async function importInventoryData(kind: InventoryTransferKind, file: File): Promise<InventoryImportResponse> {
    const form = new FormData(); form.append('file', file);
    return (await apiClient.post<InventoryImportResponse>(`${paths[kind]}/import`, form, { headers: { 'Content-Type': undefined } })).data;
}

export async function exportInventoryData(kind: InventoryTransferKind): Promise<Blob> {
    return (await apiClient.get<Blob>(`${paths[kind]}/export`, { responseType: 'blob' })).data;
}
