export interface MapSymbolDiagnostic {
    time: string;
    level: 'info' | 'warn' | 'error';
    step: string;
    details: Record<string, unknown>;
}

const storageKey = 'feuerwehr.map-symbol-diagnostics';
const listeners = new Set<() => void>();
let entries: MapSymbolDiagnostic[] = [];
try {
    const stored: unknown = JSON.parse(localStorage.getItem(storageKey) ?? '[]');
    if (Array.isArray(stored)) entries = stored.filter(item => item && typeof item.time === 'string' && typeof item.step === 'string').slice(-80);
} catch { /* Diagnostics remain available in memory when browser storage is unavailable. */ }

export function mapSymbolErrorDetails(failure: unknown): Record<string, unknown> {
    const error = failure as { name?: unknown; message?: unknown; stack?: unknown; response?: { status?: number; data?: { traceId?: string; title?: string } } } | null;
    return {
        name: typeof error?.name === 'string' ? error.name : 'UnknownError',
        message: typeof error?.message === 'string' ? error.message.slice(0, 1000) : typeof failure === 'string' ? failure.slice(0, 1000) : undefined,
        // Axios stacks are safe here; its config, headers and request payload are deliberately omitted.
        stack: typeof error?.stack === 'string' ? error.stack.slice(0, 4000) : undefined,
        status: error?.response?.status,
        title: error?.response?.data?.title,
        traceId: error?.response?.data?.traceId,
    };
}

export function logMapSymbol(step: string, details: Record<string, unknown> = {}, level: MapSymbolDiagnostic['level'] = 'info') {
    const entry = { time: new Date().toISOString(), level, step, details };
    entries = [...entries, entry].slice(-80);
    try { localStorage.setItem(storageKey, JSON.stringify(entries)); } catch { /* Logging must not interrupt file selection or saving. */ }
    console[level](`[MapSymbolAdmin] ${step}`, entry);
    listeners.forEach(listener => listener());
}

export function getMapSymbolDiagnostics() { return entries; }
export function subscribeMapSymbolDiagnostics(listener: () => void) { listeners.add(listener); return () => { listeners.delete(listener); }; }
export function clearMapSymbolDiagnostics() {
    entries = [];
    try { localStorage.removeItem(storageKey); } catch { /* Keep the in-memory controls usable. */ }
    listeners.forEach(listener => listener());
}
