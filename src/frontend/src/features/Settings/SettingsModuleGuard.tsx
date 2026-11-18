import { useEffect, useState } from 'react';
import { Link, Outlet, useLocation } from 'react-router-dom';
import { useRole } from '../../hooks/useRole';
import { getSettingsModuleStatuses } from './settingsApi';
import type { SettingsModuleStatus } from './settingsApi';

export default function SettingsModuleGuard() {
  const { pathname } = useLocation();
  const isSettingsPage = /^\/settings\/?$/i.test(pathname);
  const { isAdmin } = useRole();
  const [revision, setRevision] = useState(0);
  const [state, setState] = useState<{ path: string; modules: SettingsModuleStatus[]; failed: boolean } | null>(null);
  useEffect(() => {
    if (isSettingsPage) return;
    let active = true;
    void getSettingsModuleStatuses().then(modules => {
      if (active) setState({ path: pathname, modules, failed: false });
    }).catch(() => {
      if (active) setState({ path: pathname, modules: [], failed: true });
    });
    return () => { active = false; };
  }, [pathname, revision, isSettingsPage]);

  useEffect(() => {
    const refresh = () => { setRevision(value => value + 1); };
    window.addEventListener('focus', refresh);
    return () => window.removeEventListener('focus', refresh);
  }, []);

  if (isSettingsPage) return <Outlet />;
  if (!state || state.path !== pathname) return <p role="status">Modulkonfiguration wird geprüft …</p>;
  const blocked = state.modules.filter(module => !module.isAvailable && module.pagePaths.some(path =>
    pathname.toLowerCase() === path.toLowerCase() || pathname.toLowerCase().startsWith(`${path.toLowerCase()}/`)));
  if (!state.failed && blocked.length === 0) return <Outlet />;
  return <section className="card"><h1>Modul nicht verfügbar</h1>
    {state.failed ? <p role="alert">Die Modulkonfiguration konnte nicht geprüft werden.</p>
      : blocked.map(module => <div key={module.id}><p role="alert">Für das Modul {module.name} fehlen gültige Pflichtparameter.</p>
        <ul>{module.missingParameters.map(parameter => <li key={parameter}>{parameter}</li>)}</ul></div>)}
    {isAdmin && <p><Link to="/settings">Settings konfigurieren</Link></p>}
    <button onClick={() => { setState(null); setRevision(value => value + 1); }}>Erneut prüfen</button>
  </section>;
}
