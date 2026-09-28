# Lagekarte und Einsatztagebuch

## Produktumfang und Rollenmatrix

Die Module **Lagekarte** und **Einsatztagebuch** teilen sich denselben `Incident`.

| Rolle | Rechte |
|---|---|
| `SituationMapViewer` | Lagekarte, Änderungslog, Archiv lesen |
| `SituationMapEditor` | plus Einsatz anlegen, Bearbeitungssperre übernehmen/freigeben, Karte ändern, Einsatz abschließen, Kartenexporte |
| `IncidentDiaryViewer` | Tagebuch inkl. Historie/Archiv lesen |
| `IncidentDiaryEditor` | plus Einträge anlegen, korrigieren, stornieren, Tagebuch-PDF exportieren |
| `Admin` | Rollenzuweisungen, Tagebuchkategorien verwalten; kein automatischer Inhaltzugriff ohne Modulrolle |

## Architektur und Datenmodell

Backend (ASP.NET Core + EF Core/PostgreSQL):
- `Incident` als gemeinsamer Wurzelzustand
- `MapElement`, `MapAuditEvent`, `EditorLease` für Lagekarte
- `DiaryEntry`, `DiaryEntryRevision`, `DiaryCategory` für Tagebuch
- `ProcessedCommand` für Idempotenz
- `OutboxMessage` für transaktionale Live-Events

Wesentliche Entscheidungen:
- Nur ein aktiver Einsatz über partiellen Unique-Index (`Status = Active`)
- Karten-/Tagebuchmutationen lehnen bei abgeschlossenem Einsatz ab
- Map-Audits und Outbox werden in derselben DB-Transaktion wie die Fachänderung gespeichert
- Signalisierung über SignalR-Hubgruppen (`incident`, `map`, `diary`)

## Sperre, Idempotenz, Versionierung und Reconnect

- Bearbeitungssperre (`EditorLease`) ist an `incidentId + userId + sessionId + leaseToken` gebunden.
- Heartbeat verlängert die Sperre auf 120 Sekunden (Intervall 30 Sekunden empfohlen).
- Abgelaufene/übernommene Leases können keine Mutationen mehr ausführen.
- Jeder Schreibbefehl nutzt `CommandId`; Wiederholung mit identischer Nutzlast liefert gespeicherte Antwort.
- Gleiche `CommandId` mit abweichender Nutzlast wird abgelehnt.
- Tagebuch und Lagekarte nutzen erwartete Versionen (`ExpectedRevision` / `ExpectedVersion`) für Konflikterkennung.
- Clients laden nach Live-Invalidierung immer einen frischen Snapshot.

## API-Übersicht

- `POST /api/incidents`, `GET /api/incidents/active`, `GET /api/incidents/archive`, `POST /api/incidents/{id}/close`
- `GET /api/incidents/{id}/map/state`
- `POST /api/incidents/{id}/map/lease/{acquire|heartbeat|release}`
- `POST /api/incidents/{id}/map/elements`, `DELETE /api/incidents/{id}/map/elements/{elementId}`
- `GET /api/incidents/{id}/map/audit`, `GET /api/incidents/{id}/map/audit/export.csv`, `GET /api/incidents/{id}/map/export.pdf`
- `GET /api/incidents/{id}/diary/entries`, `POST /api/incidents/{id}/diary/entries`
- `POST /api/incidents/{id}/diary/entries/{entryId}/revisions`, `POST /api/incidents/{id}/diary/entries/{entryId}/cancel`
- `GET /api/incidents/{id}/diary/categories`, `GET /api/incidents/{id}/diary/export.pdf`
- `GET/POST /api/diary-categories` (Admin)

## Migrationen und Start

Neue Migration:
- `src/Backend/Feuerwehr.Server/Migrations/20260927070000_AddIncidentModules.cs`

Start:
1. Backend mit vorhandener Konfiguration starten (`Program.cs` führt `MigrateAsync` aus)
2. Frontend starten (`npm install`, `npm run dev`)
3. Rollen zuweisen (`SituationMapViewer|Editor`, `IncidentDiaryViewer|Editor`)

## OSM-/Symbolquellen und Lizenzhinweise

- Kartengrundlage: OpenStreetMap-Tiles, Standard `https://tile.openstreetmap.org/{z}/{x}/{y}.png`
- Attribution ist in UI und Karten-PDF enthalten.
- URL konfigurierbar via `VITE_OSM_TILE_URL`.
- Verwendeter Symbolsatz ist aktuell ein versionierter interner Startkatalog (Unicode-Icons), fachliche Normfreigabe steht noch aus.

## SignalR-Betrieb hinter Reverse Proxy

- Hub-Endpunkt: `/hubs/incidents`
- JWT wird für Hubverbindungen über Query-Token (`access_token`) unterstützt.
- Reverse Proxy muss WebSocket-Upgrade + Sticky Session / Forwarded Headers entsprechend bestehender Infrastruktur zulassen.
- Bei Verbindungsverlust bleibt letzter Stand sichtbar; neue Schreibaktionen werden in der UI als unsicher markiert und sollen nach Reconnect mit derselben `CommandId` wiederholt werden.

## Bestätigte Anforderungen

- Gemeinsame Incident-ID für beide Module
- Rollengetrennter Lese-/Schreibzugriff
- Ein aktiver Einsatz gleichzeitig
- Exklusive Lagekartenbearbeitung über Lease
- Tagebuchversionen mit Korrektur- und Stornierungsverlauf
- Paged Auditlog + CSV/PDF-Exporte
- SignalR-Liveupdates über Outbox

## Zusätzliche Implementierungsannahmen

- `Incident.Status` (`Active`/`Closed`) steuert Archiv/Mutationssperre.
- Tagebuchnummern stammen aus DB-Sequenz `DiaryEntryNumberSeq`.
- PDF-Exporte sind bewusst minimal gehalten und enthalten bestätigten Zustand als textuelle Repräsentation.

## Verbleibende fachliche Abnahmepunkte

- Karten-PDF ist aktuell textbasiert und enthält keine gerenderte Kartenkachel/Geometrieabbildung.
- Symbolkatalog benötigt fachliche Endabnahme (Norm-/Lizenzprüfung).
- Erweiterte End-to-End-Tests für Reconnect-Race-Conditions und Mehrinstanzbetrieb fehlen noch.
- Migration-Snapshot konnte in dieser Umgebung nicht automatisch neu generiert werden.

## Testbefehle und Ergebnisprotokoll

Geplant/ausgeführt:
- `npm run build` (Frontend)
- `dotnet restore src/Backend/Feuerwehr.Server/Feuerwehr.Server.csproj`
- `dotnet build src/Backend/Feuerwehr.Server/Feuerwehr.Server.csproj`
- `dotnet test src/Backend/Tests.Feuerwehr.Backend/Tests.Feuerwehr.Backend.csproj`

Ergebnis in dieser Laufumgebung:
- Frontend-Build wurde ausgeführt (siehe Abschlussbericht).
- .NET-Restore/Build/Test waren durch externe Feed-/Workload-Infrastruktur in dieser Sandbox nicht vollständig ausführbar.

## Manuelle Abnahmecheckliste (kurz)

1. Rollen zuweisen und Login mit Viewer/Editor/Admin testen.
2. Einsatz anlegen (Klickposition), Sperre prüfen, Heartbeat abwarten/ablaufen lassen.
3. Symbole/Flächen platzieren, verschieben, bearbeiten, löschen und Auditlog verifizieren.
4. Tagebucheinträge anlegen, korrigieren, stornieren (mit Pflichtgrund), Versionshistorie prüfen.
5. Einsatz abschließen; Mutationen müssen in beiden Modulen scheitern, Archive/Exporte lesbar bleiben.
6. Zwei Browser-Tabs für Lease-/Konfliktszenarien verwenden.
