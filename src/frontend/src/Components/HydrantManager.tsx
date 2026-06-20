import React, { useState, useEffect, ChangeEvent, FormEvent } from 'react';

const API_BASE_URL = 'https://server-feuerwehr.dev.localhost:7538/api/hydrant'; // Passe den Port deines Setups an

// --- TypeScript Enums (Exakt wie in C#) ---
export enum HydrantStatus {
    Operational = 1,
    Defective = 2,
    Blocked = 3,
    UnderMaintenance = 4
}

export enum HydrantType {
    Underground = 1,
    AbovegroundWithoutJack = 2,
    AbovegroundWithJack = 3,
    WallHydrant = 4
}

export enum WaterSource {
    WaterGrid = 1,
    FirePond = 2,
    FireWell = 3,
    Cistern = 4
}

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

const INITIAL_FORM_STATE: Hydrant = {
    id: 0,
    address: { street: '', houseNumber: '', postalCode: '', city: '', additionalInfo: '' },
    latitude: 0.0,
    longitude: 0.0,
    nominalDiameter: 80,
    type: HydrantType.Underground,
    waterSource: WaterSource.WaterGrid,
    status: HydrantStatus.Operational,
    notes: ''
};

export default function HydrantManager() {
    const [hydrants, setHydrants] = useState<Hydrant[]>([]);
    const [formData, setFormData] = useState<Hydrant>(INITIAL_FORM_STATE);
    const [isEditing, setIsEditing] = useState<boolean>(false);
    const [loading, setLoading] = useState<boolean>(false);

    // 1. Daten vom API-Controller laden (GET)
    const fetchHydrants = async () => {
        setLoading(true);
        try {
            const response = await fetch(API_BASE_URL);
            if (response.ok) {
                const data: Hydrant[] = await response.json();
                setHydrants(data);
            }
        } catch (error) {
            console.error("Fehler beim Laden der Hydranten:", error);
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        fetchHydrants();
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
            const parsedValue = ['latitude', 'longitude', 'nominalDiameter', 'type', 'waterSource', 'status'].includes(name)
                ? Number(value)
                : value;

            setFormData(prev => ({ ...prev, [name]: parsedValue }));
        }
    };

    // 2. Absenden: Hinzufügen (POST) oder Bearbeiten (PUT)
    const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
        e.preventDefault();
        const isNew = formData.id === 0;
        const url = isNew ? API_BASE_URL : `${API_BASE_URL}/${formData.id}`;
        const method = isNew ? 'POST' : 'PUT';

        try {
            const response = await fetch(url, {
                method: method,
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(formData)
            });

            if (response.ok) {
                fetchHydrants();
                resetForm();
            } else {
                alert("Fehler beim Speichern des Hydranten.");
            }
        } catch (error) {
            console.error("Netzwerkfehler:", error);
        }
    };

    // 3. Löschen (DELETE)
    const handleDelete = async (id: number) => {
        if (!window.confirm("Möchtest du diesen Hydranten wirklich löschen?")) return;

        try {
            const response = await fetch(`${API_BASE_URL}/${id}`, { method: 'DELETE' });
            if (response.ok) {
                fetchHydrants();
            }
        } catch (error) {
            console.error("Fehler beim Löschen:", error);
        }
    };

    const handleEditClick = (hydrant: Hydrant) => {
        setFormData(hydrant);
        setIsEditing(true);
    };

    const resetForm = () => {
        setFormData(INITIAL_FORM_STATE);
        setIsEditing(false);
    };

    return (
        <div className="p-6 max-w-6xl mx-auto font-sans">
            <h1 className="text-3xl font-bold mb-6 text-red-700">🚒 Hydranten-Verwaltung (TSX)</h1>

            <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">

                {/* FORMULAR: Hinzufügen / Bearbeiten */}
                <div className="bg-gray-50 p-4 rounded-xl shadow-md border border-gray-200 h-fit">
                    <h2 className="text-xl font-semibold mb-4 text-gray-800">
                        {isEditing ? '📝 Hydrant bearbeiten' : '➕ Neuen Hydranten erfassen'}
                    </h2>
                    <form onSubmit={handleSubmit} className="space-y-3">
                        <div>
                            <label className="block text-xs font-bold text-gray-600 uppercase">Straße & Hausnummer</label>
                            <div className="grid grid-cols-3 gap-2 mt-1">
                                <input type="text" name="address.street" value={formData.address.street} onChange={handleInputChange} placeholder="Str." className="col-span-2 p-2 border rounded text-sm" required />
                                <input type="text" name="address.houseNumber" value={formData.address.houseNumber} onChange={handleInputChange} placeholder="Nr." className="p-2 border rounded text-sm" required />
                            </div>
                        </div>

                        <div className="grid grid-cols-3 gap-2">
                            <div className="col-span-1">
                                <label className="block text-xs font-bold text-gray-600 uppercase">PLZ</label>
                                <input type="text" name="address.postalCode" value={formData.address.postalCode} onChange={handleInputChange} className="mt-1 w-full p-2 border rounded text-sm" required />
                            </div>
                            <div className="col-span-2">
                                <label className="block text-xs font-bold text-gray-600 uppercase">Ort</label>
                                <input type="text" name="address.city" value={formData.address.city} onChange={handleInputChange} className="mt-1 w-full p-2 border rounded text-sm" required />
                            </div>
                        </div>

                        <div className="grid grid-cols-2 gap-2">
                            <div>
                                <label className="block text-xs font-bold text-gray-600 uppercase">Breitengrad (Lat)</label>
                                <input type="number" step="any" name="latitude" value={formData.latitude} onChange={handleInputChange} className="mt-1 w-full p-2 border rounded text-sm" required />
                            </div>
                            <div>
                                <label className="block text-xs font-bold text-gray-600 uppercase">Längengrad (Lng)</label>
                                <input type="number" step="any" name="longitude" value={formData.longitude} onChange={handleInputChange} className="mt-1 w-full p-2 border rounded text-sm" required />
                            </div>
                        </div>

                        <div className="grid grid-cols-2 gap-2">
                            <div>
                                <label className="block text-xs font-bold text-gray-600 uppercase">Nennweite (DN)</label>
                                <input type="number" name="nominalDiameter" value={formData.nominalDiameter} onChange={handleInputChange} className="mt-1 w-full p-2 border rounded text-sm" required />
                            </div>
                            <div>
                                <label className="block text-xs font-bold text-gray-600 uppercase">Typ</label>
                                <select name="type" value={formData.type} onChange={handleInputChange} className="mt-1 w-full p-2 border rounded text-sm">
                                    {Object.entries(HydrantTypeLabels).map(([key, value]) => (
                                        <option key={key} value={key}>{value}</option>
                                    ))}
                                </select>
                            </div>
                        </div>

                        <div className="grid grid-cols-2 gap-2">
                            <div>
                                <label className="block text-xs font-bold text-gray-600 uppercase">Wasserquelle</label>
                                <select name="waterSource" value={formData.waterSource} onChange={handleInputChange} className="mt-1 w-full p-2 border rounded text-sm">
                                    {Object.entries(WaterSourceLabels).map(([key, value]) => (
                                        <option key={key} value={key}>{value}</option>
                                    ))}
                                </select>
                            </div>
                            <div>
                                <label className="block text-xs font-bold text-gray-600 uppercase">Status</label>
                                <select name="status" value={formData.status} onChange={handleInputChange} className="mt-1 w-full p-2 border rounded text-sm">
                                    {Object.entries(HydrantStatusLabels).map(([key, value]) => (
                                        <option key={key} value={key}>{value}</option>
                                    ))}
                                </select>
                            </div>
                        </div>

                        <div>
                            <label className="block text-xs font-bold text-gray-600 uppercase">Notizen</label>
                            <textarea name="notes" value={formData.notes || ''} onChange={handleInputChange} className="mt-1 w-full p-2 border rounded text-sm h-16" />
                        </div>

                        <div className="flex gap-2 pt-2">
                            <button type="submit" className="flex-1 bg-red-600 hover:bg-red-700 text-white font-bold p-2 rounded text-sm transition">
                                {isEditing ? 'Aktualisieren' : 'Speichern'}
                            </button>
                            {isEditing && (
                                <button type="button" onClick={resetForm} className="bg-gray-400 hover:bg-gray-500 text-white font-bold p-2 rounded text-sm transition">
                                    Abbrechen
                                </button>
                            )}
                        </div>
                    </form>
                </div>

                {/* LISTE: Verzeichnis */}
                <div className="lg:col-span-2 bg-white p-4 rounded-xl shadow-md border border-gray-200">
                    <h2 className="text-xl font-semibold mb-4 text-gray-800">📋 Hydranten-Verzeichnis</h2>

                    {loading ? (
                        <p className="text-gray-500">Daten werden geladen...</p>
                    ) : hydrants.length === 0 ? (
                        <p className="text-gray-500 italic">Keine Hydranten in der Datenbank vorhanden.</p>
                    ) : (
                        <div className="space-y-3 max-h-[600px] overflow-y-auto pr-2">
                            {hydrants.map((hydrant) => (
                                <div key={hydrant.id} className="p-4 border rounded-lg hover:bg-gray-50 flex justify-between items-start transition shadow-sm">
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
                                        </p>
                                        <p className="text-xs text-gray-500">
                                            Koordinaten: {hydrant.latitude.toFixed(5)}, {hydrant.longitude.toFixed(5)} | Typ: {HydrantTypeLabels[hydrant.type]} | Quelle: {WaterSourceLabels[hydrant.waterSource]}
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
        </div>
    );
}