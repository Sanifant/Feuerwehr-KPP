import { useEffect, useState } from 'react';
import { useRole } from '../hooks/useRole';
import { getDiaryCategoryAdmin, upsertDiaryCategory } from '../api/incidentApi';

export function DiaryCategoryAdmin() {
    const { isAdmin } = useRole();
    const [categories, setCategories] = useState<any[]>([]);
    const [draft, setDraft] = useState({ code: '', name: '', isActive: true });
    const [status, setStatus] = useState('');

    const load = async () => {
        const data = await getDiaryCategoryAdmin();
        setCategories(data);
    };

    useEffect(() => {
        if (!isAdmin) {
            return;
        }

        load().catch(() => setStatus('Kategorien konnten nicht geladen werden.'));
    }, [isAdmin]);

    if (!isAdmin) {
        return <div className="card"><h2>Tagebuchkategorien</h2><p>Nur für Administratoren.</p></div>;
    }

    const saveCategory = async () => {
        if (!draft.code.trim() || !draft.name.trim()) {
            setStatus('Code und Name sind erforderlich.');
            return;
        }

        try {
            await upsertDiaryCategory({ code: draft.code.trim(), name: draft.name.trim(), isActive: draft.isActive });
            setStatus('Kategorie gespeichert.');
            setDraft({ code: '', name: '', isActive: true });
            await load();
        } catch {
            setStatus('Kategorie konnte nicht gespeichert werden.');
        }
    };

    return (
        <div className="incident-page">
            <section className="card">
                <h2>Tagebuchkategorien verwalten</h2>
                <p>{status}</p>
                <div className="diary-grid">
                    <label>
                        Code
                        <input value={draft.code} onChange={(e) => setDraft((prev) => ({ ...prev, code: e.target.value }))} />
                    </label>
                    <label>
                        Name
                        <input value={draft.name} onChange={(e) => setDraft((prev) => ({ ...prev, name: e.target.value }))} />
                    </label>
                    <label className="checkbox-row">
                        <input type="checkbox" checked={draft.isActive} onChange={(e) => setDraft((prev) => ({ ...prev, isActive: e.target.checked }))} />
                        Aktiv
                    </label>
                </div>
                <button type="button" onClick={saveCategory}>Speichern</button>
            </section>

            <section className="card">
                <h3>Vorhandene Kategorien</h3>
                <table className="log-table">
                    <thead>
                        <tr>
                            <th>Code</th>
                            <th>Name</th>
                            <th>Status</th>
                        </tr>
                    </thead>
                    <tbody>
                        {categories.map((cat) => (
                            <tr key={cat.id}>
                                <td>{cat.code}</td>
                                <td>{cat.name}</td>
                                <td>{cat.isActive ? 'Aktiv' : 'Deaktiviert'}</td>
                            </tr>
                        ))}
                    </tbody>
                </table>
            </section>
        </div>
    );
}

export default DiaryCategoryAdmin;
