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
    alarmedAtUtc: string;
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

export interface IncidentCreatedResponse {
    incident: IncidentSummary;
    lease: EditorLease;
    serverUtc: string;
}

type IncidentWire = Omit<IncidentSummary, 'status'> & { status: IncidentStatus | 1 | 2 };

function normalizeIncidentStatus(status: IncidentStatus | 1 | 2): IncidentStatus {
    if (status === 1 || status === 'Active') return 'Active';
    if (status === 2 || status === 'Closed') return 'Closed';
    throw new Error('Unbekannter Einsatzstatus.');
}

function normalizeIncident(incident: IncidentWire): IncidentSummary {
    return { ...incident, status: normalizeIncidentStatus(incident.status), alarmedAtUtc: incident.alarmedAtUtc ?? incident.createdAtUtc };
}

export interface MapElementDto {
    id: string;
    incidentId: string;
    elementType: 'Symbol' | 'Polygon';
    symbolId?: string;
    symbolSvg?: string;
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

type MapElementWire = Omit<MapElementDto, 'elementType'> & { elementType: MapElementDto['elementType'] | 1 | 2 };

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
    const response = await apiClient.get<IncidentWire>('/api/incidents/active');
    return normalizeIncident(response.data);
}

export async function getIncidentArchive(page = 1, pageSize = 25) {
    const response = await apiClient.get<IncidentWire[]>('/api/incidents/archive', { params: { page, pageSize } });
    return response.data.map(normalizeIncident);
}

type DiaryRevisionWire = Omit<DiaryRevision, 'revisionType'> & { revisionType: DiaryRevision['revisionType'] | 1 | 2 | 3 };
type DiaryEntryWire = Omit<DiaryEntry, 'currentRevisionData' | 'revisions'> & { currentRevisionData: DiaryRevisionWire; revisions: DiaryRevisionWire[] };

function normalizeRevision(revision: DiaryRevisionWire): DiaryRevision {
    const revisionType = revision.revisionType === 1 ? 'Created' : revision.revisionType === 2 ? 'Edited'
        : revision.revisionType === 3 ? 'Canceled' : revision.revisionType;
    return { ...revision, revisionType };
}

function normalizeDiaryEntry(entry: DiaryEntryWire): DiaryEntry {
    return { ...entry, currentRevisionData: normalizeRevision(entry.currentRevisionData), revisions: entry.revisions.map(normalizeRevision) };
}

export async function getArchivedDiaryEntries(incidentId: string) {
    const response = await apiClient.get<DiaryEntryWire[]>(`/api/incidents/archive/${incidentId}/diary`);
    return response.data.map(normalizeDiaryEntry);
}

export async function downloadArchivedIncidentPdf(incidentId: string) {
    const response = await apiClient.get<Blob>(`/api/incidents/archive/${incidentId}/export.pdf`, { responseType: 'blob' });
    const url = URL.createObjectURL(response.data);
    const link = document.createElement('a');
    try {
        link.href = url;
        link.download = `einsatztagebuch-${incidentId.replace(/-/g, '')}.pdf`;
        document.body.append(link);
        link.click();
    } finally {
        link.remove();
        URL.revokeObjectURL(url);
    }
}

export async function createIncident(payload: {
    name: string;
    incidentNumber?: string;
    keyword?: string;
    description?: string;
    alarmedAtUtc: string;
    originLongitude: number;
    originLatitude: number;
    sessionId: string;
    commandId: string;
}) {
    const response = await apiClient.post<Omit<IncidentCreatedResponse, 'incident'> & { incident: IncidentWire }>('/api/incidents', payload);
    return { ...response.data, incident: normalizeIncident(response.data.incident) };
}

export async function closeIncident(incidentId: string, payload: { leaseToken: string; sessionId: string; commandId: string }) {
    const response = await apiClient.post(`/api/incidents/${incidentId}/close`, payload);
    return response.data;
}

export async function getMapState(incidentId: string) {
    const response = await apiClient.get<Omit<ActiveIncidentSnapshot, 'incidentStatus' | 'elements'> & { incidentStatus: IncidentStatus | 1 | 2; elements: MapElementWire[] }>(`/api/incidents/${incidentId}/map/state`);
    return { ...response.data, incidentStatus: normalizeIncidentStatus(response.data.incidentStatus), elements: response.data.elements.map(normalizeMapElement) };
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

export interface MapElementWrite {
    commandId: string;
    sessionId: string;
    leaseToken: string;
    elementId?: string;
    elementType: 'Symbol' | 'Polygon';
    symbolId?: string;
    category?: string;
    label?: string;
    radioCallName?: string;
    strength?: string;
    note?: string;
    colorHex?: string;
    geometryJson: string;
    expectedVersion?: number;
}

function normalizeMapElement(element: MapElementWire): MapElementDto {
    const type = element.elementType;
    return { ...element, elementType: type === 1 ? 'Symbol' : type === 2 ? 'Polygon' : type };
}

export async function upsertMapElement(incidentId: string, payload: MapElementWrite) {
    const response = await apiClient.post<{ element: MapElementWire }>(`/api/incidents/${incidentId}/map/elements`, {
        ...payload, elementType: payload.elementType === 'Symbol' ? 1 : 2,
    });
    return { ...response.data, element: normalizeMapElement(response.data.element) };
}

export async function deleteMapElement(incidentId: string, elementId: string, payload: { commandId: string; sessionId: string; leaseToken: string; expectedVersion: number }) {
    const response = await apiClient.delete(`/api/incidents/${incidentId}/map/elements/${elementId}`, { data: payload });
    return response.data;
}

export async function getMapAudit(incidentId: string) {
    const response = await apiClient.get(`/api/incidents/${incidentId}/map/audit`);
    return response.data;
}

export async function getDiaryEntries(incidentId: string) {
    const response = await apiClient.get<DiaryEntryWire[]>(`/api/incidents/${incidentId}/diary/entries`);
    return response.data.map(normalizeDiaryEntry);
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
