import { useEffect, useRef, useState } from 'react';
import type { FormEvent } from 'react';

export interface MapSymbolPlacementDetails {
    radioCallName: string;
    strength: string;
    note: string;
}

interface Props {
    symbolName: string;
    busy: boolean;
    canSave: boolean;
    error: string;
    onSave: (details: MapSymbolPlacementDetails) => Promise<void>;
    onClose: () => void;
}

export default function MapSymbolPlacementModal({ symbolName, busy, canSave, error, onSave, onClose }: Props) {
    const dialogRef = useRef<HTMLDialogElement>(null);
    const callNameRef = useRef<HTMLInputElement>(null);
    const [radioCallName, setRadioCallName] = useState('');
    const [strength, setStrength] = useState('');
    const [note, setNote] = useState('');
    const [validationError, setValidationError] = useState('');

    useEffect(() => {
        const dialog = dialogRef.current;
        if (!dialog) return;
        const previousFocus = document.activeElement;
        const previousOverflow = document.body.style.overflow;
        dialog.showModal();
        callNameRef.current?.focus();
        document.body.style.overflow = 'hidden';
        return () => {
            dialog.close();
            document.body.style.overflow = previousOverflow;
            if (previousFocus instanceof HTMLElement && previousFocus.isConnected) previousFocus.focus();
        };
    }, []);

    const save = (event: FormEvent) => {
        event.preventDefault();
        if (busy || !canSave) return;
        if (!radioCallName.trim()) {
            setValidationError('Bitte einen Funkrufnamen angeben.');
            callNameRef.current?.focus();
            return;
        }
        setValidationError('');
        void onSave({ radioCallName: radioCallName.trim(), strength: strength.trim(), note: note.trim() });
    };

    return <dialog ref={dialogRef} className="modal map-symbol-placement-modal" aria-modal="true" aria-labelledby="map-symbol-placement-title"
        onCancel={event => { event.preventDefault(); if (!busy) onClose(); }}>
        <header className="modal-header">
            <h2 id="map-symbol-placement-title">Symbol platzieren</h2>
            <button type="button" className="modal-close" disabled={busy} onClick={onClose} aria-label="Schließen">×</button>
        </header>
        <p>{symbolName}</p>
        <form onSubmit={save} noValidate>
            <fieldset disabled={busy} className="map-symbol-placement-fields">
                <label>Funkrufname *<input ref={callNameRef} value={radioCallName} required maxLength={120}
                    aria-invalid={!!validationError} aria-describedby={validationError ? 'map-symbol-placement-validation' : undefined}
                    onChange={event => { setRadioCallName(event.target.value); setValidationError(''); }} /></label>
                <label>Stärke<input value={strength} maxLength={120} placeholder="z. B. 1/8/9" onChange={event => setStrength(event.target.value)} /></label>
                <label>Bemerkung<textarea value={note} maxLength={2000} rows={3} onChange={event => setNote(event.target.value)} /></label>
            </fieldset>
            {validationError && <p role="alert" id="map-symbol-placement-validation">{validationError}</p>}
            {error && <p role="alert">{error}</p>}
            {!canSave && <p role="alert">Die Bearbeitungssperre ist nicht mehr gültig. Bitte den Dialog schließen und die Bearbeitung erneut übernehmen.</p>}
            <div className="incident-actions">
                <button type="button" className="btn-secondary" disabled={busy} onClick={onClose}>Abbrechen</button>
                <button type="submit" disabled={busy || !canSave}>{busy ? 'Wird gespeichert …' : 'Symbol platzieren'}</button>
            </div>
        </form>
    </dialog>;
}
