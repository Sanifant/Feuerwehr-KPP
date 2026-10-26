export function getIncidentSessionId() {
    const existing = sessionStorage.getItem('incident-session-id');
    if (existing) return existing;
    const id = crypto.randomUUID();
    sessionStorage.setItem('incident-session-id', id);
    return id;
}
