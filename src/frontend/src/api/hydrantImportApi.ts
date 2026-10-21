import apiClient from './apiClient';

export interface HydrantImportError {
    lineNumber: number | null;
    description: string;
}

export interface HydrantImportResponse {
    importedCount: number;
    errors: HydrantImportError[];
}

export async function importHydrants(file: File): Promise<HydrantImportResponse> {
    const form = new FormData();
    form.append('file', file);
    const response = await apiClient.post<HydrantImportResponse>('/api/hydrant/import', form, {
        // Override apiClient's JSON default. The browser supplies the multipart boundary.
        headers: { 'Content-Type': undefined },
    });
    return response.data;
}
