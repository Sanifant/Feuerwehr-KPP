import { type FormEvent, useEffect, useMemo, useState } from "react";
import { useSearchParams } from "react-router-dom";
import apiClient from "../api/apiClient";
import "./Dashboard.css";

interface TrainingCourseDto {
    id: number;
    title: string;
    description: string;
    level: number;
    status: number;
    assignedFireDepartmentId: number;
    assignedFireDepartmentName: string;
    assignedSeats: number;
}

interface FireDepartmentDto {
    id: number;
    name: string;
}

interface NewTrainingFormState {
    title: string;
    description: string;
    level: number;
    status: number;
    assignedFireDepartmentId: number;
    assignedSeats: number;
}

interface LevelSummary {
    title: string;
    courses: string;
    participants: string;
}

const levelDisplayMap: Record<number, { fullLabel: string; badge: string; badgeClass: string }> = {
    1: { fullLabel: "Gemeinde", badge: "Gemeinde", badgeClass: "municipality" },
    2: { fullLabel: "Kreis", badge: "Kreis", badgeClass: "district" },
    3: { fullLabel: "Land", badge: "Land", badgeClass: "state" }
};

const statusDisplayMap: Record<number, { text: string; className: string }> = {
    1: { text: "Geplant", className: "pending" },
    2: { text: "Offen", className: "pending" },
    3: { text: "Ausgebucht", className: "rejected" },
    4: { text: "Abgeschlossen", className: "approved" },
    5: { text: "Abgesagt", className: "rejected" }
};

const INITIAL_NEW_TRAINING_FORM: NewTrainingFormState = {
    title: "",
    description: "",
    level: 1,
    status: 1,
    assignedFireDepartmentId: 0,
    assignedSeats: 1
};

export function TrainingDashboard() {
    const [searchParams, setSearchParams] = useSearchParams();
    const [trainingCourses, setTrainingCourses] = useState<TrainingCourseDto[]>([]);
    const [fireDepartments, setFireDepartments] = useState<FireDepartmentDto[]>([]);
    const [newTrainingForm, setNewTrainingForm] = useState<NewTrainingFormState>(INITIAL_NEW_TRAINING_FORM);
    const [titleSearchResults, setTitleSearchResults] = useState<TrainingCourseDto[]>([]);
    const [titleSearchLoading, setTitleSearchLoading] = useState(false);
    const [loading, setLoading] = useState(false);
    const [submitting, setSubmitting] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const selectedLevel = (searchParams.get("level") || "").toLowerCase();

    const fetchTrainingCourses = async (level: string) => {
        setLoading(true);
        setError(null);

        try {
            const params = level ? { level } : {};
            const response = await apiClient.get<TrainingCourseDto[]>('/api/TrainingRecord', { params });
            setTrainingCourses(response.data);
        } catch (requestError) {
            setError(requestError instanceof Error ? requestError.message : "Unbekannter Fehler beim Laden.");
        } finally {
            setLoading(false);
        }
    };

    const fetchFireDepartments = async () => {
        try {
            const response = await apiClient.get<FireDepartmentDto[]>('/api/FireDepartment');
            const data = response.data;
            setFireDepartments(data);

            setNewTrainingForm((current) => ({
                ...current,
                assignedFireDepartmentId: current.assignedFireDepartmentId || data[0]?.id || 0
            }));
        } catch {
            setFireDepartments([]);
        }
    };

    useEffect(() => {
        fetchTrainingCourses(selectedLevel);
    }, [selectedLevel]);

    useEffect(() => {
        fetchFireDepartments();
    }, []);

    useEffect(() => {
        const trimmedTitle = newTrainingForm.title.trim();

        if (trimmedTitle.length < 3) {
            setTitleSearchResults([]);
            setTitleSearchLoading(false);
            return;
        }

        let isCancelled = false;
        const timeoutId = setTimeout(async () => {
            setTitleSearchLoading(true);

            try {
                const response = await apiClient.get<TrainingCourseDto[]>('/api/TrainingRecord', {
                    params: { title: trimmedTitle }
                });

                if (!isCancelled) {
                    setTitleSearchResults(response.data);
                }
            } catch {
                if (!isCancelled) {
                    setTitleSearchResults([]);
                }
            } finally {
                if (!isCancelled) {
                    setTitleSearchLoading(false);
                }
            }
        }, 300);

        return () => {
            isCancelled = true;
            clearTimeout(timeoutId);
        };
    }, [newTrainingForm.title]);

    const levelSummaries = useMemo<LevelSummary[]>(() => {
        const counts = trainingCourses.reduce<Record<number, number>>((result, course) => {
            result[course.level] = (result[course.level] || 0) + 1;
            return result;
        }, {});

        return [
            { title: "Gemeindeebene", courses: String(counts[1] || 0), participants: "-" },
            { title: "Kreisebene", courses: String(counts[2] || 0), participants: "-" },
            { title: "Landesebene", courses: String(counts[3] || 0), participants: "-" }
        ];
    }, [trainingCourses]);

    const handleLevelChange = (value: string) => {
        if (!value) {
            setSearchParams({});
            return;
        }

        setSearchParams({ level: value });
    };

    const trimmedNewTrainingTitle = newTrainingForm.title.trim();
    const shouldSearchByTitle = trimmedNewTrainingTitle.length >= 3;
    const requireDescriptionForNewTitle = shouldSearchByTitle && !titleSearchLoading && titleSearchResults.length === 0;

    const handleSelectSuggestedTraining = (trainingId: number) => {
        const selectedTraining = titleSearchResults.find((training) => training.id === trainingId);

        if (!selectedTraining) {
            return;
        }

        setNewTrainingForm((current) => ({
            ...current,
            title: selectedTraining.title,
            description: selectedTraining.description,
            level: selectedTraining.level,
            status: selectedTraining.status
        }));
    };

    const handleCreateTraining = async (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();

        const trimmedTitle = newTrainingForm.title.trim();
        const trimmedDescription = newTrainingForm.description.trim();

        if (!newTrainingForm.assignedFireDepartmentId) {
            setError("Bitte eine Feuerwehr auswählen.");
            return;
        }

        if (trimmedTitle.length < 3) {
            setError("Bitte mindestens drei Zeichen für den Titel eingeben.");
            return;
        }

        if (requireDescriptionForNewTitle && !trimmedDescription) {
            setError("Für einen neuen Titel ist eine Beschreibung erforderlich.");
            return;
        }

        setSubmitting(true);
        setError(null);

        const selectedFireDepartment = fireDepartments.find((fireDepartment) => fireDepartment.id === newTrainingForm.assignedFireDepartmentId);

        try {
            await apiClient.post('/api/TrainingRecord', {
                id: 0,
                title: trimmedTitle,
                description: trimmedDescription,
                level: newTrainingForm.level,
                status: newTrainingForm.status,
                assignedFireDepartmentId: newTrainingForm.assignedFireDepartmentId,
                assignedFireDepartmentName: selectedFireDepartment?.name || "",
                assignedSeats: newTrainingForm.assignedSeats
            });

            setNewTrainingForm({
                ...INITIAL_NEW_TRAINING_FORM,
                assignedFireDepartmentId: fireDepartments[0]?.id || 0
            });
            setTitleSearchResults([]);

            await fetchTrainingCourses(selectedLevel);
        } catch (requestError) {
            setError(requestError instanceof Error ? requestError.message : "Unbekannter Fehler beim Erstellen.");
        } finally {
            setSubmitting(false);
        }
    };

    return (
        <>
            <header>
                <h1>Dashboard</h1>
                <p>Lehrgänge aus dem Trainings-Controller.</p>
            </header>

            <section className="stats-grid">
                <StatCard title="Gemeindeebene" value={levelSummaries[0].courses} subtitle="Lehrgänge" />
                <StatCard title="Kreisebene" value={levelSummaries[1].courses} subtitle="Lehrgänge"/>
                <StatCard title="Landesebene" value={levelSummaries[2].courses} subtitle="Lehrgänge" />
            </section>

            <section className="content-grid">

                <div className="card">
                    <CardHeader title="Lehrgänge" />
                    <div className="fire-department-search">
                        <label>
                            Ebene filtern
                            <select value={selectedLevel} onChange={(event) => handleLevelChange(event.target.value)}>
                                <option value="">Alle</option>
                                <option value="gemeinde">Gemeinde</option>
                                <option value="kreis">Kreis</option>
                                <option value="land">Land</option>
                            </select>
                        </label>
                    </div>
                    <table>
                        <thead>
                        <tr>
                            <th>ID</th>
                            <th>Lehrgang</th>
                            <th>Ebene</th>
                            <th>Status</th>
                            <th>Feuerwehr</th>
                            <th>Plätze</th>
                        </tr>
                        </thead>
                        <tbody>
                        {trainingCourses.map((course) => {
                            const levelDisplay = levelDisplayMap[course.level] || { fullLabel: "Unbekannt", badge: "Unbekannt", badgeClass: "state" };
                            const statusDisplay = statusDisplayMap[course.status] || { text: "Unbekannt", className: "pending" };
                            const fireDepartmentName = course.assignedFireDepartmentName || `ID ${course.assignedFireDepartmentId}`;

                            return (
                                <tr key={course.id}>
                                    <td>{course.id}</td>
                                    <td>{course.title}</td>
                                    <td><span className={`badge ${levelDisplay.badgeClass}`}>{levelDisplay.badge}</span></td>
                                    <td><span className={`status ${statusDisplay.className}`}>{statusDisplay.text}</span></td>
                                    <td>{fireDepartmentName}</td>
                                    <td>{course.assignedSeats}</td>
                                </tr>
                            );
                        })}
                        </tbody>
                    </table>

                    {error && <p className="fire-department-error">{error}</p>}
                    {loading && <p className="fire-department-muted">Daten werden geladen...</p>}
                    {!loading && trainingCourses.length === 0 && !error && (
                        <p className="fire-department-muted">Keine Lehrgänge vorhanden.</p>
                    )}

                    <button className="link-button" onClick={() => fetchTrainingCourses(selectedLevel)}>Aktualisieren</button>
                </div>

                <div className="card">
                    <CardHeader title="Neuen Lehrgang erstellen" />
                    <form className="fire-department-form" onSubmit={handleCreateTraining}>
                        <label>
                            Titel
                            <input
                                type="text"
                                value={newTrainingForm.title}
                                onChange={(event) => setNewTrainingForm((current) => ({ ...current, title: event.target.value }))}
                                required
                            />
                        </label>

                        {shouldSearchByTitle && (
                            <>
                                {titleSearchLoading && <p className="fire-department-muted">Trainings werden gesucht...</p>}

                                {!titleSearchLoading && titleSearchResults.length > 0 && (
                                    <label>
                                        Schnellauswahl
                                        <select defaultValue="" onChange={(event) => handleSelectSuggestedTraining(Number(event.target.value))}>
                                            <option value="">Gefundenes Training auswählen</option>
                                            {titleSearchResults.map((training) => (
                                                <option key={training.id} value={training.id}>{training.title}</option>
                                            ))}
                                        </select>
                                    </label>
                                )}

                                {!titleSearchLoading && titleSearchResults.length === 0 && (
                                    <p className="fire-department-muted">Kein Training gefunden. Bitte Beschreibung für neues Training angeben.</p>
                                )}
                            </>
                        )}

                        <label>
                            Beschreibung
                            <textarea
                                value={newTrainingForm.description}
                                onChange={(event) => setNewTrainingForm((current) => ({ ...current, description: event.target.value }))}
                                required={requireDescriptionForNewTitle}
                            />
                        </label>

                        <label>
                            Ebene
                            <select
                                value={newTrainingForm.level}
                                onChange={(event) => setNewTrainingForm((current) => ({ ...current, level: Number(event.target.value) }))}
                            >
                                <option value={1}>Gemeinde</option>
                                <option value={2}>Kreis</option>
                                <option value={3}>Land</option>
                            </select>
                        </label>
                        <label>
                            Status
                            <select
                                value={newTrainingForm.status}
                                onChange={(event) => setNewTrainingForm((current) => ({ ...current, status: Number(event.target.value) }))}
                            >
                                <option value={1}>Geplant</option>
                                <option value={2}>Offen</option>
                                <option value={3}>Ausgebucht</option>
                                <option value={4}>Abgeschlossen</option>
                                <option value={5}>Abgesagt</option>
                            </select>
                        </label>
                        <label>
                            Feuerwehr
                            <select
                                value={newTrainingForm.assignedFireDepartmentId}
                                onChange={(event) => setNewTrainingForm((current) => ({ ...current, assignedFireDepartmentId: Number(event.target.value) }))}
                                required
                            >
                                {fireDepartments.map((fireDepartment) => (
                                    <option key={fireDepartment.id} value={fireDepartment.id}>{fireDepartment.name}</option>
                                ))}
                            </select>
                        </label>
                        <label>
                            Plätze
                            <input
                                type="number"
                                min={1}
                                value={newTrainingForm.assignedSeats}
                                onChange={(event) => setNewTrainingForm((current) => ({ ...current, assignedSeats: Number(event.target.value) }))}
                                required
                            />
                        </label>

                        <button type="submit" disabled={submitting || fireDepartments.length === 0}>
                            {submitting ? "Wird gespeichert..." : "Lehrgang erstellen"}
                        </button>
                    </form>
                </div>

                <div className="card">
                    <CardHeader title="Statusübersicht" />
                    <div className="request-list">
                        {trainingCourses.map((course) => {
                            const statusDisplay = statusDisplayMap[course.status] || { text: "Unbekannt", className: "pending" };
                            const fireDepartmentName = course.assignedFireDepartmentName || `ID ${course.assignedFireDepartmentId}`;

                            return (
                                <div className="request-item" key={`status-${course.id}`}>
                                    <div>
                                        <strong>{course.title}</strong>
                                        <span>{fireDepartmentName}</span>
                                    </div>
                                    <small>{levelDisplayMap[course.level]?.fullLabel || "Unbekannt"}</small>
                                    <span className={`status ${statusDisplay.className}`}>{statusDisplay.text}</span>
                                </div>
                            );
                        })}
                    </div>
                </div>
            </section>
        </>
    );
}

function StatCard(props: { title: string; value: string; subtitle: string}) {
    return (
        <div className="stat-card">
            <div className="icon"></div>
            <div>
                <h3>{props.title}</h3>
                <strong>{props.value}</strong>
                <p>{props.subtitle}</p>
            </div>
        </div>
    );
}

function CardHeader({ title }: { title: string }) {
    return (
        <div className="card-header">
            <h2>{title}</h2>
        </div>
    );
}


