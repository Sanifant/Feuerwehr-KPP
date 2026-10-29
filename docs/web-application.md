# Webanwendung

Der Webclient liegt unter `src/frontend/` und verwendet React, TypeScript, React Router und Vite. Authentifizierung und API-Kommunikation sind im Client zentralisiert; die API bleibt die maßgebliche Stelle für Berechtigungsprüfungen.

## Navigationsbereiche

Die geschützten Routen in `src/frontend/src/App.tsx` umfassen:

| Bereich | Route | Beschreibung |
|---|---|---|
| Anmeldung | `/login` | Öffentliche Anmeldung |
| Kennwort | `/password` und `/password/:token` | Kennwortwechsel bzw. Annahme einer Einladung |
| Dashboard | `/dashboard` | Anwendungsübersicht |
| Feuerwehren | `/firedepartments` | Verwaltung von Feuerwehren |
| Hydranten | `/hydrant` | Hydrantenverwaltung und Karte |
| Ausbildung | `/trainingdashboard` | Ausbildungsübersicht |
| Benutzer | `/users` | Benutzerverwaltung |
| Einsätze | `/incidents`, `/incident-archive` | Aktive Einsätze und Archiv |
| Einsatzmodule | `/incident-map`, `/incident-diary` | Lagekarte und Einsatztagebuch eines aktiven Einsatzes |
| Administration | `/diary-categories`, `/map-symbols` | Tagebuchkategorien und Kartensymbole |
| Inventar | `/inventory`, `/inventory/types`, `/inventory/categories` | Geräte, Typen und Kategorien |

Die Routenliste beschreibt die vorhandenen UI-Einstiegspunkte. Die tatsächliche Nutzung einzelner Aktionen hängt zusätzlich von serverseitigen Rollen und Richtlinien ab.

## API und Liveverbindungen

- HTTP-Aufrufe laufen über `src/frontend/src/api/apiClient.ts`.
- Der Webclient kommuniziert mit der API über HTTP und nutzt SignalR für Einsatz-Liveupdates.
- Vite leitet `/api` und `/hubs` im Entwicklungsbetrieb an die API weiter.
- Die SignalR-Verbindung verwendet `/hubs/incidents`; Verbindungsabbrüche und konkurrierende Änderungen sind im Einsatzkontext besonders zu beachten.

## Entwicklung

Die Befehle für Installation, Start, Build und Tests stehen unter [Projektüberblick und Entwicklungsstart](getting-started.md). Die Rollen- und Tokenregeln sind unter [Authentifizierung](AUTHENTICATION.md) beschrieben.
