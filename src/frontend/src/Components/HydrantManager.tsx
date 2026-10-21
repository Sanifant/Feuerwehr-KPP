import { useState, useEffect, useRef } from 'react';
import type { ChangeEvent, FormEvent } from 'react';
import apiClient from '../api/apiClient';
import HydrantLocationMap from './HydrantLocationMap';
import HydrantImportModal from './HydrantImportModal';
import { useRole } from '../hooks/useRole';
import './HydrantManager.css';

// --- TypeScript Werte (Exakt wie in C#) ---
export const HydrantStatus = {
    Operational: 1,
    Defective: 2,
    Blocked: 3,
    UnderMaintenance: 4
} as const;

export type HydrantStatus = typeof HydrantStatus[keyof typeof HydrantStatus];

export const HydrantType = {
    Underground: 1,
    AbovegroundWithoutJack: 2,
    AbovegroundWithJack: 3,
    WallHydrant: 4
} as const;

export type HydrantType = typeof HydrantType[keyof typeof HydrantType];

export const WaterSource = {
    WaterGrid: 1,
    FirePond: 2,
    FireWell: 3,
    Cistern: 4
} as const;

export type WaterSource = typeof WaterSource[keyof typeof WaterSource];

// --- TypeScript Interfaces ---
export interface Address {
    street: string;
    houseNumber: string;
    postalCode: string;
    city: string;
    additionalInfo?: string | null;
}

export interface Hydrant {
    id: number;
    address: Address;
    latitude: number;
    longitude: number;
    nominalDiameter: number;
    type: HydrantType;
    waterSource: WaterSource;
    status: HydrantStatus;
    notes?: string | null;
}

// --- Labels für die UI-Anzeige ---
const HydrantTypeLabels: Record<HydrantType, string> = {
    [HydrantType.Underground]: 'Unterflurhydrant',
    [HydrantType.AbovegroundWithoutJack]: 'Überflurhydrant ohne Fallmantel',
    [HydrantType.AbovegroundWithJack]: 'Überflurhydrant mit Fallmantel',
    [HydrantType.WallHydrant]: 'Wandhydrant'
};

const WaterSourceLabels: Record<WaterSource, string> = {
    [WaterSource.WaterGrid]: 'Trinkwassernetz',
    [WaterSource.FirePond]: 'Löschwasserteich',
    [WaterSource.FireWell]: 'Löschwasserbrunnen',
    [WaterSource.Cistern]: 'Zisterne'
};

const HydrantStatusLabels: Record<HydrantStatus, string> = {
    [HydrantStatus.Operational]: 'Betriebsbereit',
    [HydrantStatus.Defective]: 'Defekt',
    [HydrantStatus.Blocked]: 'Gesperrt',
    [HydrantStatus.UnderMaintenance]: 'In Wartung'
};

type HydrantForm = Omit<Hydrant, 'latitude' | 'longitude'> & {
    latitude: number | '';
    longitude: number | '';
};

const INITIAL_FORM_STATE: HydrantForm = {
    id: 0,
    address: { street: '', houseNumber: '', postalCode: '', city: '', additionalInfo: '' },
    latitude: '',
    longitude: '',
    nominalDiameter: 80,
    type: HydrantType.Underground,
    waterSource: WaterSource.WaterGrid,
    status: HydrantStatus.Operational,
    notes: ''
};

export default function HydrantManager() {
    const { isAdmin } = useRole();
    const [hydrants, setHydrants] = useState<Hydrant[]>([]);
    const [formData, setFormData] = useState<HydrantForm>(INITIAL_FORM_STATE);
    const [isEditing, setIsEditing] = useState<boolean>(false);
    const [loading, setLoading] = useState<boolean>(false);
    const [isFormOpen, setIsFormOpen] = useState(false);
    const [isSaving, setIsSaving] = useState(false);
    const dialogRef = useRef<HTMLDialogElement>(null);
    const [isImportOpen, setIsImportOpen] = useState(false);
    const [importNotice, setImportNotice] = useState<string | null>(null);

    useEffect(() => {
        const dialog = dialogRef.current;
        if (!isFormOpen || !dialog) return;

        const previousFocus = document.activeElement;
        const previousOverflow = document.body.style.overflow;
        dialog.showModal();
        document.body.style.overflow = 'hidden';
        return () => {
            dialog.close();
            document.body.style.overflow = previousOverflow;
            if (previousFocus instanceof HTMLElement) previousFocus.focus();
        };
    }, [isFormOpen]);

    // 1. Daten vom API-Controller laden (GET)
    const fetchHydrants = async () => {
        setLoading(true);
        try {
            const response = await apiClient.get<Hydrant[]>('/api/hydrant');
            setHydrants(response.data);
        } catch (error) {
            console.error("Fehler beim Laden der Hydranten:", error);
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        fetchHydrants()
            .catch(error => {
                console.error("Fehler beim Laden der Hydranten:", error);
            });
    }, []);

    // Handler für Änderungen in den Inputs/Selects
    const handleInputChange = (e: ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>) => {
        const { name, value } = e.target;

        if (name.startsWith('address.')) {
            const field = name.split('.')[1] as keyof Address;
            setFormData(prev => ({
                ...prev,
                address: { ...prev.address, [field]: value }
            }));
        } else {
            // Numerische Werte konvertieren, damit sie zu den Typen passen
            const parsedValue = (name === 'latitude' || name === 'longitude') && value === ''
                ? ''
                : ['latitude', 'longitude', 'nominalDiameter', 'type', 'waterSource', 'status'].includes(name)
                ? Number(value)
                : value;

            setFormData(prev => ({ ...prev, [name]: parsedValue }));
        }
    };

    // 2. Absenden: Hinzufügen (POST) oder Bearbeiten (PUT)
    const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
        e.preventDefault();
        const { latitude, longitude } = formData;
        if (isSaving || latitude === '' || longitude === '' || !Number.isFinite(latitude) || !Number.isFinite(longitude)
            || latitude < -90 || latitude > 90 || longitude < -180 || longitude > 180) return;

        const isNew = formData.id === 0;
        const payload: Hydrant = { ...formData, latitude, longitude };
        setIsSaving(true);

        try {
            if (isNew) {
                await apiClient.post('/api/hydrant', payload);
            } else {
                await apiClient.put(`/api/hydrant/${formData.id}`, payload);
            }

            fetchHydrants()
            .catch(error => {
                console.error("Fehler beim Laden der Hydranten:", error);
            });
            resetForm();
        } catch (error) {
            console.error("Fehler beim Speichern:", error);
            alert("Fehler beim Speichern des Hydranten.");
        } finally {
            setIsSaving(false);
        }
    };

    // 3. Löschen (DELETE)
    const handleDelete = async (id: number) => {
        if (!window.confirm("Möchtest du diesen Hydranten wirklich löschen?")) return;

        try {
            await apiClient.delete(`/api/hydrant/${id}`);
            fetchHydrants()
            .catch(error => {
                console.error("Fehler beim Laden der Hydranten:", error);
            });
        } catch (error) {
            console.error("Fehler beim Löschen:", error);
        }
    };

    const handleEditClick = (hydrant: Hydrant) => {
        setFormData(hydrant);
        setIsEditing(true);
        setIsFormOpen(true);
    };

    const resetForm = () => {
        setFormData(INITIAL_FORM_STATE);
        setIsEditing(false);
        setIsFormOpen(false);
    };

    const openCreateForm = () => {
        setFormData(INITIAL_FORM_STATE);
        setIsEditing(false);
        setIsFormOpen(true);
    };

    return (
        <div className="hydrant-manager">
            <h1 className="text-3xl font-bold mb-6 text-red-700">🚒 Hydranten-Verwaltung</h1>

            <div className="hydrant-manager-toolbar">
                {isAdmin && <button type="button" className="hydrant-cancel-button" aria-haspopup="dialog"
                    onClick={() => { setImportNotice(null); setIsImportOpen(true); }}>CSV-Import</button>}
                <button id="Form-title" type="button" className="hydrant-add-button" onClick={openCreateForm}
                    aria-haspopup="dialog" aria-controls="hydrant-form-dialog">
                    + Neuen Hydranten erfassen
                </button>
            </div>
            {importNotice && <p role="status" className="hydrant-import-notice">{importNotice}</p>}
            {isAdmin && isImportOpen && <HydrantImportModal
                onClose={() => setIsImportOpen(false)}
                onImported={count => {
                    setIsImportOpen(false);
                    setImportNotice(`${count} Hydranten wurden importiert.`);
                    void fetchHydrants();
                }} />}

                {/* FORMULAR: Hinzufügen / Bearbeiten */}
                {isFormOpen && <dialog ref={dialogRef} id="hydrant-form-dialog" className="hydrant-modal"
                    aria-labelledby="hydrant-form-title" aria-modal="true"
                    onCancel={event => { event.preventDefault(); if (!isSaving) resetForm(); }}
                    onClick={event => { if (event.target === event.currentTarget && !isSaving) resetForm(); }}>
                    <div className="hydrant-modal-content">
                    <header className="hydrant-modal-header">
                    <h2 id="hydrant-form-title">
                        {isEditing ? '📝 Hydrant bearbeiten' : '➕ Neuen Hydranten erfassen'}
                    </h2>
                    <button type="button" className="hydrant-close-button" onClick={resetForm}
                        disabled={isSaving} aria-label="Schließen">×</button>
                    </header>
                    <div className="hydrant-modal-body">
                    <form onSubmit={handleSubmit} className="hydrant-form" aria-label="Hydrantendaten">
                        <fieldset disabled={isSaving}>
                        <div>
                            <label htmlFor="address.street" className="block text-xs font-bold text-gray-600 uppercase">Straße & Hausnummer</label>
                            <div id="address" className="grid grid-cols-3 gap-2 mt-1">
                                <input type="text" id="address.street" name="address.street" value={formData.address.street} onChange={handleInputChange} placeholder="Str." className="col-span-2 p-2 border rounded text-sm" required />
                                <input type="text" name="address.houseNumber" aria-label="Hausnummer" value={formData.address.houseNumber} onChange={handleInputChange} placeholder="Nr." className="p-2 border rounded text-sm" required />
                            </div>
                        </div>

                        <div className="grid grid-cols-3 gap-2">
                            <div className="col-span-1">
                                <label htmlFor="address.postalCode" className="block text-xs font-bold text-gray-600 uppercase">PLZ</label>
                                <input type="text" id="address.postalCode" name="address.postalCode" value={formData.address.postalCode} onChange={handleInputChange} className="mt-1 w-full p-2 border rounded text-sm" required />
                            </div>
                            <div className="col-span-2">
                                <label htmlFor="address.city" className="block text-xs font-bold text-gray-600 uppercase">Ort</label>
                                <input type="text" id="address.city" name="address.city" value={formData.address.city} onChange={handleInputChange} className="mt-1 w-full p-2 border rounded text-sm" required />
                            </div>
                        </div>

                        <div className="grid grid-cols-2 gap-2">
                            <div>
                                <label htmlFor="latitude" className="block text-xs font-bold text-gray-600 uppercase">Breitengrad (Lat)</label>
                                <input type="number" id="latitude" step="any" min="-90" max="90" name="latitude" value={formData.latitude} onChange={handleInputChange} className="mt-1 w-full p-2 border rounded text-sm" required />
                            </div>
                            <div>
                                <label htmlFor="longitude" className="block text-xs font-bold text-gray-600 uppercase">Längengrad (Lng)</label>
                                <input type="number" id="longitude" step="any" min="-180" max="180" name="longitude" value={formData.longitude} onChange={handleInputChange} className="mt-1 w-full p-2 border rounded text-sm" required />
                            </div>
                        </div>

                        <div className="grid grid-cols-2 gap-2">
                            <div>
                                <label htmlFor="nominalDiameter" className="block text-xs font-bold text-gray-600 uppercase">Nennweite (DN)</label>
                                <input type="number" id="nominalDiameter" name="nominalDiameter" value={formData.nominalDiameter} onChange={handleInputChange} className="mt-1 w-full p-2 border rounded text-sm" required />
                            </div>
                            <div>
                                <label htmlFor="type" className="block text-xs font-bold text-gray-600 uppercase">Typ</label>
                                <select id="type" name="type" value={formData.type} onChange={handleInputChange} className="mt-1 w-full p-2 border rounded text-sm">
                                    {Object.entries(HydrantTypeLabels).map(([key, value]) => (
                                        <option key={key} value={key}>{value}</option>
                                    ))}
                                </select>
                            </div>
                        </div>

                        <div className="grid grid-cols-2 gap-2">
                            <div>
                                <label htmlFor="waterSource" className="block text-xs font-bold text-gray-600 uppercase">Wasserquelle</label>
                                <select id="waterSource" name="waterSource" value={formData.waterSource} onChange={handleInputChange} className="mt-1 w-full p-2 border rounded text-sm">
                                    {Object.entries(WaterSourceLabels).map(([key, value]) => (
                                        <option key={key} value={key}>{value}</option>
                                    ))}
                                </select>
                            </div>
                            <div>
                                <label htmlFor="status" className="block text-xs font-bold text-gray-600 uppercase">Status</label>
                                <select id="status" name="status" value={formData.status} onChange={handleInputChange} className="mt-1 w-full p-2 border rounded text-sm">
                                    {Object.entries(HydrantStatusLabels).map(([key, value]) => (
                                        <option key={key} value={key}>{value}</option>
                                    ))}
                                </select>
                            </div>
                        </div>

                        <div>
                            <label htmlFor="notes" className="block text-xs font-bold text-gray-600 uppercase">Notizen</label>
                            <textarea id="notes" name="notes" value={formData.notes || ''} onChange={handleInputChange} className="mt-1 w-full p-2 border rounded text-sm h-16" />
                        </div>

                        </fieldset>
                        <div className="hydrant-form-actions">
                            <button type="submit" disabled={isSaving} className="hydrant-save-button">
                                {isSaving ? 'Wird gespeichert…' : isEditing ? 'Aktualisieren' : 'Speichern'}
                            </button>
                                <button type="button" disabled={isSaving} onClick={resetForm} className="hydrant-cancel-button">
                                    Abbrechen
                                </button>
                        </div>
                    </form>
                    <HydrantLocationMap latitude={formData.latitude} longitude={formData.longitude} />
                    </div>
                    </div>
                </dialog>}

                {/* LISTE: Verzeichnis */}
                <div className="hydrant-directory">
                    <h2 className="text-xl font-semibold mb-4 text-gray-800">📋 Hydranten-Verzeichnis</h2>

                    {loading ? (
                        <p className="text-gray-500">Daten werden geladen...</p>
                    ) : hydrants.length === 0 ? (
                        <p className="text-gray-500 italic">Keine Hydranten in der Datenbank vorhanden.</p>
                    ) : (
                        <div className="space-y-3 max-h-[600px] overflow-y-auto pr-2">
                            {hydrants.map((hydrant) => (
                                <div key={hydrant.id} className="card">
                                    <div className="space-y-1">
                                        <div className="flex items-center gap-2">
                                            <span className="font-bold text-gray-900">ID {hydrant.id}</span>
                                            <span className={`px-2 py-0.5 rounded text-xs font-semibold ${hydrant.status === HydrantStatus.Operational ? 'bg-green-100 text-green-800' :
                                                    hydrant.status === HydrantStatus.Defective ? 'bg-red-100 text-red-800' : 'bg-yellow-100 text-yellow-800'
                                                }`}>
                                                {HydrantStatusLabels[hydrant.status]}
                                            </span>
                                            <span className="bg-gray-200 text-gray-700 px-2 py-0.5 rounded text-xs font-mono">
                                                DN {hydrant.nominalDiameter}
                                            </span>
                                        </div>
                                        <p className="text-sm text-gray-700">
                                            📍 {hydrant.address?.street} {hydrant.address?.houseNumber}, {hydrant.address?.postalCode} {hydrant.address?.city}
                                            <br />
                                            Koordinaten: {hydrant.latitude.toFixed(5)},  {hydrant.longitude.toFixed(5)}
                                        </p>
                                        <p className="text-xs text-gray-500">
                                           Typ: {HydrantTypeLabels[hydrant.type]} | Quelle: {WaterSourceLabels[hydrant.waterSource]}
                                        </p>
                                        {hydrant.notes && (
                                            <p className="text-xs italic text-gray-600 bg-amber-50 p-1.5 rounded border border-amber-200 mt-1">
                                                📝 {hydrant.notes}
                                            </p>
                                        )}
                                    </div>

                                    <div className="flex gap-2 ml-4">
                                        <button onClick={() => handleEditClick(hydrant)} className="bg-blue-100 hover:bg-blue-200 text-blue-700 px-3 py-1 rounded text-xs font-medium transition">
                                            Edit
                                        </button>
                                        <button onClick={() => handleDelete(hydrant.id)} className="bg-red-100 hover:bg-red-200 text-red-700 px-3 py-1 rounded text-xs font-medium transition">
                                            Löschen
                                        </button>
                                    </div>
                                </div>
                            ))}
                        </div>
                    )}
                </div>

        </div>
    );
}
