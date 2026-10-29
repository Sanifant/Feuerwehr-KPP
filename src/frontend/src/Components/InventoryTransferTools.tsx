import { useRef, useState } from 'react';
import { isAxiosError } from 'axios';
import { getApiErrorMessage } from '../api/errors';
import { exportInventoryData, importInventoryData } from '../api/inventoryTransferApi';
import type { InventoryImportError, InventoryImportResponse, InventoryTransferKind } from '../api/inventoryTransferApi';

const formats = {
    equipment: {
        label: 'Inventar', extension: 'csv', filename: 'inventar.csv', mime: 'text/csv;charset=utf-8',
        description: 'Excel-kompatible UTF-8-CSV mit Semikolon. Vorhandene Barcodes ersetzen die Stammdaten unter derselben ID; Prüfstatus, Aktivzustand und gespeicherte Prüfungsdaten bleiben erhalten. Ein neuer Gerätetyp berechnet den nächsten Termin neu. Unbekannte Barcodes werden aus der Datei übernommen. Der Export umfasst auch deaktivierte Geräte.',
        example: '\uFEFFbarcode;equipmentType;category;serialNumber;manufacturer;status;isActive;createdOn;lastInspectedOn\r\nKPP-001;Strahlrohr;Armaturen;SN-001;Hersteller;Einsatzklar;true;;\r\n',
    },
    types: {
        label: 'Gerätetypen', extension: 'json', filename: 'geraetetypen.json', mime: 'application/json',
        description: 'JSON-Liste. Vorhandene Gerätetypen werden über ihre ID oder über Kategorie und Typname abgeglichen. Kategorien müssen bereits existieren. Geänderte Prüfpläne gelten für alle zugeordneten Geräte; bestehende Prüfprotokolle bleiben erhalten.',
        example: JSON.stringify([{ name: 'Strahlrohr', categoryName: 'Armaturen', inspectionIntervalMonths: 12, inspectionInstructions: 'Sicht- und Funktionsprüfung nach Herstellerangaben.' }], null, 2),
    },
    categories: {
        label: 'Inventarkategorien', extension: 'json', filename: 'inventarkategorien.json', mime: 'application/json',
        description: 'JSON-Liste. Vorhandene Kategorien werden über ihre ID oder ihren Namen abgeglichen und aktualisiert. Neue Kategorien werden angelegt.',
        example: JSON.stringify([{ name: 'Werkzeuge' }], null, 2),
    },
};

function download(blob: Blob, filename: string) {
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a'); link.href = url; link.download = filename;
    document.body.appendChild(link); link.click(); link.remove();
    window.setTimeout(() => URL.revokeObjectURL(url), 1000);
}

export default function InventoryTransferTools({ kind, canImport, disabled = false, onImported, onBusyChange }: {
    kind: InventoryTransferKind; canImport: boolean; disabled?: boolean;
    onImported: () => Promise<void>; onBusyChange: (busy: boolean) => void;
}) {
    const format = formats[kind];
    const input = useRef<HTMLInputElement>(null);
    const [file, setFile] = useState<File | null>(null);
    const [working, setWorking] = useState(false);
    const [errors, setErrors] = useState<InventoryImportError[]>([]);
    const [notice, setNotice] = useState('');
    const busy = disabled || working;

    const exportFile = async () => {
        if (busy) return;
        setWorking(true); onBusyChange(true); setErrors([]); setNotice('');
        try { download(await exportInventoryData(kind), format.filename); }
        catch (error) { setErrors([{ lineNumber: null, description: getApiErrorMessage(error, 'Export konnte nicht erstellt werden.') }]); }
        finally { setWorking(false); onBusyChange(false); }
    };
    const importFile = async () => {
        if (busy || !canImport) return;
        setErrors([]); setNotice('');
        if (!file || file.size === 0) { setErrors([{ lineNumber: null, description: 'Bitte eine nicht leere Importdatei auswählen.' }]); return; }
        if (file.size > 5 * 1024 * 1024) { setErrors([{ lineNumber: null, description: 'Die Importdatei darf höchstens 5 MB groß sein.' }]); return; }
        setWorking(true); onBusyChange(true);
        try {
            const result = await importInventoryData(kind, file);
            setNotice(`Import abgeschlossen: ${result.createdCount} neu angelegt, ${result.updatedCount} aktualisiert.`);
            setFile(null); if (input.current) input.current.value = '';
            try { await onImported(); }
            catch { setErrors([{ lineNumber: null, description: 'Die Daten wurden importiert, aber die Ansicht konnte nicht aktualisiert werden. Bitte die Seite neu laden.' }]); }
        } catch (error) {
            const details = isAxiosError<InventoryImportResponse>(error) ? error.response?.data?.errors : undefined;
            setErrors(details?.length ? details : [{ lineNumber: null, description: getApiErrorMessage(error, 'Die Datei konnte nicht importiert werden.') }]);
        } finally { setWorking(false); onBusyChange(false); }
    };

    return <section className="card inventory-transfer" aria-label={`${format.label} Import und Export`}>
        <div className="inventory-actions"><button type="button" disabled={busy} onClick={() => void exportFile()}>{format.label} als {format.extension.toUpperCase()} exportieren</button></div>
        {canImport && <details><summary>{format.label} importieren</summary>
            <p>{format.description}</p><p>Bei fehlerhaften Einträgen wird die gesamte Datei abgelehnt. Höchstens 5 MB. Für einen vollständigen Datenumzug zuerst Kategorien, dann Gerätetypen und zuletzt das Inventar importieren.</p>
            {kind === 'equipment' && <p>Barcodes: KPP-001 bis KPP-999. Neue Geräte übernehmen Status und Datumsangaben aus der Datei, sofern angegeben; andernfalls gelten Einsatzklar, aktiv und das aktuelle Anlagedatum. Datumsformat: JJJJ-MM-TT. ID und nächster Prüftermin sind berechnete bzw. geschützte Exportangaben.</p>}
            <details><summary>Format und Beispieldatei</summary><pre className="inventory-example"><code>{format.example.trim()}</code></pre>
                <button type="button" disabled={busy} onClick={() => download(new Blob([format.example], { type: format.mime }), `beispiel-${format.filename}`)}>Beispieldatei herunterladen</button>
            </details>
            <div className="inventory-filters"><label>Importdatei<input ref={input} type="file" accept={`.${format.extension}`} disabled={busy} onChange={event => { setFile(event.target.files?.[0] ?? null); setErrors([]); setNotice(''); }} /></label>
                <button type="button" disabled={busy || !file} onClick={() => void importFile()}>{working ? 'Wird verarbeitet …' : 'Datei importieren'}</button>
            </div>
        </details>}
        {notice && <p role="status">{notice}</p>}
        {errors.length > 0 && <div role="alert"><ul>{errors.map((error, index) => <li key={index}>{error.lineNumber !== null && `${kind === 'equipment' ? 'Zeile' : 'Eintrag'} ${error.lineNumber}: `}{error.description}</li>)}</ul></div>}
    </section>;
}
