import { Component } from 'react';
import type { ErrorInfo, ReactNode } from 'react';
import { logMapSymbol, mapSymbolErrorDetails } from '../api/mapSymbolDiagnostics';

export default class MapSymbolErrorBoundary extends Component<{ children: ReactNode }, { failed: boolean }> {
    state = { failed: false };
    static getDerivedStateFromError() { return { failed: true }; }
    componentDidCatch(error: Error, info: ErrorInfo) {
        logMapSymbol('render.failed', { ...mapSymbolErrorDetails(error), componentStack: info.componentStack }, 'error');
    }
    render() {
        if (!this.state.failed) return this.props.children;
        return <section className="card incident-top">
            <h2>Lagekartensymbole</h2>
            <p role="alert">Die Symbolverwaltung konnte wegen eines unerwarteten Fehlers nicht angezeigt werden. Die Debug-Diagnose unten enthält die Fehlerdetails.</p>
            <button type="button" onClick={() => { logMapSymbol('render.retry'); this.setState({ failed: false }); }}>Erneut versuchen</button>
        </section>;
    }
}
