import { useEffect, useRef, useState } from 'react';
import type { FormEvent } from 'react';
import { isAxiosError } from 'axios';
import { importHydrants } from '../api/hydrantImportApi';
import type { HydrantImportError, HydrantImportResponse } from '../api/hydrantImportApi';
import { getApiErrorMessage } from '../api/errors';
import csvExample from '../assets/hydranten-import.csv?raw';
import csvExampleUrl from '../assets/hydranten-import.csv?url';

interface HydrantImportModalProps {
    onClose: () => void;
    onImported: (count: number) => void;
}

export default function HydrantImportModal({ onClose, onImported }: HydrantImportModalProps) {
    const dialogRef = useRef<HTMLDialogElement>(null);
    const [file, setFile] = useState<File | null>(null);
    const [isImporting, setIsImporting] = useState(false);
    const [errors, setErrors] = useState<HydrantImportError[]>([]);

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
        if (isImporting) return;
        if (!file || file.size === 0) {
            setErrors([{ lineNumber: null, description: 'Bitte eine nicht leere CSV-Datei auswählen.' }]);
            return;
        }
        if (file.size > 5 * 1024 * 1024) {
            setErrors([{ lineNumber: null, description: 'Die CSV-Datei darf höchstens 5 MB groß sein.' }]);
            return;
        }

        setErrors([]);
        setIsImporting(true);
        try {
            const result = await importHydrants(file);
            onImported(result.importedCount);
        } catch (error: unknown) {
            const details = isAxiosError<HydrantImportResponse>(error) ? error.response?.data?.errors : undefined;
            setErrors(details?.length ? details : [{
                lineNumber: null,
                description: getApiErrorMessage(error, 'Die CSV-Datei konnte nicht importiert werden.'),
            }]);
        } finally {
            setIsImporting(false);
        }
    };

    return (
        <dialog ref={dialogRef} className="hydrant-modal hydrant-import-modal" aria-modal="true"
            aria-labelledby="hydrant-import-title" aria-describedby="hydrant-import-description"
            onCancel={event => { event.preventDefault(); if (!isImporting) onClose(); }}
            onClick={event => { if (event.target === event.currentTarget && !isImporting) onClose(); }}>
            <div className="hydrant-modal-content">
                <header className="hydrant-modal-header">
                    <h2 id="hydrant-import-title">Hydranten aus CSV importieren</h2>
                    <button type="button" className="hydrant-close-button" disabled={isImporting}
                        onClick={onClose} aria-label="Schließen">×</button>
                </header>
                <p id="hydrant-import-description">
                    UTF-8-Datei mit Semikolon als Trennzeichen und Dezimalpunkt für Zahlen.
                    Alle Spalten außer <code>notes</code> (Notizen) sind Pflichtfelder.
                    Die Spalte <code>notes</code> darf leer bleiben oder ganz entfallen.
                    Koordinaten werden nicht auf ihre geografische Gültigkeit geprüft.
                </p>
                <p>Es werden neue Hydranten angelegt. Bestehende Einträge werden nicht geändert.
                    Bei fehlerhaften Zeilen wird die gesamte Datei abgelehnt.</p>
                <h3>CSV-Beispiel</h3>
                <pre className="hydrant-csv-example"><code>{csvExample}</code></pre>
                <a href={csvExampleUrl} download="hydranten-import.csv">CSV-Beispiel herunterladen</a>
                <dl className="hydrant-csv-values">
                    <dt>type (Typ)</dt>
                    <dd>1 Unterflurhydrant · 2 Überflurhydrant ohne Fallmantel · 3 Überflurhydrant mit Fallmantel · 4 Wandhydrant</dd>
                    <dt>waterSource (Wasserquelle)</dt>
                    <dd>1 Trinkwassernetz · 2 Löschwasserteich · 3 Löschwasserbrunnen · 4 Zisterne</dd>
                    <dt>status</dt>
                    <dd>1 Betriebsbereit · 2 Defekt · 3 Gesperrt · 4 In Wartung</dd>
                </dl>
                <p>Text mit Semikolon oder Zeilenumbruch in doppelte Anführungszeichen setzen.
                    Höchstens 5 MB und 10.000 Hydranten pro Import.</p>
                {errors.length > 0 && <div role="alert" className="hydrant-import-errors">
                    <p>Der Import wurde nicht durchgeführt:</p>
                    <ul>{errors.map((error, index) => <li key={index}>
                        {error.lineNumber !== null && `Zeile ${error.lineNumber}: `}{error.description}
                    </li>)}</ul>
                </div>}
                <form onSubmit={submit} className="hydrant-form">
                    <label htmlFor="hydrant-csv-file">CSV-Datei auswählen</label>
                    <input id="hydrant-csv-file" type="file" accept=".csv,text/csv" disabled={isImporting}
                        onChange={event => { setFile(event.target.files?.[0] ?? null); setErrors([]); }} />
                    <div className="hydrant-form-actions">
                        <button type="submit" className="hydrant-save-button" disabled={isImporting || !file}>
                            {isImporting ? 'Wird importiert…' : 'CSV importieren'}
                        </button>
                        <button type="button" className="btn-secondary hydrant-cancel-button" disabled={isImporting} onClick={onClose}>Abbrechen</button>
                    </div>
                </form>
            </div>
        </dialog>
    );
}
