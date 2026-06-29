import "./Dashboard.css";

export function TrainingDashboard() {
    return (
        <>
                <header>
                    <h1>Dashboard</h1>
                    <p>Willkommen zurück, Max Mustermann!</p>
                </header>

                <section className="stats-grid">
                    <StatCard title="Gemeindeebene" value="28" subtitle="Lehrgänge geplant" />
                    <StatCard title="Kreisebene" value="15" subtitle="Lehrgänge geplant" />
                    <StatCard title="Landesebene" value="42" subtitle="Lehrgänge geplant" />
                </section>

                <section className="content-grid">
                    <div className="card">
                        <CardHeader title="Nächste Lehrgänge" />
                        <table>
                            <thead>
                            <tr>
                                <th>Lehrgang</th>
                                <th>Ebene</th>
                                <th>Datum</th>
                                <th>Ort</th>
                                <th>Freie Plätze</th>
                            </tr>
                            </thead>
                            <tbody>
                            {courses.map((course) => (
                                <tr key={course.name}>
                                    <td>{course.name}</td>
                                    <td><span className={`badge ${course.level}`}>{course.label}</span></td>
                                    <td>{course.date}</td>
                                    <td>{course.location}</td>
                                    <td>{course.slots}</td>
                                </tr>
                            ))}
                            </tbody>
                        </table>
                        <button className="link-button">Alle Lehrgänge anzeigen</button>
                    </div>

                    <div className="card">
                        <CardHeader title="Lehrgangsplatz Anträge (Landesebene)" />
                        <div className="request-list">
                            {requests.map((request) => (
                                <div className="request-item" key={request.department}>
                                    <div>
                                        <strong>{request.department}</strong>
                                        <span>{request.course}</span>
                                    </div>
                                    <small>{request.date}</small>
                                    <span className={`status ${request.statusClass}`}>{request.status}</span>
                                </div>
                            ))}
                        </div>
                        <button className="link-button">Alle Anträge anzeigen</button>
                    </div>
                </section>

                <section className="card">
                    <CardHeader title="Übersicht nach Ebene" />
                    <div className="level-grid">
                        <LevelOverview title="Gemeindeebene" courses="28" participants="156" />
                        <LevelOverview title="Kreisebene" courses="15" participants="98" />
                        <LevelOverview title="Landesebene" courses="42" participants="312" />
                    </div>
                </section>
        </>
    );
}

function StatCard(props: { title: string; value: string; subtitle: string }) {
    return (
        <div className="stat-card">
            <div className="icon">🏫</div>
            <div>
                <h3>{props.title}</h3>
                <strong>{props.value}</strong>
                <p>{props.subtitle}</p>
            </div>
            <span className="arrow">→</span>
        </div>
    );
}

function CardHeader({ title }: { title: string }) {
    return (
        <div className="card-header">
            <h2>{title}</h2>
            <button>Alle anzeigen</button>
        </div>
    );
}

function LevelOverview(props: {
    title: string;
    courses: string;
    participants: string;
}) {
    return (
        <div className="level-card">
            <h3>{props.title}</h3>
            <div className="level-values">
                <div>
                    <strong>{props.courses}</strong>
                    <span>Lehrgänge</span>
                </div>
                <div>
                    <strong>{props.participants}</strong>
                    <span>Teilnehmer</span>
                </div>
            </div>
            <button>Übersicht anzeigen →</button>
        </div>
    );
}

const courses = [
    {
        name: "Truppmann Teil 1",
        level: "municipality",
        label: "Gemeinde",
        date: "15.06.2025",
        location: "FF Musterstadt",
        slots: "5 / 20",
    },
    {
        name: "Sprechfunker",
        level: "district",
        label: "Kreis",
        date: "22.06.2025",
        location: "Kreisfeuerwehrzentrum",
        slots: "8 / 16",
    },
    {
        name: "Maschinist für Löschfahrzeuge",
        level: "state",
        label: "Landes",
        date: "05.07.2025",
        location: "Landesfeuerwehrschule",
        slots: "3 / 24",
    },
];

const requests = [
    {
        department: "Freiwillige Feuerwehr Musterstadt",
        course: "Atemschutzgeräteträger",
        date: "20.05.2025",
        status: "Ausstehend",
        statusClass: "pending",
    },
    {
        department: "Freiwillige Feuerwehr Beispielhausen",
        course: "Maschinist für Löschfahrzeuge",
        date: "19.05.2025",
        status: "Genehmigt",
        statusClass: "approved",
    },
    {
        department: "Freiwillige Feuerwehr Sonnendorf",
        course: "Gruppenführer",
        date: "18.05.2025",
        status: "Abgelehnt",
        statusClass: "rejected",
    },
];
