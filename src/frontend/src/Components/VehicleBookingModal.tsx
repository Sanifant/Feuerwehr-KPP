import { useId, useRef, useState } from 'react';
import type { FormEvent } from 'react';
import { isAxiosError } from 'axios';
import { useRole } from '../hooks/useRole';
import { getApiErrorMessage } from '../api/errors';
import { reserveVehicle, vehicleDate } from '../api/vehicleApi';
import type { Vehicle } from '../api/vehicleApi';
import VehicleModal from './VehicleModal';

export default function VehicleBookingModal({ vehicle, onClose, onSaved, onRefresh }: {
    vehicle: Vehicle; onClose: () => void; onSaved: (direct: boolean) => Promise<void>; onRefresh: () => Promise<void>;
}) {
    const { canRequestVehicles } = useRole();
    const today = vehicleDate(new Date());
    const [draft, setDraft] = useState({ from: today, to: today, comment: '', direct: vehicle.canBookDirectly, commandId: crypto.randomUUID() });
    const [busy, setBusy] = useState(false);
    const [error, setError] = useState('');
    const submitting = useRef(false);
    const commentHint = useId();
    const submit = async (event: FormEvent) => {
        event.preventDefault();
        if (submitting.current) return;
        if (draft.to < draft.from) { setError('Das Bis-Datum darf nicht vor dem Von-Datum liegen.'); return; }
        submitting.current = true; setBusy(true); setError('');
        try {
            await reserveVehicle(vehicle.id, { commandId: draft.commandId, from: draft.from, to: draft.to, comment: draft.comment }, draft.direct);
            onClose();
            await onSaved(draft.direct);
        } catch (err) {
            if (isAxiosError(err) && err.response?.status === 409) await onRefresh();
            setError(getApiErrorMessage(err, 'Die Buchung konnte nicht gespeichert werden. Bitte erneut versuchen.'));
        } finally { submitting.current = false; setBusy(false); }
    };
    return <VehicleModal title={`MTW buchen: ${vehicle.radioCallSign}`} busy={busy} onClose={onClose}>
        <form onSubmit={event => void submit(event)}>
            {error && <p className="alert alert-error" role="alert">{error}</p>}
            <fieldset disabled={busy}>
                <label>Fahrzeug / Funkrufname<input readOnly value={vehicle.radioCallSign} /></label>
                <p>{vehicle.fireDepartmentName}{vehicle.vehicleTypeName && ` · ${vehicle.vehicleTypeName}`}</p>
                <label>Von-Datum<input type="date" required min={today} value={draft.from}
                    onChange={event => setDraft({ ...draft, from: event.target.value, commandId: crypto.randomUUID() })} /></label>
                <label>Bis-Datum (einschließlich)<input type="date" required min={draft.from} value={draft.to}
                    onChange={event => setDraft({ ...draft, to: event.target.value, commandId: crypto.randomUUID() })} /></label>
                <label>Kommentar / Buchungsgrund (optional)<textarea maxLength={2000} rows={4} value={draft.comment}
                    onChange={event => setDraft({ ...draft, comment: event.target.value, commandId: crypto.randomUUID() })} aria-describedby={commentHint} /></label>
                <small id={commentHint}>Optional: Beschreibe kurz, wofür du das Fahrzeug benötigst. Der zuständige Gerätewart erhält diese Angabe mit deiner Anfrage.</small>
                {vehicle.canBookDirectly && canRequestVehicles && <label><input type="checkbox" checked={draft.direct}
                    onChange={event => setDraft({ ...draft, direct: event.target.checked, commandId: crypto.randomUUID() })} /> Direkt verbindlich buchen</label>}
                <p>{draft.direct ? 'Du buchst das Fahrzeug als zuständiger Gerätewart verbindlich.' : 'Die Anfrage wird erst durch die Bestätigung des zuständigen Gerätewarts verbindlich.'}</p>
            </fieldset>
            <footer><button type="button" className="btn-secondary" disabled={busy} onClick={onClose}>Abbrechen</button>
                <button disabled={busy}>{busy ? 'Wird gespeichert …' : draft.direct ? 'Direkt buchen' : 'Reservierung anfragen'}</button></footer>
        </form>
    </VehicleModal>;
}
