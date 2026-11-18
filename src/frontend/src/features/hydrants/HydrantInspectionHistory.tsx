import { useEffect, useState } from 'react';
import { getApiErrorMessage } from '../../api/errors';
import { getHydrantInspections } from './api/hydrantInspectionApi';
import type { HydrantInspection } from './api/hydrantInspectionApi';
import { hydrantStatusLabels } from './models/hydrantStatusLabels';

export default function HydrantInspectionHistory({ hydrantId, revision }: { hydrantId: number; revision: number }) {
  const [inspections, setInspections] = useState<HydrantInspection[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [reload, setReload] = useState(0);
  useEffect(() => {
    let active = true;
    setLoading(true);
    setError('');
    void getHydrantInspections(hydrantId).then(items => {
      if (active) setInspections([...items].sort((a, b) => Date.parse(b.inspectedAtUtc) - Date.parse(a.inspectedAtUtc)));
    }).catch(err => {
      if (active) setError(getApiErrorMessage(err, 'Inspektionen konnten nicht geladen werden.'));
    }).finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, [hydrantId, revision, reload]);

  return <section className="hydrant-inspections" aria-labelledby={`hydrant-inspections-${hydrantId}`} aria-busy={loading}>
    <header><h2 id={`hydrant-inspections-${hydrantId}`}>Inspektionen – Hydrant {hydrantId}</h2>
      <button type="button" className="btn-secondary" disabled={loading} onClick={() => setReload(value => value + 1)}>Prüfungen aktualisieren</button></header>
    {loading ? <p role="status">Inspektionen werden geladen …</p>
      : error ? <p role="alert">{error}</p>
      : inspections.length === 0 ? <p>Noch keine Inspektionen vorhanden.</p>
      : <div className="hydrant-inspection-table"><table aria-label={`Inspektionen für Hydrant ${hydrantId}`}>
        <thead><tr><th>Prüfdatum</th><th>Prüfer</th><th>Neuer Status</th></tr></thead>
        <tbody>{inspections.map(inspection => <tr key={inspection.id}>
          <td><time dateTime={inspection.inspectedOn}>{new Date(`${inspection.inspectedOn}T00:00:00`).toLocaleDateString('de-DE')}</time></td>
          <td>{inspection.inspectorName}</td><td>{hydrantStatusLabels[inspection.newStatus]}</td>
        </tr>)}</tbody>
      </table></div>}
  </section>;
}
