import { useCallback, useEffect, useRef, useState } from 'react';
import type { FormEvent } from 'react';
import { useRole } from '../../hooks/useRole';
import { getApiErrorMessage } from '../../api/errors';
import VehicleModal from '../../Components/VehicleModal';
import { createSetting, deleteSetting, getSettingDefinitions, getSettings, settingTypeLabels, updateSetting } from './settingsApi';
import type { Setting, SettingDefinition, SettingType } from './settingsApi';
import './SettingsPage.css';

interface Editor {
  id: string | null;
  key: string;
  name: string;
  value: string;
  type: SettingType;
}

export default function SettingsPage() {
  const { isAdmin } = useRole();
  const [settings, setSettings] = useState<Setting[]>([]);
  const [definitions, setDefinitions] = useState<SettingDefinition[]>([]);
  const [editor, setEditor] = useState<Editor | null>(null);
  const [deleting, setDeleting] = useState<Setting | null>(null);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const [modalError, setModalError] = useState('');
  const running = useRef(false);
  const load = useCallback(async () => {
    if (!isAdmin) return;
    setLoading(true);
    try {
      const [values, catalog] = await Promise.all([getSettings(), getSettingDefinitions()]);
      setSettings(values); setDefinitions(catalog); setError('');
    }
    catch (err) { setError(getApiErrorMessage(err, 'Settings konnten nicht geladen werden.')); }
    finally { setLoading(false); }
  }, [isAdmin]);

  useEffect(() => { void load(); }, [load]);

  if (!isAdmin) return <section className="card"><h1>Settings</h1>
    <p role="alert">Nur Administratoren dürfen Settings verwalten.</p></section>;

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    if (!editor || running.current) return;
    if (!editor.key) {
      setModalError('Bitte einen Parameter auswählen.');
      return;
    }
    running.current = true;
    setBusy(true);
    setModalError('');
    try {
      if (editor.id) await updateSetting(editor.id, editor.value);
      else await createSetting({ key: editor.key, value: editor.value });
      setEditor(null);
      await load();
    } catch (err) { setModalError(getApiErrorMessage(err, 'Parameter konnte nicht gespeichert werden.')); }
    finally { running.current = false; setBusy(false); }
  };

  const remove = async () => {
    if (!deleting || running.current) return;
    running.current = true;
    setBusy(true);
    setModalError('');
    try { await deleteSetting(deleting.id); setDeleting(null); await load(); }
    catch (err) { setModalError(getApiErrorMessage(err, 'Parameter konnte nicht gelöscht werden.')); }
    finally { running.current = false; setBusy(false); }
  };

  const available = definitions.filter(definition => !settings.some(setting => setting.key === definition.key));
  return <div className="settings-page">
    <header className="page-header"><div><h1>Settings</h1><p>Globale Parameter für alle Feuerwehren verwalten.</p></div>
      <button disabled={busy || loading || available.length === 0} onClick={() => {
        setModalError(''); setEditor({ id: null, key: '', name: '', value: '', type: 0 });
      }}>Parameter anlegen</button></header>
    {error && <p role="alert" className="alert alert-error">{error}</p>}
    <section className="card">
      <button className="btn-secondary" disabled={busy || loading} onClick={() => void load()}>Aktualisieren</button>
      {loading && <p role="status">Settings werden geladen …</p>}
      {!loading && !error && settings.length === 0 && <p>Noch keine Parameter vorhanden.</p>}
      {!loading && !error && definitions.length === 0 && <p>Es sind noch keine Parameter durch Module definiert.</p>}
      <div className="settings-table"><table><thead><tr>
        <th>Parametername</th><th>Wert</th><th>Geändert am</th><th>Geändert von</th><th>Aktionen</th>
      </tr></thead><tbody>{settings.map(setting => <tr key={setting.id}>
        <td>{setting.name}</td><td className="settings-value">{setting.value}</td>
        <td><time dateTime={setting.modifiedOn}>{new Date(setting.modifiedOn).toLocaleString('de-DE')}</time></td>
        <td>{setting.modifiedByName || 'Gelöschter Benutzer'}</td>
        <td><div className="settings-actions">
          <button className="btn-secondary" disabled={busy || loading} aria-label={`${setting.name} bearbeiten`}
            onClick={() => { setModalError(''); setEditor(setting); }}>Bearbeiten</button>
          <button className="btn-secondary" disabled={busy || loading} aria-label={`${setting.name} löschen`}
            onClick={() => { setModalError(''); setDeleting(setting); }}>Löschen</button>
        </div></td>
      </tr>)}</tbody></table></div>
    </section>
    {editor && <VehicleModal title={editor.id ? 'Parameter bearbeiten' : 'Parameter anlegen'} busy={busy}
      onClose={() => setEditor(null)}>
      <form onSubmit={event => void submit(event)}>
        {modalError && <p role="alert" className="alert alert-error">{modalError}</p>}
        {editor.id ? <label>Parametername<input readOnly value={editor.name} /></label>
          : <label>Parametername<select required disabled={busy} value={editor.key} onChange={event => {
            const definition = available.find(item => item.key === event.target.value);
            setEditor({ ...editor, key: definition?.key ?? '', name: definition?.name ?? '', type: definition?.type ?? 0 });
          }}>
            <option value="">Parameter auswählen</option>
            {available.map(definition => <option key={definition.key} value={definition.key}>
              {definition.moduleName}: {definition.name}{definition.mandatory ? ' (Pflichtparameter)' : ''}
            </option>)}
          </select></label>}
        <label>Typ<input readOnly value={settingTypeLabels[editor.type]} /></label>
        <label>Wert<input disabled={busy || !editor.key} type="text"
          inputMode={editor.type === 1 ? 'numeric' : editor.type === 2 ? 'email' : undefined}
          value={editor.value} onChange={event => setEditor({ ...editor, value: event.target.value })} /></label>
        <footer><button type="button" className="btn-secondary" disabled={busy} onClick={() => setEditor(null)}>Abbrechen</button>
          <button disabled={busy || !editor.key}>Speichern</button></footer>
      </form>
    </VehicleModal>}
    {deleting && <VehicleModal title="Parameter löschen" busy={busy} onClose={() => setDeleting(null)}>
      {modalError && <p role="alert" className="alert alert-error">{modalError}</p>}
      <p>Parameter „{deleting.name}“ löschen?</p>
      <footer><button type="button" className="btn-secondary" disabled={busy} onClick={() => setDeleting(null)}>Abbrechen</button>
        <button disabled={busy} onClick={() => void remove()}>Löschen bestätigen</button></footer>
    </VehicleModal>}
  </div>;
}
