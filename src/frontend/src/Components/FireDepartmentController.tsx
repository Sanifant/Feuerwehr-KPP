import { useEffect, useState } from 'react';
import type { ChangeEvent, FormEvent } from 'react';
import apiClient from '../api/apiClient';

interface FireDepartmentDto {
    id: number;
    name: string;
    contactPersonName?: string | null;
    contactPersonEmail?: string | null;
}

const INITIAL_FORM_STATE: FireDepartmentDto = {
    id: 0,
    name: '',
    contactPersonName: '',
    contactPersonEmail: ''
};

export default function FireDepartmentController() {
    const [fireDepartments, setFireDepartments] = useState<FireDepartmentDto[]>([]);
    const [selectedFireDepartment, setSelectedFireDepartment] = useState<FireDepartmentDto | null>(null);
    const [formData, setFormData] = useState<FireDepartmentDto>(INITIAL_FORM_STATE);
    const [searchId, setSearchId] = useState('');
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const fetchFireDepartments = async () => {
        setLoading(true);
        setError(null);

        try {
            const response = await apiClient.get<FireDepartmentDto[]>('/api/FireDepartment');
            setFireDepartments(response.data);
        } catch (requestError) {
            setError(requestError instanceof Error ? requestError.message : 'Unbekannter Fehler beim Laden.');
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        fetchFireDepartments();
    }, []);

    const handleInputChange = (event: ChangeEvent<HTMLInputElement>) => {
        const { name, value } = event.target;

        setFormData((current) => ({
            ...current,
            [name]: name === 'id' ? Number(value) : value
        }));
    };

    const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();
        setError(null);

        try {
            await apiClient.post('/api/FireDepartment', formData);

            setFireDepartments((current) => [
                ...current.filter((fireDepartment) => fireDepartment.id !== formData.id),
                formData
            ]);
            setFormData(INITIAL_FORM_STATE);
        } catch (requestError) {
            setError(requestError instanceof Error ? requestError.message : 'Unbekannter Fehler beim Speichern.');
        }
    };

    const handleFindById = async () => {
        const id = Number(searchId);

        if (!id) {
            setSelectedFireDepartment(null);
            return;
        }

        setLoading(true);
        setError(null);

        try {
            const response = await apiClient.get<FireDepartmentDto>(`/api/FireDepartment/${id}`);
            setSelectedFireDepartment(response.data);
        } catch (requestError) {
            setSelectedFireDepartment(null);
            setError(requestError instanceof Error ? requestError.message : 'Unbekannter Fehler beim Suchen.');
        } finally {
            setLoading(false);
        }
    };

    return (
        <>
            <header>
                <h1>Feuerwehren</h1>
                <p>Feuerwehren laden, suchen und neu erfassen.</p>
            </header>

            <section className="fire-department-grid">
                <div className="card">
                    <div className="card-header">
                        <h2>Neue Feuerwehr</h2>
                    </div>

                    <form className="fire-department-form" onSubmit={handleSubmit}>
                        <label>
                            Name
                            <input type="text" name="name" value={formData.name} onChange={handleInputChange} required />
                        </label>
                        <label>
                            Kontaktperson
                            <input
                                type="text"
                                name="contactPersonName"
                                value={formData.contactPersonName || ''}
                                onChange={handleInputChange}
                            />
                        </label>
                        <label>
                            Kontakt E-Mail
                            <input
                                type="email"
                                name="contactPersonEmail"
                                value={formData.contactPersonEmail || ''}
                                onChange={handleInputChange}
                            />
                        </label>

                        <button type="submit">Feuerwehr speichern</button>
                    </form>
                </div>

                <div className="card">
                    <div className="card-header">
                        <h2>Feuerwehr suchen</h2>
                    </div>

                    <div className="fire-department-search">
                        <input
                            type="number"
                            value={searchId}
                            onChange={(event) => setSearchId(event.target.value)}
                            placeholder="ID eingeben"
                        />
                        <button type="button" onClick={handleFindById}>Suchen</button>
                    </div>

                    {selectedFireDepartment && (
                        <FireDepartmentDetails fireDepartment={selectedFireDepartment} />
                    )}
                </div>
            </section>

            <section className="card fire-department-list-card">
                <div className="card-header">
                    <h2>Alle Feuerwehren</h2>
                    <button type="button" onClick={fetchFireDepartments}>Aktualisieren</button>
                </div>

                {error && <p className="fire-department-error">{error}</p>}
                {loading && <p className="fire-department-muted">Daten werden geladen...</p>}

                {!loading && fireDepartments.length === 0 ? (
                    <p className="fire-department-muted">Keine Feuerwehren vorhanden.</p>
                ) : (
                    <div className="fire-department-list">
                        {fireDepartments.map((fireDepartment) => (
                            <FireDepartmentDetails key={fireDepartment.id} fireDepartment={fireDepartment} />
                        ))}
                    </div>
                )}
            </section>
        </>
    );
}

function FireDepartmentDetails({ fireDepartment }: { fireDepartment: FireDepartmentDto }) {
    return (
        <article className="fire-department-item">
            <div>
                <strong>{fireDepartment.name}</strong>
                <span>ID {fireDepartment.id}</span>
            </div>
            <dl>
                <div>
                    <dt>Kontakt</dt>
                    <dd>{fireDepartment.contactPersonName || '-'}</dd>
                </div>
                <div>
                    <dt>E-Mail</dt>
                    <dd>{fireDepartment.contactPersonEmail || '-'}</dd>
                </div>
            </dl>
        </article>
    );
}
