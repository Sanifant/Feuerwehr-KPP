import { useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { useRole } from '../hooks/useRole';
import { getApiErrorMessage } from '../api/errors';
import { createInventoryCategory, getInventoryCategories } from '../api/inventoryApi';
import type { InventoryCategory } from '../api/inventoryApi';
import './InventoryPage.css';
import InventoryTransferTools from './InventoryTransferTools';

export default function InventoryCategoryAdmin() {
    const { isAdmin } = useRole();
    const [categories, setCategories] = useState<InventoryCategory[]>([]);
    const [name, setName] = useState('');
    const [loading, setLoading] = useState(true);
    const [busy, setBusy] = useState(false);
    const [error, setError] = useState('');
    const [notice, setNotice] = useState('');

    useEffect(() => {
        if (!isAdmin) return;
        let current = true;
        setLoading(true);
        getInventoryCategories().then(list => { if (current) { setCategories(list); setError(''); } })
            .catch(err => { if (current) setError(getApiErrorMessage(err, 'Inventarkategorien konnten nicht geladen werden.')); })
            .finally(() => { if (current) setLoading(false); });
        return () => { current = false; };
    }, [isAdmin]);

    if (!isAdmin) return <section className="card"><h1>Inventarkategorien verwalten</h1><p role="alert">Nur für Administratoren.</p></section>;

    const submit = async (event: FormEvent) => {
        event.preventDefault();
        if (busy || !name.trim()) return;
        setBusy(true); setError(''); setNotice('');
        try {
            const saved = await createInventoryCategory(name.trim());
            setCategories(previous => [...previous, saved].sort((a, b) => a.name.localeCompare(b.name, 'de')));
            setName(''); setNotice('Kategorie angelegt.');
        } catch (err) { setError(getApiErrorMessage(err, 'Die Kategorie konnte nicht angelegt werden.')); }
        finally { setBusy(false); }
    };

    return <div className="inventory-page">
        <header className="page-header"><div><h1>Inventarkategorien verwalten</h1><p>Kategorien für die Zuordnung der Gerätetypen anlegen.</p></div></header>
        {error && <p className="alert alert-error" role="alert">{error}</p>}
        {notice && <p role="status">{notice}</p>}
        <InventoryTransferTools kind="categories" canImport={isAdmin} disabled={busy || loading} onBusyChange={setBusy}
            onImported={async () => { setCategories(await getInventoryCategories()); setName(''); }} />
        <section className="card"><h2>Vorhandene Kategorien</h2>
            {loading ? <p role="status">Inventarkategorien werden geladen …</p> : categories.length === 0 ? <p>Noch keine Inventarkategorien angelegt.</p> : <ul>{categories.map(x => <li key={x.id}>{x.name}</li>)}</ul>}
        </section>
        <section className="card"><h2>Kategorie anlegen</h2><form className="form-stack" onSubmit={submit}><fieldset disabled={busy || loading}>
            <label>Neue Kategorie<input required maxLength={100} value={name} onChange={e => setName(e.target.value)} /></label>
            <button disabled={!name.trim()}>Kategorie anlegen</button>
        </fieldset></form></section>
    </div>;
}
