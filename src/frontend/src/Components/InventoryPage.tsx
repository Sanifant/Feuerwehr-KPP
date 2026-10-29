import { useCallback, useEffect, useRef, useState } from 'react';
import type { FormEvent } from 'react';
import { useRole } from '../hooks/useRole';
import { getApiErrorMessage } from '../api/errors';
import { equipmentStatusLabels, getEquipment, getEquipmentBarcodeSvg, getEquipmentTypes, getInventory,
    getInventoryCategories, inventoryDate, recordEquipmentInspection, saveEquipment,
    setEquipmentActive, todayDate } from '../api/inventoryApi';
import type { Equipment, EquipmentDetail, EquipmentStatus, EquipmentType, InventoryCategory, SaveEquipment } from '../api/inventoryApi';
import './InventoryPage.css';
import InventoryTransferTools from './InventoryTransferTools';

const emptyEquipment: SaveEquipment = { equipmentTypeId: '', serialNumber: '', manufacturer: '', status: 'Ready', expectedVersion: 1 };

export default function InventoryPage() {
    const { canViewInventory, canManageInventory } = useRole();
    const [items, setItems] = useState<Equipment[]>([]);
    const [categories, setCategories] = useState<InventoryCategory[]>([]);
    const [types, setTypes] = useState<EquipmentType[]>([]);
    const [includeInactive, setIncludeInactive] = useState(false);
    const [search, setSearch] = useState('');
    const [category, setCategory] = useState('');
    const [detail, setDetail] = useState<EquipmentDetail | null>(null);
    const [editor, setEditor] = useState<{ id: string | null; draft: SaveEquipment } | null>(null);
    const [inspection, setInspection] = useState({ inspectedOn: todayDate(), status: 'Ready' as EquipmentStatus, comment: '' });
    const commandId = useRef(crypto.randomUUID());
    const selection = useRef(0);
    const invalidateSelection = useCallback(() => { selection.current++; }, []);
    const [busy, setBusy] = useState(false);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState('');
    const [notice, setNotice] = useState('');

    const load = useCallback(async () => {
        const [list, cats, typeList] = await Promise.all([getInventory(includeInactive), getInventoryCategories(), getEquipmentTypes()]);
        setItems(list); setCategories(cats); setTypes(typeList);
    }, [includeInactive]);

    useEffect(() => {
        let current = true;
        if (!canViewInventory) { setItems([]); setDetail(null); setLoading(false); return; }
        setLoading(true);
        Promise.all([getInventory(includeInactive), getInventoryCategories(), getEquipmentTypes()])
            .then(([list, cats, typeList]) => { if (current) { setItems(list); setCategories(cats); setTypes(typeList); setError(''); } })
            .catch(err => { if (current) setError(getApiErrorMessage(err, 'Inventar konnte nicht geladen werden.')); })
            .finally(() => { if (current) setLoading(false); });
        return () => { current = false; invalidateSelection(); };
    }, [canViewInventory, includeInactive, invalidateSelection]);

    if (!canViewInventory) return <section className="card"><h1>Inventar</h1><p role="alert">Keine Berechtigung für das Inventar.</p></section>;

    const run = async (action: () => Promise<void>, success: string) => {
        if (busy) return;
        setBusy(true); setError(''); setNotice('');
        try { await action(); setNotice(success); }
        catch (err) { setError(getApiErrorMessage(err, 'Die Änderung konnte nicht gespeichert werden. Bitte gegebenenfalls neu laden.')); }
        finally { setBusy(false); }
    };
    const openDetail = async (item: Equipment) => {
        const request = ++selection.current;
        setDetail(null); setError('');
        try {
            const result = await getEquipment(item.id);
            if (request !== selection.current) return;
            setDetail(result); setInspection({ inspectedOn: todayDate(), status: result.equipment.status, comment: '' });
            commandId.current = crypto.randomUUID();
        } catch (err) { if (request === selection.current) setError(getApiErrorMessage(err, 'Gerät konnte nicht geladen werden.')); }
    };
    const submitEquipment = (event: FormEvent) => {
        event.preventDefault(); if (!editor || !canManageInventory) return;
        void run(async () => { await saveEquipment(editor.id, editor.draft); setEditor(null); await load(); }, 'Gerät gespeichert.');
    };
    const filtered = items.filter(x => (!category || x.categoryName === category) &&
        `${x.barcode} ${x.equipmentTypeName} ${x.serialNumber} ${x.manufacturer}`.toLocaleLowerCase().includes(search.toLocaleLowerCase()));

    return <div className="inventory-page">
        <header className="page-header"><div><h1>Inventar und Geräteprüfung</h1><p>Prüffristen, Einsatzbereitschaft und Prüfnachweise.</p></div>
            {canManageInventory && <button disabled={busy} onClick={() => { setDetail(null); setEditor({ id: null, draft: { ...emptyEquipment } }); }}>Gerät anlegen</button>}
        </header>
        {error && <p className="alert alert-error" role="alert">{error}</p>}
        {notice && <p role="status">{notice}</p>}
        <InventoryTransferTools kind="equipment" canImport={canManageInventory} disabled={busy || loading} onBusyChange={setBusy}
            onImported={async () => { invalidateSelection(); setDetail(null); setEditor(null); await load(); }} />
        <section className="card">
            <div className="inventory-filters">
                <label>Suche<input value={search} onChange={e => setSearch(e.target.value)} placeholder="Barcode, Typ, Seriennummer, Hersteller" /></label>
                <label>Kategorie<select value={category} onChange={e => setCategory(e.target.value)}><option value="">Alle Kategorien</option>{categories.map(x => <option key={x.id} value={x.name}>{x.name}</option>)}</select></label>
                <label className="checkbox-row"><input type="checkbox" checked={includeInactive} onChange={e => setIncludeInactive(e.target.checked)} />Deaktivierte anzeigen</label>
            </div>
            {loading ? <p role="status">Inventar wird geladen …</p> : <div className="table-scroll"><table className="log-table">
                <thead><tr><th>Barcode / Gerät</th><th>Kategorie</th><th>Hersteller / Seriennummer</th><th>Status</th><th>Letzte Prüfung</th><th>Intervall</th><th>Nächste Prüfung</th><th>Aktionen</th></tr></thead>
                <tbody>{filtered.map(item => <tr key={item.id}>
                    <td><strong>{item.barcode}</strong><br />{item.equipmentTypeName}</td><td>{item.categoryName}</td><td>{item.manufacturer}<br />{item.serialNumber}</td>
                    <td>{equipmentStatusLabels[item.status]}{!item.isActive && <strong> · Deaktiviert</strong>}</td>
                    <td>{item.lastInspectedOn ? inventoryDate(item.lastInspectedOn) : 'Noch nicht geprüft'}</td><td>{item.inspectionIntervalMonths} Monate</td>
                    <td><time dateTime={item.nextInspectionOn} className={item.isActive && item.nextInspectionOn <= todayDate() ? 'inventory-overdue' : ''}>{inventoryDate(item.nextInspectionOn)}</time></td>
                    <td><button disabled={busy} onClick={() => void openDetail(item)} aria-label={`${item.barcode} anzeigen`}>Details / Prüfungen</button>
                        {canManageInventory && item.isActive && <button disabled={busy} onClick={() => { setDetail(null); setEditor({ id: item.id, draft: { equipmentTypeId: item.equipmentTypeId, serialNumber: item.serialNumber, manufacturer: item.manufacturer, status: item.status, expectedVersion: item.version } }); }} aria-label={`${item.barcode} bearbeiten`}>Bearbeiten</button>}
                    </td>
                </tr>)}</tbody></table>{filtered.length === 0 && <p>Keine Geräte gefunden.</p>}</div>}
        </section>

        {editor && canManageInventory && <section className="card"><h2>{editor.id ? 'Gerät bearbeiten' : 'Gerät anlegen'}</h2>
            <form className="form-stack" onSubmit={submitEquipment}><fieldset disabled={busy}>
                <label>Gerätetyp<select required value={editor.draft.equipmentTypeId} onChange={e => setEditor({ ...editor, draft: { ...editor.draft, equipmentTypeId: e.target.value } })}><option value="">Bitte wählen</option>{types.map(x => <option key={x.id} value={x.id}>{x.name} · {x.categoryName}</option>)}</select></label>
                <label>Hersteller<input required maxLength={150} value={editor.draft.manufacturer} onChange={e => setEditor({ ...editor, draft: { ...editor.draft, manufacturer: e.target.value } })} /></label>
                <label>Seriennummer<input required maxLength={150} value={editor.draft.serialNumber} onChange={e => setEditor({ ...editor, draft: { ...editor.draft, serialNumber: e.target.value } })} /></label>
                {!editor.id && <label>Gerätestatus<select value={editor.draft.status} onChange={e => setEditor({ ...editor, draft: { ...editor.draft, status: e.target.value as EquipmentStatus } })}>{Object.entries(equipmentStatusLabels).map(([value, name]) => <option key={value} value={value}>{name}</option>)}</select></label>}
                <p>Der Barcode wird automatisch vergeben (KPP-001 bis KPP-999). Der erste Prüftermin ergibt sich aus Anlagedatum und Prüfintervall des Gerätetyps.</p>
                <div className="inventory-actions"><button type="submit">Gerät speichern</button><button type="button" onClick={() => setEditor(null)}>Abbrechen</button></div>
            </fieldset></form></section>}

        {detail && <section className="card" aria-label="Gerätedetails"><h2>{detail.equipment.barcode} · {detail.equipment.equipmentTypeName}</h2>
            <EquipmentBarcodeLabel id={detail.equipment.id} barcode={detail.equipment.barcode} />
            <p>ID: {detail.equipment.id}</p><p>{detail.equipment.manufacturer} · Seriennummer {detail.equipment.serialNumber}</p>
            <p>{equipmentStatusLabels[detail.equipment.status]} · {detail.equipment.isActive ? 'Aktiv' : 'Deaktiviert'}</p>
            <p>Nächste Prüfung: <strong>{inventoryDate(detail.equipment.nextInspectionOn)}</strong></p>
            <h3>Prüfanweisung</h3><p className="inventory-instructions">{detail.equipment.inspectionInstructions}</p>
            {canManageInventory && detail.equipment.isActive && <form className="form-stack" onSubmit={event => {
                event.preventDefault(); void run(async () => {
                    const result = await recordEquipmentInspection(detail.equipment.id, { ...inspection, commandId: commandId.current, expectedVersion: detail.equipment.version });
                    setDetail(result); setInspection({ inspectedOn: todayDate(), status: result.equipment.status, comment: '' }); commandId.current = crypto.randomUUID(); await load();
                }, 'Prüfung dokumentiert.');
            }}><h3>Prüfung dokumentieren</h3><fieldset disabled={busy}>
                <label>Prüfdatum<input type="date" required min={detail.equipment.lastInspectedOn ?? detail.equipment.createdOn} max={todayDate()} value={inspection.inspectedOn} onChange={e => setInspection({ ...inspection, inspectedOn: e.target.value })} /></label>
                <label>Status nach Prüfung<select value={inspection.status} onChange={e => setInspection({ ...inspection, status: e.target.value as EquipmentStatus })}>{Object.entries(equipmentStatusLabels).map(([value, name]) => <option key={value} value={value}>{name}</option>)}</select></label>
                <label>Kommentar zur Prüfung<textarea maxLength={4000} value={inspection.comment} onChange={e => setInspection({ ...inspection, comment: e.target.value })} /></label>
                <button type="submit">Prüfung speichern</button>
            </fieldset></form>}
            {canManageInventory && <button disabled={busy} onClick={() => void run(async () => { setDetail(await setEquipmentActive(detail.equipment, !detail.equipment.isActive)); await load(); }, detail.equipment.isActive ? 'Gerät deaktiviert.' : 'Gerät aktiviert.')}>
                {detail.equipment.isActive ? 'Gerät deaktivieren' : 'Gerät wieder aktivieren'}</button>}
            <h3>Prüfhistorie</h3>{detail.inspections.length === 0 ? <p>Noch keine Prüfungen dokumentiert.</p> : <ol className="inventory-history">{detail.inspections.map(x => <li key={x.id}>
                <strong>{inventoryDate(x.inspectedOn)} · {equipmentStatusLabels[x.status]}</strong><p>Geprüft von {x.inspectorName}</p><p className="inventory-instructions">{x.comment || 'Kein Kommentar'}</p>
                <details><summary>Prüfplan zum Prüfzeitpunkt</summary><p>{x.equipmentTypeName} · {x.inspectionIntervalMonths} Monate</p><p className="inventory-instructions">{x.inspectionInstructions}</p></details>
            </li>)}</ol>}
        </section>}

    </div>;
}

function EquipmentBarcodeLabel({ id, barcode }: { id: string; barcode: string }) {
    const [svg, setSvg] = useState('');
    const [failed, setFailed] = useState(false);
    useEffect(() => {
        let current = true; setSvg(''); setFailed(false);
        getEquipmentBarcodeSvg(id).then(value => { if (current) setSvg(value); })
            .catch(() => { if (current) setFailed(true); });
        return () => { current = false; };
    }, [id]);
    if (failed) return <p>Barcodeetikett konnte nicht geladen werden.</p>;
    if (!svg) return null;
    const url = `data:image/svg+xml;charset=utf-8,${encodeURIComponent(svg)}`;
    return <div className="inventory-barcode"><img src={url} alt={`Barcode ${barcode}`} /><a href={url} download={`${barcode}.svg`}>Barcodeetikett herunterladen</a></div>;
}
