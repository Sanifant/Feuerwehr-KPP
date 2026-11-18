import { useCallback, useEffect, useRef, useState } from 'react';
import type { FormEvent } from 'react';
import { useRole } from '../../hooks/useRole';
import { getApiErrorMessage } from '../../api/errors';
import VehicleModal from '../../Components/VehicleModal';
import { createSetting, deleteSetting, getSettings, settingTypeLabels, updateSetting } from './settingsApi';
import type { Setting, SettingType } from './settingsApi';
import './SettingsPage.css';

interface Editor {
  id: string | null;
  name: string;
  value: string;
  type: SettingType;
}

export default function SettingsPage() {
  const { isAdmin } = useRole();
  const [settings, setSettings] = useState<Setting[]>([]);
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
    try { setSettings(await getSettings()); setError(''); }
    catch (err) { setError(getApiErrorMessage(err, 'Settings konnten nicht geladen werden.')); }
    finally { setLoading(false); }
  }, [isAdmin]);

  useEffect(() => { void load(); }, [load]);

  if (!isAdmin) return <section className="card"><h1>Settings</h1>
    <p role="alert">Nur Administratoren dürfen Settings verwalten.</p></section>;

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    if (!editor || running.current) return;
    if (!editor.name.trim() || !editor.value.trim()) {
      setModalError('Bitte Name und Wert angeben.');
      return;
    }
    if (editor.type === 1 && (!/^[0-9]+$/.test(editor.value) || !/[1-9]/.test(editor.value))) {
      setModalError('Bitte eine positive natürliche Zahl größer als Null ohne Vorzeichen oder Dezimalstellen angeben.');
      return;
    }
    running.current = true;
    setBusy(true);
    setModalError('');
    try {
      if (editor.id) await updateSetting(editor.id, editor.value);
      else await createSetting({ name: editor.name.trim(), value: editor.value, type: editor.type });
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

  const hydrantEmail = editor?.name.trim().toLocaleLowerCase() === 'hydrant benachrichtigung';
  return <div className="settings-page">
    <header className="page-header"><div><h1>Settings</h1><p>Globale Parameter für alle Feuerwehren verwalten.</p></div>
      <button disabled={busy || loading} onClick={() => {
        setModalError(''); setEditor({ id: null, name: '', value: '', type: 0 });
      }}>Parameter anlegen</button></header>
    {error && <p role="alert" className="alert alert-error">{error}</p>}
    <section className="card">
      <button className="btn-secondary" disabled={busy || loading} onClick={() => void load()}>Aktualisieren</button>
      {loading && <p role="status">Settings werden geladen …</p>}
      {!loading && !error && settings.length === 0 && <p>Noch keine Parameter vorhanden.</p>}
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
        <label>Parametername<input required maxLength={200} readOnly={!!editor.id} disabled={busy} value={editor.name}
          onChange={event => {
            const name = event.target.value;
            setEditor({ ...editor, name, type: name.trim().toLocaleLowerCase() === 'hydrant benachrichtigung' ? 2 : editor.type });
          }} /></label>
        <label>Typ<select disabled={busy || !!editor.id || hydrantEmail} value={editor.type}
          onChange={event => setEditor({ ...editor, type: Number(event.target.value) as SettingType })}>
          <option value={0}>{settingTypeLabels[0]}</option><option value={1}>{settingTypeLabels[1]}</option>
          <option value={2}>{settingTypeLabels[2]}</option>
        </select></label>
        <label>Wert<input required disabled={busy} type={editor.type === 2 ? 'email' : 'text'}
          inputMode={editor.type === 1 ? 'numeric' : undefined}
          pattern={editor.type === 1 ? '[0-9]*[1-9][0-9]*' : undefined}
          value={editor.value} onChange={event => setEditor({ ...editor, value: event.target.value })} /></label>
        <footer><button type="button" className="btn-secondary" disabled={busy} onClick={() => setEditor(null)}>Abbrechen</button>
          <button disabled={busy}>Speichern</button></footer>
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
