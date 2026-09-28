import apiClient from './apiClient';

export type IncidentStatus = 'Active' | 'Closed';

export interface IncidentSummary {
    id: string;
    name: string;
    incidentNumber?: string;
    keyword?: string;
    description?: string;
    originLongitude: number;
    originLatitude: number;
    status: IncidentStatus;
    createdAtUtc: string;
    closedAtUtc?: string;
    revision: number;
}

export interface EditorLease {
    incidentId: string;
    userId: string;
    userDisplayName: string;
    sessionId: string;
    leaseToken: string;
    expiresAtUtc: string;
    lastHeartbeatAtUtc: string;
    isOwnedByCurrentSession: boolean;
    isExpired: boolean;
}

export interface MapElementDto {
    id: string;
    incidentId: string;
    elementType: 'Symbol' | 'Polygon';
    symbolId?: string;
    category?: string;
    label?: string;
    radioCallName?: string;
    strength?: string;
    note?: string;
    colorHex: string;
    geometryJson: string;
    version: number;
    updatedAtUtc: string;
    updatedByDisplayName: string;
}

export interface DiaryCategory {
    id: string;
    code: string;
    name: string;
    isActive: boolean;
}

export interface DiaryRevision {
    id: string;
    revisionNumber: number;
    revisionType: 'Created' | 'Edited' | 'Canceled';
    eventTimestampUtc: string;
    categoryCode: string;
    categoryNameSnapshot: string;
    text: string;
    sender?: string;
    recipient?: string;
    transmissionType?: string;
    cancellationReason?: string;
    createdAtUtc: string;
    createdByDisplayName: string;
}

export interface DiaryEntry {
    id: string;
    incidentId: string;
    entryNumber: number;
    currentRevision: number;
    isCanceled: boolean;
    cancellationReason?: string;
    createdAtUtc: string;
    updatedAtUtc: string;
    currentRevisionData: DiaryRevision;
    revisions: DiaryRevision[];
}

export interface ActiveIncidentSnapshot {
    incidentId: string;
    incidentStatus: IncidentStatus;
    revision: number;
    serverUtc: string;
    lease: EditorLease | null;
    elements: MapElementDto[];
    hydrants: Array<{ id: number; longitude: number; latitude: number; status: string; nominalDiameter: number }>;
}

export function buildCommandId() {
    return crypto.randomUUID();
}

export async function getActiveIncident() {
    const response = await apiClient.get<IncidentSummary>('/api/incidents/active');
    return response.data;
}

export async function getIncidentArchive() {
    const response = await apiClient.get<IncidentSummary[]>('/api/incidents/archive');
    return response.data;
}

export async function createIncident(payload: {
    name: string;
    incidentNumber?: string;
    keyword?: string;
    description?: string;
    originLongitude: number;
    originLatitude: number;
    sessionId: string;
    commandId: string;
}) {
    const response = await apiClient.post('/api/incidents', payload);
    return response.data;
}

export async function closeIncident(incidentId: string, payload: { leaseToken: string; sessionId: string; commandId: string }) {
    const response = await apiClient.post(`/api/incidents/${incidentId}/close`, payload);
    return response.data;
}

export async function getMapState(incidentId: string) {
    const response = await apiClient.get<ActiveIncidentSnapshot>(`/api/incidents/${incidentId}/map/state`);
    return response.data;
}

export async function acquireMapLease(incidentId: string, payload: { sessionId: string; commandId: string }) {
    const response = await apiClient.post(`/api/incidents/${incidentId}/map/lease/acquire`, payload);
    return response.data;
}

export async function heartbeatMapLease(incidentId: string, payload: { sessionId: string; leaseToken: string; commandId: string }) {
    const response = await apiClient.post(`/api/incidents/${incidentId}/map/lease/heartbeat`, payload);
    return response.data;
}

export async function releaseMapLease(incidentId: string, payload: { sessionId: string; leaseToken: string; commandId: string }) {
    const response = await apiClient.post(`/api/incidents/${incidentId}/map/lease/release`, payload);
    return response.data;
}

export async function upsertMapElement(incidentId: string, payload: any) {
    const response = await apiClient.post(`/api/incidents/${incidentId}/map/elements`, payload);
    return response.data;
}

export async function deleteMapElement(incidentId: string, elementId: string, payload: any) {
    const response = await apiClient.delete(`/api/incidents/${incidentId}/map/elements/${elementId}`, { data: payload });
    return response.data;
}

export async function getMapAudit(incidentId: string) {
    const response = await apiClient.get(`/api/incidents/${incidentId}/map/audit`);
    return response.data;
}

export async function getDiaryEntries(incidentId: string) {
    const response = await apiClient.get<DiaryEntry[]>(`/api/incidents/${incidentId}/diary/entries`);
    return response.data;
}

export async function getDiaryCategories(incidentId: string) {
    const response = await apiClient.get<DiaryCategory[]>(`/api/incidents/${incidentId}/diary/categories`);
    return response.data;
}

export async function createDiaryEntry(incidentId: string, payload: any) {
    const response = await apiClient.post(`/api/incidents/${incidentId}/diary/entries`, payload);
    return response.data;
}

export async function updateDiaryEntry(incidentId: string, entryId: string, payload: any) {
    const response = await apiClient.post(`/api/incidents/${incidentId}/diary/entries/${entryId}/revisions`, payload);
    return response.data;
}

export async function cancelDiaryEntry(incidentId: string, entryId: string, payload: any) {
    const response = await apiClient.post(`/api/incidents/${incidentId}/diary/entries/${entryId}/cancel`, payload);
    return response.data;
}

export async function getDiaryCategoryAdmin() {
    const response = await apiClient.get<DiaryCategory[]>('/api/diary-categories');
    return response.data;
}

export async function upsertDiaryCategory(payload: { code: string; name: string; isActive: boolean }) {
    const response = await apiClient.post('/api/diary-categories', payload);
    return response.data;
}

export function getMapAuditExportUrl(incidentId: string) {
    const base = apiClient.defaults.baseURL ?? '';
    return `${base}/api/incidents/${incidentId}/map/audit/export.csv`;
}

export function getMapPdfExportUrl(incidentId: string) {
    const base = apiClient.defaults.baseURL ?? '';
    return `${base}/api/incidents/${incidentId}/map/export.pdf`;
}

export function getDiaryPdfExportUrl(incidentId: string) {
    const base = apiClient.defaults.baseURL ?? '';
    return `${base}/api/incidents/${incidentId}/diary/export.pdf`;
}
