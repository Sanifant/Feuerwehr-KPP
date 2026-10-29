import { useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { useRole } from '../hooks/useRole';
import { getApiErrorMessage } from '../api/errors';
import { getEquipmentTypes, getInventoryCategories, saveEquipmentType } from '../api/inventoryApi';
import type { EquipmentType, InventoryCategory, SaveEquipmentType } from '../api/inventoryApi';
import './InventoryPage.css';
import InventoryTransferTools from './InventoryTransferTools';

const emptyType: SaveEquipmentType = { categoryId: '', name: '', inspectionIntervalMonths: 12, inspectionInstructions: '', expectedVersion: 1 };

export default function EquipmentTypesPage() {
    const { canManageInventory } = useRole();
    const [types, setTypes] = useState<EquipmentType[]>([]);
    const [categories, setCategories] = useState<InventoryCategory[]>([]);
    const [editor, setEditor] = useState<{ id: string | null; draft: SaveEquipmentType } | null>(null);
    const [loading, setLoading] = useState(true);
    const [busy, setBusy] = useState(false);
    const [error, setError] = useState('');
    const [notice, setNotice] = useState('');

    useEffect(() => {
        if (!canManageInventory) return;
        let current = true;
        setLoading(true);
        Promise.all([getEquipmentTypes(), getInventoryCategories()])
            .then(([list, cats]) => { if (current) { setTypes(list); setCategories(cats); setError(''); } })
            .catch(err => { if (current) setError(getApiErrorMessage(err, 'Gerätetypen und Kategorien konnten nicht geladen werden.')); })
            .finally(() => { if (current) setLoading(false); });
        return () => { current = false; };
    }, [canManageInventory]);

    if (!canManageInventory) return <section className="card"><h1>Gerätetypen und Prüfpläne</h1><p role="alert">Nur für Gerätewarte und Administratoren.</p></section>;

    const submit = async (event: FormEvent) => {
        event.preventDefault();
        if (!editor || busy) return;
        setBusy(true); setError(''); setNotice('');
        try {
            const saved = await saveEquipmentType(editor.id, editor.draft);
            setTypes(previous => [...previous.filter(x => x.id !== saved.id), saved].sort((a, b) => a.name.localeCompare(b.name, 'de')));
            setEditor(null); setNotice('Gerätetyp und Prüfplan gespeichert.');
        } catch (err) { setError(getApiErrorMessage(err, 'Der Prüfplan konnte nicht gespeichert werden. Bitte gegebenenfalls neu laden.')); }
        finally { setBusy(false); }
    };

    return <div className="inventory-page">
        <header className="page-header"><div><h1>Gerätetypen und Prüfpläne</h1><p>Prüfanweisungen und Fristen je Gerätetyp festlegen.</p></div>
            <button disabled={busy || loading} onClick={() => { setEditor({ id: null, draft: { ...emptyType } }); setNotice(''); }}>Gerätetyp anlegen</button>
        </header>
        {error && <p className="alert alert-error" role="alert">{error}</p>}
        {notice && <p role="status">{notice}</p>}
        <InventoryTransferTools kind="types" canImport={canManageInventory} disabled={busy || loading} onBusyChange={setBusy}
            onImported={async () => { const [list, cats] = await Promise.all([getEquipmentTypes(), getInventoryCategories()]); setTypes(list); setCategories(cats); setEditor(null); }} />
        <section className="card">
            {loading ? <p role="status">Gerätetypen werden geladen …</p> : types.length === 0 ? <p>Noch keine Gerätetypen angelegt.</p> : <div className="table-scroll"><table className="log-table">
                <thead><tr><th>Gerätetyp</th><th>Kategorie</th><th>Prüfintervall</th><th>Prüfanweisung</th><th>Aktionen</th></tr></thead>
                <tbody>{types.map(type => <tr key={type.id}>
                    <td>{type.name}</td><td>{type.categoryName}</td><td>{type.inspectionIntervalMonths} Monate</td><td className="inventory-instructions">{type.inspectionInstructions}</td>
                    <td><button disabled={busy} aria-label={`${type.name} Prüfplan bearbeiten`} onClick={() => {
                        setEditor({ id: type.id, draft: { categoryId: type.categoryId, name: type.name, inspectionIntervalMonths: type.inspectionIntervalMonths, inspectionInstructions: type.inspectionInstructions, expectedVersion: type.version } }); setNotice('');
                    }}>Prüfplan bearbeiten</button></td>
                </tr>)}</tbody>
            </table></div>}
        </section>
        {editor && <section className="card"><h2>{editor.id ? 'Gerätetyp und Prüfplan bearbeiten' : 'Neuen Gerätetyp anlegen'}</h2>
            <form className="form-stack" onSubmit={submit}><fieldset disabled={busy}>
                <label>Typbezeichnung<input required maxLength={150} value={editor.draft.name} onChange={e => setEditor({ ...editor, draft: { ...editor.draft, name: e.target.value } })} /></label>
                <label>Typkategorie<select required value={editor.draft.categoryId} onChange={e => setEditor({ ...editor, draft: { ...editor.draft, categoryId: e.target.value } })}><option value="">Bitte wählen</option>{categories.map(x => <option key={x.id} value={x.id}>{x.name}</option>)}</select></label>
                <label>Prüfintervall in Monaten<input type="number" required min={1} max={120} step={1} value={editor.draft.inspectionIntervalMonths} onChange={e => setEditor({ ...editor, draft: { ...editor.draft, inspectionIntervalMonths: Number(e.target.value) } })} /></label>
                <label>Prüfanweisung<textarea required maxLength={8000} rows={5} value={editor.draft.inspectionInstructions} onChange={e => setEditor({ ...editor, draft: { ...editor.draft, inspectionInstructions: e.target.value } })} /></label>
                <p>Prüfanweisungen und Fristen nach Geräteausführung, Herstellerangaben und einschlägigen Prüfgrundsätzen festlegen. Änderungen gelten für alle Geräte dieses Typs und berechnen deren nächste Fälligkeit neu. Bereits dokumentierte Prüfpläne bleiben erhalten.</p>
                <div className="inventory-actions"><button type="submit">Prüfplan speichern</button><button type="button" onClick={() => setEditor(null)}>Abbrechen</button></div>
            </fieldset></form>
        </section>}
    </div>;
}
