import { useState, useSyncExternalStore } from 'react';
import { clearMapSymbolDiagnostics, getMapSymbolDiagnostics, subscribeMapSymbolDiagnostics } from '../api/mapSymbolDiagnostics';

export default function MapSymbolDiagnostics() {
    const entries = useSyncExternalStore(subscribeMapSymbolDiagnostics, getMapSymbolDiagnostics);
    const [message, setMessage] = useState('');
    const report = entries.map(entry => `${entry.time} [${entry.level}] ${entry.step}\n${JSON.stringify(entry.details, null, 2)}`).join('\n\n');
    return <details className="card map-symbol-diagnostics">
        <summary>Debug-Diagnose ({entries.length} Meldungen)</summary>
        <p>Die letzten 80 Schritte bleiben nach dem Neuladen erhalten. SVG-Inhalte, Passwörter und Tokens werden nicht protokolliert.</p>
        <div className="incident-actions">
            <button type="button" onClick={async () => {
                try { await navigator.clipboard.writeText(report); setMessage('Diagnose kopiert.'); }
                catch { setMessage('Kopieren nicht möglich. Den Diagnose-Text unten auswählen und kopieren.'); }
            }}>Diagnose kopieren</button>
            <button type="button" className="btn-secondary" onClick={() => { clearMapSymbolDiagnostics(); setMessage(''); }}>Diagnose leeren</button>
        </div>
        {message && <p>{message}</p>}
        <pre tabIndex={0}>{report || 'Noch keine Meldungen.'}</pre>
    </details>;
}
