import { useCallback, useEffect, useRef, useState } from 'react';
import type { ReactNode } from 'react';
import { useLocation } from 'react-router-dom';
import { useAuth } from './AuthContext';
import { useRole } from './hooks/useRole';
import { ActiveIncidentContext } from './hooks/useActiveIncident';
import { getActiveIncident } from './api/incidentApi';
import type { IncidentSummary } from './api/incidentApi';

export default function ActiveIncidentProvider({ children }: { children: ReactNode }) {
    const { isAuthenticated, user } = useAuth();
    const userId = user?.id;
    const { canViewSituationMap, canViewIncidentDiary } = useRole();
    const canAccess = isAuthenticated && (canViewSituationMap || canViewIncidentDiary);
    const { pathname } = useLocation();
    const [incident, setIncident] = useState<IncidentSummary | null>(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const requestRef = useRef(0);

    const refresh = useCallback(async () => {
        const request = ++requestRef.current;
        if (!canAccess || !userId) {
            setIncident(null);
            setError(null);
            setLoading(false);
            return;
        }
        try {
            const active = await getActiveIncident();
            if (request !== requestRef.current) return;
            setIncident(active.status === 'Active' ? active : null);
            setError(null);
        } catch (failure: unknown) {
            if (request !== requestRef.current) return;
            const status = (failure as { response?: { status?: number } })?.response?.status;
            if (status === 404) setIncident(null);
            setError(status === 404 ? null : 'Der aktive Einsatz konnte nicht geladen werden.');
        } finally {
            if (request === requestRef.current) setLoading(false);
        }
    }, [canAccess, userId]);

    const invalidate = useCallback(() => { ++requestRef.current; }, []);

    const setActiveIncident = useCallback((active: IncidentSummary | null) => {
        ++requestRef.current;
        setIncident(active);
        setError(null);
        setLoading(false);
    }, []);

    useEffect(() => {
        void refresh();
        return invalidate;
    }, [refresh, pathname, invalidate]);

    useEffect(() => {
        if (!canAccess) return;
        const update = () => { void refresh(); };
        const timer = window.setInterval(update, 10000);
        window.addEventListener('focus', update);
        return () => {
            window.clearInterval(timer);
            window.removeEventListener('focus', update);
        };
    }, [canAccess, refresh]);

    return <ActiveIncidentContext.Provider value={{ incident: canAccess ? incident : null, loading, error, refresh, setActiveIncident }}>
        {children}
    </ActiveIncidentContext.Provider>;
}
