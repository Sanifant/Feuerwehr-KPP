import { useRef, useState } from 'react';
import { isAxiosError } from 'axios';
import { getApiErrorMessage } from '../api/errors';
import { exportVehicleData, importVehicleData } from '../api/vehicleTransferApi';
import type { VehicleImportError, VehicleImportResponse, VehicleTransferKind } from '../api/vehicleTransferApi';

const formats = {
    vehicles: { label: 'Fahrzeuge', extension: 'csv', filename: 'fahrzeuge.csv', mime: 'text/csv;charset=utf-8',
        example: '\uFEFFradioCallSign;name;licensePlate;fireDepartment;vehicleType;status;equipmentOfficerEmail;isActive\r\nFlorian 19-01;Mannschaftstransporter;OH-FF 19;Feuerwehr A;MTW;Betriebsbereit;geraetewart@example.org;true\r\n' },
    types: { label: 'Fahrzeugtypen', extension: 'json', filename: 'fahrzeugtypen.json', mime: 'application/json',
        example: JSON.stringify([{ name: 'MTW', isActive: true }], null, 2) },
};
function download(blob: Blob, filename: string) {
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a'); link.href = url; link.download = filename;
    document.body.appendChild(link); link.click(); link.remove();
    window.setTimeout(() => URL.revokeObjectURL(url), 1000);
}
export default function VehicleTransferTools({ kind, disabled, onImported, onBusyChange }: {
    kind: VehicleTransferKind; disabled: boolean; onImported: () => Promise<void>; onBusyChange: (busy: boolean) => void;
}) {
    const format = formats[kind];
    const input = useRef<HTMLInputElement>(null);
    const running = useRef(false);
    const [file, setFile] = useState<File | null>(null);
    const [working, setWorking] = useState(false);
    const [errors, setErrors] = useState<VehicleImportError[]>([]);
    const [notice, setNotice] = useState('');
    const busy = disabled || working;
    const run = async (action: () => Promise<void>) => {
        if (running.current || disabled) return;
        running.current = true; setWorking(true); onBusyChange(true); setErrors([]); setNotice('');
        try { await action(); }
        catch (err) {
            const details = isAxiosError<VehicleImportResponse>(err) ? err.response?.data?.errors : undefined;
            setErrors(details?.length ? details : [{ lineNumber: null, description: getApiErrorMessage(err, 'Die Datei konnte nicht verarbeitet werden.') }]);
        } finally { running.current = false; setWorking(false); onBusyChange(false); }
    };
    const importFile = async () => {
        if (!file || file.size === 0 || file.size > 5 * 1024 * 1024) {
            setErrors([{ lineNumber: null, description: 'Bitte eine nicht leere Importdatei mit höchstens 5 MB auswählen.' }]); return;
        }
        await run(async () => {
            const result = await importVehicleData(kind, file);
            setNotice(`Import abgeschlossen: ${result.createdCount} neu angelegt, ${result.updatedCount} aktualisiert.`);
            setFile(null); if (input.current) input.current.value = '';
            try { await onImported(); }
            catch { setErrors([{ lineNumber: null, description: 'Die Daten wurden importiert. Bitte die Ansicht neu laden.' }]); }
        });
    };
    return <section className="card" aria-label={`${format.label} Import und Export`}>
        <button disabled={busy} onClick={() => void run(async () => download(await exportVehicleData(kind), format.filename))}>
            {format.label} als {format.extension.toUpperCase()} exportieren</button>
        <details><summary>{format.label} importieren</summary>
            {kind === 'vehicles' ? <p>UTF-8-CSV mit Semikolon. Der Gerätewart wird über equipmentOfficerEmail ermittelt und muss aktiv sein, die Rolle Gerätewart besitzen und der angegebenen Feuerwehr angehören.
                Feuerwehren werden über ihren Namen gefunden; bei gleichen Namen dient fireDepartmentId zur Unterscheidung. Typen und Status müssen bereits existieren.
                Bestehende Fahrzeuge werden über ihre ID oder Feuerwehr und Funkrufname aktualisiert; Buchungen und Historie bleiben erhalten.
                Der Export enthält auch deaktivierte Fahrzeuge. Gerätewarte exportieren nur ihre zugewiesenen Fahrzeuge und können nur Fahrzeuge ihrer Feuerwehr anlegen beziehungsweise ihre zugewiesenen Fahrzeuge aktualisieren.</p>
                : <p>JSON-Liste. Fahrzeugtypen werden über ihre ID oder ihren Namen abgeglichen. Namen und Aktivstatus werden aktualisiert; bestehende Fahrzeugzuordnungen bleiben erhalten.</p>}
            <p>Bei einem Fehler wird die gesamte Datei abgelehnt. Höchstens 5 MB und 10.000 Einträge. Zuerst Fahrzeugtypen importieren und benötigte Status anlegen, danach Fahrzeuge importieren.</p>
            <details><summary>Format und Beispieldatei</summary><pre className="vehicle-transfer-example"><code>{format.example.trim()}</code></pre>
                <button disabled={busy} onClick={() => download(new Blob([format.example], { type: format.mime }), `beispiel-${format.filename}`)}>Beispieldatei herunterladen</button></details>
            <div className="vehicle-management-toolbar"><label>Importdatei<input ref={input} type="file" accept={`.${format.extension}`} disabled={busy}
                onChange={event => { setFile(event.target.files?.[0] ?? null); setErrors([]); setNotice(''); }} /></label>
                <button disabled={busy || !file} onClick={() => void importFile()}>{working ? 'Wird verarbeitet …' : 'Datei importieren'}</button></div>
        </details>
        {notice && <p role="status">{notice}</p>}{errors.length > 0 && <div role="alert"><ul>{errors.map((error, index) => <li key={index}>
            {error.lineNumber !== null && `${kind === 'vehicles' ? 'Zeile' : 'Eintrag'} ${error.lineNumber}: `}{error.description}</li>)}</ul></div>}
    </section>;
}
