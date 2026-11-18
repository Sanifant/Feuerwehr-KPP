import { useEffect, useRef, useState } from 'react';
import type { FormEvent } from 'react';
import { getApiErrorMessage } from '../../api/errors';
import { createHydrantInspection } from './api/hydrantInspectionApi';
import type { HydrantInspection } from './api/hydrantInspectionApi';
import type { Hydrant, HydrantStatus } from './models/HydrantModels';
import { hydrantStatusLabels } from './models/hydrantStatusLabels';

export default function HydrantInspectionModal({ hydrant, onClose, onCreated }: {
  hydrant: Hydrant;
  onClose: () => void;
  onCreated: (inspection: HydrantInspection) => void;
}) {
  const dialogRef = useRef<HTMLDialogElement>(null);
  const running = useRef(false);
  const [newStatus, setNewStatus] = useState<HydrantStatus>(hydrant.status);
  const [comment, setComment] = useState('');
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    const dialog = dialogRef.current;
    if (!dialog) return;
    const previousFocus = document.activeElement;
    const previousOverflow = document.body.style.overflow;
    dialog.showModal();
    document.body.style.overflow = 'hidden';
    return () => {
      dialog.close();
      document.body.style.overflow = previousOverflow;
      if (previousFocus instanceof HTMLElement) previousFocus.focus();
    };
  }, []);

  const submit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (running.current) return;
    if (comment.length > 500) { setError('Der Kommentar darf maximal 500 Zeichen enthalten.'); return; }
    running.current = true;
    setSaving(true);
    setError('');
    try {
      const inspection = await createHydrantInspection(hydrant.id, { comment: comment || null, newStatus });
      onCreated(inspection);
    } catch (err) {
      setError(getApiErrorMessage(err, 'Die Inspektion konnte nicht gespeichert werden. Bitte erneut versuchen.'));
    } finally {
      running.current = false;
      setSaving(false);
    }
  };

  return <dialog ref={dialogRef} className="hydrant-modal hydrant-inspection-modal" aria-labelledby="hydrant-inspection-dialog-title"
    aria-modal="true" onCancel={event => { event.preventDefault(); if (!running.current) onClose(); }}
    onClick={event => { if (event.target === event.currentTarget && !running.current) onClose(); }}>
    <div className="hydrant-modal-content">
      <header className="hydrant-modal-header"><h2 id="hydrant-inspection-dialog-title">Inspektion erfassen</h2>
        <button type="button" className="hydrant-close-button" aria-label="Schließen" disabled={saving} onClick={onClose}>×</button></header>
      <div className="hydrant-inspection-body">
        <p>Hydrant {hydrant.id}: {hydrant.address.street} {hydrant.address.houseNumber}, {hydrant.address.city}</p>
        <form onSubmit={event => void submit(event)} className="hydrant-form">
          {error && <p role="alert">{error}</p>}
          <fieldset disabled={saving}>
            <label htmlFor="inspection-new-status">Neuer Status</label>
            <select id="inspection-new-status" value={newStatus} onChange={event => setNewStatus(Number(event.target.value) as HydrantStatus)}>
              {Object.entries(hydrantStatusLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
            </select>
            <label htmlFor="inspection-comment">Kommentar (optional)</label>
            <textarea id="inspection-comment" maxLength={500} value={comment} onChange={event => setComment(event.target.value)}
              aria-describedby="inspection-comment-limit" rows={4} />
            <small id="inspection-comment-limit">{comment.length}/500 Zeichen</small>
          </fieldset>
          <div className="hydrant-form-actions"><button type="submit" className="hydrant-save-button" disabled={saving}>
            {saving ? 'Wird gespeichert…' : 'Inspektion speichern'}</button>
            <button type="button" className="btn-secondary hydrant-cancel-button" disabled={saving} onClick={onClose}>Abbrechen</button></div>
        </form>
      </div>
    </div>
  </dialog>;
}
