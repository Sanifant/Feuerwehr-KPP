import { createContext, useContext } from 'react';
import type { IncidentSummary } from '../api/incidentApi';

export interface ActiveIncidentState {
    incident: IncidentSummary | null;
    loading: boolean;
    error: string | null;
    refresh: () => Promise<void>;
    setActiveIncident: (incident: IncidentSummary | null) => void;
}

export const ActiveIncidentContext = createContext<ActiveIncidentState | null>(null);

export function useActiveIncident() {
    const context = useContext(ActiveIncidentContext);
    if (!context) throw new Error('ActiveIncidentProvider fehlt.');
    return context;
}
