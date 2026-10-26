import { useCallback, useEffect, useRef, useState } from 'react';
import type { ChangeEvent, FormEvent } from 'react';
import { useRole } from '../hooks/useRole';
import { getMapSymbolCategories, getMapSymbols, saveMapSymbol, svgImageUrl } from '../api/mapSymbolApi';
import type { MapSymbol } from '../api/mapSymbolApi';
import { getApiErrorMessage } from '../api/errors';
import { logMapSymbol, mapSymbolErrorDetails } from '../api/mapSymbolDiagnostics';
import MapSymbolDiagnostics from './MapSymbolDiagnostics';
import MapSymbolErrorBoundary from './MapSymbolErrorBoundary';

export default function MapSymbolAdmin() {
    const { isAdmin } = useRole();
    if (!isAdmin) return <section className="card"><h2>Lagekartensymbole</h2><p role="alert">Nur für Administratoren.</p></section>;
    return <div className="incident-page"><MapSymbolErrorBoundary><MapSymbolAdminContent /></MapSymbolErrorBoundary><MapSymbolDiagnostics /></div>;
}

function MapSymbolAdminContent() {
    const { isAdmin } = useRole();
    const [symbols, setSymbols] = useState<MapSymbol[]>([]);
    const [categories, setCategories] = useState<string[]>([]);
    const [editing, setEditing] = useState<MapSymbol | null>(null);
    const [name, setName] = useState('');
    const [category, setCategory] = useState('Feuerwehr');
    const [active, setActive] = useState(true);
    const [file, setFile] = useState<File | null>(null);
    const [busy, setBusy] = useState(false);
    const [error, setError] = useState('');
    const [message, setMessage] = useState('');
    const [connectionError, setConnectionError] = useState('');
    const fileRef = useRef<HTMLInputElement>(null);
    const formRef = useRef<HTMLFormElement>(null);
    const busyRef = useRef(false);

    const load = useCallback(async () => {
        if (!isAdmin) return;
        logMapSymbol('catalog.load.start');
        try {
            const [items, groups] = await Promise.all([getMapSymbols(), getMapSymbolCategories()]);
            setSymbols(items);
            setCategories(groups);
            logMapSymbol('catalog.load.success', { symbols: items.length, categories: groups.length });
        } catch (failure: unknown) {
            logMapSymbol('catalog.load.failed', mapSymbolErrorDetails(failure), 'error');
            throw failure;
        }
    }, [isAdmin]);
    useEffect(() => { void load().catch(() => setError('Der Symbolkatalog konnte nicht geladen werden.')); }, [load]);

    useEffect(() => {
        logMapSymbol('page.mounted');
        const onError = (event: ErrorEvent) => {
            logMapSymbol('window.error', { ...mapSymbolErrorDetails(event.error), message: event.message, line: event.lineno, column: event.colno }, 'error');
            setError('Ein unerwarteter Browserfehler ist aufgetreten. Bitte die Debug-Diagnose öffnen.');
        };
        const onRejection = (event: PromiseRejectionEvent) => {
            logMapSymbol('window.unhandledrejection', mapSymbolErrorDetails(event.reason), 'error');
            setError('Ein unerwarteter Hintergrundfehler ist aufgetreten. Bitte die Debug-Diagnose öffnen.');
        };
        const onBlur = () => logMapSymbol('window.blur');
        const onFocus = () => {
            logMapSymbol('window.focus');
            logMapSymbol('api.connection.check');
            void getMapSymbolCategories().then(() => { setConnectionError(''); logMapSymbol('api.connection.ok'); })
                .catch((failure: unknown) => {
                    logMapSymbol('api.connection.failed', mapSymbolErrorDetails(failure), 'error');
                    setConnectionError('Die API ist nicht erreichbar. Bitte prüfen, ob der Debug-Lauf in Visual Studio noch läuft.');
                });
        };
        window.addEventListener('error', onError); window.addEventListener('unhandledrejection', onRejection);
        window.addEventListener('blur', onBlur); window.addEventListener('focus', onFocus);
        return () => {
            logMapSymbol('page.unmounted');
            window.removeEventListener('error', onError); window.removeEventListener('unhandledrejection', onRejection);
            window.removeEventListener('blur', onBlur); window.removeEventListener('focus', onFocus);
        };
    }, []);

    const selectFile = (event: ChangeEvent<HTMLInputElement>) => {
        const input = event.currentTarget;
        try {
            const selected = input.files?.[0] ?? null;
            logMapSymbol('file.selected', selected ? { name: selected.name, type: selected.type, bytes: selected.size } : { canceled: true });
            setMessage('');
            setFile(null);
            if (!selected) { setError(''); return; }
            const extensionValid = selected.name.toLowerCase().endsWith('.svg');
            const mimeValid = !selected.type || ['image/svg+xml', 'application/xml', 'text/xml', 'text/plain', 'application/octet-stream'].includes(selected.type.toLowerCase());
            if (!extensionValid || !mimeValid || selected.size === 0 || selected.size > 262144) {
                const reason = !extensionValid || !mimeValid ? 'format' : selected.size === 0 ? 'empty' : 'size';
                logMapSymbol('file.rejected', { reason, name: selected.name, type: selected.type, bytes: selected.size }, 'warn');
                setError(reason === 'format' ? 'PNG-, JPG- und andere Bilddateien werden nicht unterstützt. Bitte eine SVG-Datei auswählen.'
                    : 'Bitte eine nicht leere SVG-Datei mit höchstens 256 KB auswählen.');
                input.value = '';
                return;
            }
            setFile(selected); setError('');
            if (!name) setName(selected.name.replace(/\.svg$/i, ''));
            logMapSymbol('file.accepted', { name: selected.name, bytes: selected.size });
        } catch (failure: unknown) {
            logMapSymbol('file.selection.failed', mapSymbolErrorDetails(failure), 'error');
            setFile(null); setError('Die Dateiauswahl konnte nicht verarbeitet werden. Bitte die Debug-Diagnose öffnen.');
        }
    };

    const reset = () => {
        setEditing(null); setName(''); setCategory(categories[0] ?? 'Feuerwehr'); setActive(true); setFile(null);
        if (fileRef.current) fileRef.current.value = '';
    };

    const save = async (event: FormEvent) => {
        event.preventDefault();
        if (busyRef.current || !isAdmin) return;
        logMapSymbol('save.requested', { symbolId: editing?.id ?? null, category, hasFile: file !== null });
        setError(''); setMessage('');
        if (!name.trim() || !categories.includes(category) || (!file && !editing)) { setError('Bitte Name, Kategorie und eine SVG-Datei angeben.'); return; }
        if (file && (!file.name.toLowerCase().endsWith('.svg') || file.size > 262144 || file.size === 0)) {
            setError('Bitte eine SVG-Datei mit höchstens 256 KB auswählen.'); return;
        }
        busyRef.current = true; setBusy(true);
        let phase = 'file.read';
        try {
            logMapSymbol('file.read.start', { replacement: file !== null, bytes: file?.size });
            const svgContent = file ? await file.text() : editing!.svgContent;
            logMapSymbol('file.read.success', { characters: svgContent.length });
            phase = 'upload';
            logMapSymbol('upload.start', { symbolId: editing?.id ?? null, category });
            const saved = await saveMapSymbol(editing?.id ?? null, { name: name.trim(), category, svgContent, isActive: active });
            logMapSymbol('upload.success', { symbolId: saved.id });
            reset();
            setMessage('Symbol gespeichert.');
            await load().catch(() => setError('Symbol wurde gespeichert, aber der Katalog konnte nicht aktualisiert werden.'));
        } catch (failure: unknown) {
            logMapSymbol(`${phase}.failed`, mapSymbolErrorDetails(failure), 'error');
            setError(getApiErrorMessage(failure, phase === 'file.read'
                ? 'Die SVG-Datei konnte nicht gelesen werden. Details stehen in der Debug-Diagnose.'
                : 'Das Symbol konnte nicht gespeichert werden. Details stehen in der Debug-Diagnose.'));
        }
        finally { busyRef.current = false; setBusy(false); }
    };

    if (!isAdmin) return <section className="card"><h2>Lagekartensymbole</h2><p role="alert">Nur für Administratoren.</p></section>;
    return <div className="incident-page">
        <section className="card incident-top">
            <h2>Lagekartensymbole verwalten</h2>
            <p>SVG-Bilder einer Kategorie zuordnen. Deaktivierte Symbole können nicht neu platziert werden; vorhandene Kartenelemente bleiben erhalten.</p>
            <p>Enthaltene PNG- oder JPEG-Bilder müssen direkt in der SVG-Datei eingebettet sein. Maximale Dateigröße: 256 KB.</p>
            {error && <p role="alert">{error}</p>}{message && <p role="status">{message}</p>}
            {connectionError && <p role="alert">{connectionError}</p>}
            <button type="button" disabled={busy} onClick={() => { setError(''); void load().catch(() => setError('Der Symbolkatalog konnte nicht geladen werden.')); }}>Aktualisieren</button>
            <form onSubmit={save} ref={formRef} noValidate>
                <fieldset className="incident-create-fields" disabled={busy}>
                    <legend>{editing ? 'Symbol bearbeiten' : 'Neues Symbol hochladen'}</legend>
                    <div className="incident-create-grid">
                        <label>Name *<input required maxLength={200} value={name} onChange={event => setName(event.target.value)} /></label>
                        <label>Kategorie *<select value={category} onChange={event => setCategory(event.target.value)}>{categories.map(item => <option key={item}>{item}</option>)}</select></label>
                        <label>SVG-Datei{editing ? ' (optional ersetzen)' : ' *'}<input ref={fileRef} type="file" accept=".svg,image/svg+xml" required={!editing}
                            onClick={() => logMapSymbol('file.picker.open')} onChange={selectFile} /></label>
                        <label className="checkbox-row"><input type="checkbox" checked={active} onChange={event => setActive(event.target.checked)} />Aktiv</label>
                    </div>
                    <div className="incident-actions"><button type="submit">{busy ? 'Wird gespeichert …' : 'Symbol speichern'}</button>{editing && <button type="button" className="btn-secondary" onClick={reset}>Abbrechen</button>}</div>
                </fieldset>
            </form>
        </section>
        {categories.map(group => <section className="card incident-top" key={group}>
            <h3>{group}</h3>
            <div className="symbol-admin-grid">{symbols.filter(symbol => symbol.category === group).map(symbol => <div className="symbol-admin-item" key={symbol.id}>
                <img src={svgImageUrl(symbol.svgContent)} alt="" width="64" height="64" onError={() => {
                    logMapSymbol('preview.failed', { symbolId: symbol.id }, 'error');
                    setError('Eine SVG-Vorschau konnte nicht geladen werden. Details stehen in der Debug-Diagnose.');
                }} />
                <strong>{symbol.name}</strong><span>{symbol.isActive ? 'Aktiv' : 'Deaktiviert'}</span>
                <button type="button" disabled={busy} onClick={() => {
                    setEditing(symbol); setName(symbol.name); setCategory(symbol.category); setActive(symbol.isActive); setFile(null);
                    if (fileRef.current) fileRef.current.value = '';
                    setError(''); setMessage(''); formRef.current?.scrollIntoView({ block: 'start' });
                }} aria-label={`${symbol.name} bearbeiten`}>Bearbeiten</button>
            </div>)}</div>
            {!symbols.some(symbol => symbol.category === group) && <p>Noch keine Symbole vorhanden.</p>}
        </section>)}
    </div>;
}
