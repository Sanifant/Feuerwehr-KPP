# Projektüberblick und Entwicklungsstart

Feuerwehr-KPP ist ein Monorepository mit einer ASP.NET-Core-Web-API, einem React-Webclient und einem gemeinsamen Avalonia-Client für Desktop und mobile Plattformen. Die Lösung liegt unter `src/Feuerwehr.slnx`.

## Projekte

| Bereich | Projektpfad | Aufgabe |
|---|---|---|
| Gemeinsame Verträge | `src/Feuerwehr.Common/` | API-Requests, Responses, DTOs und gemeinsame Werttypen |
| Backend | `src/Backend/Feuerwehr.Server/` | ASP.NET Core, Identity/JWT, Services, EF Core und SignalR |
| Webclient | `src/frontend/` | React, TypeScript und Vite |
| Avalonia-Oberfläche | `src/App/Feuerwehr.App/Feuerwehr.App/` | Gemeinsame Views, ViewModels und Client-Services |
| Plattform-Hosts | `src/App/Feuerwehr.App/Feuerwehr.App.Desktop/`, `Feuerwehr.App.Android/`, `Feuerwehr.App.iOS/` | Plattform-Einstiegspunkte |
| Orchestrierung | `src/Feuerwehr.AppHost/` | Aspire-Orchestrierung für API, Webclient, PostgreSQL, Redis und Mailpit |
| Tests | `src/Backend/Tests.Feuerwehr.Backend/`, `src/App/Feuerwehr.App/Tests.Feuerwehr.App/`, `src/Backend/Tests.Feuerwehr.Backend.Integration/` | Backend-, Client- und Integrationstests |

Die Repository-Entscheidung für diese Struktur ist in [ADR-0001](adr/0001-use-monorepo.md) dokumentiert. Die Entscheidung für Avalonia ist in [ADR-0002](adr/0002-use-avalonia-for-cross-platform-clients.md) dokumentiert.

## Voraussetzungen

- .NET SDK 10
- Node.js `^20.19.0` oder `>=22.12.0` für das Frontend
- Docker für die lokale Infrastruktur des AppHost
- Android-SDK und passende .NET-Workloads für Android-Builds
- macOS, Xcode und iOS-Workload für echte iOS-Builds

## Entwicklungsumgebung starten

Vom Repository-Root:

```powershell
dotnet run --project src/Feuerwehr.AppHost/Feuerwehr.AppHost.csproj
```

Der AppHost orchestriert PostgreSQL, Redis, Mailpit, die API, das Frontend und den Desktop-Client. Die dynamischen Endpunkte und die Mailpit-Oberfläche sind im Aspire-Dashboard sichtbar.

Für den ersten API-Start wird ein Initialpasswort über `SeedAdmin__Password` benötigt, sofern das konfigurierte Administratorkonto noch nicht existiert. Das Passwort wird nicht mit dem Quellcode ausgeliefert; Einzelheiten stehen unter [Authentifizierung](AUTHENTICATION.md).

## Einzelprojekte starten

API (benötigt konfigurierte PostgreSQL-, Redis-, JWT- und gegebenenfalls Mail-Einstellungen):

```powershell
dotnet run --project src/Backend/Feuerwehr.Server/Feuerwehr.Server.csproj
```

Frontend:

```powershell
Set-Location src/frontend
npm ci
npm run dev
```

Desktop-Client:

```powershell
dotnet run --project src/App/Feuerwehr.App/Feuerwehr.App.Desktop/Feuerwehr.App.Desktop.csproj
```

## Build und Tests

Backend:

```powershell
dotnet build src/Backend/Feuerwehr.Server/Feuerwehr.Server.csproj
dotnet test src/Backend/Tests.Feuerwehr.Backend/Tests.Feuerwehr.Backend.csproj
```

Webclient:

```powershell
Set-Location src/frontend
npm ci
npm run build
npm test
```

Ein Solution-Build kann die Android-Toolchain benötigen. Auf Windows wird das iOS-Projekt als Dummy-Ziel gebaut; das ersetzt keinen iOS-Build mit Xcode auf macOS. Nicht jede Funktion verfügt über vollständige Integration- oder End-to-End-Tests.

## Wichtige Konfigurationsgrenzen

- Gemeinsame API-Verträge liegen in `src/Feuerwehr.Common/Models/`; Datenbank-Entities bleiben im Backend.
- Backend-Fachlogik und Datenzugriff gehören in Services, nicht in Controller.
- Datenbankmigrationen und Seed-Daten werden beim API-Start ausgeführt.
- Lokale Entwicklungswerte und produktive Secrets sind getrennt zu verwalten. Keine Passwörter oder JWT-Schlüssel in Git einchecken.
- Details zu Einsatzmodulen und deren offenen Abnahmepunkten stehen in [Lagekarte und Einsatztagebuch](incident-modules.md).
