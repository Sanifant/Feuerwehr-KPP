# AGENTS.md

## Scope
- Gilt fuer das gesamte Repository `Feuerwehr-KPP/` (Quellcode unter `src/`).
- Haupt-Einstieg: `src/Feuerwehr.slnx` (Common, Backend, App, Frontend, AppHost und Tests).

## Architektur in 30 Sekunden
- Gemeinsame Modelle und DTOs liegen in `src/Feuerwehr.Common/Models/`.
- Die ASP.NET-Core-API liegt in `src/Backend/Feuerwehr.Server/`.
- API-Controller liegen in `src/Backend/Feuerwehr.Server/Controller/`.
- Authentifizierung und Autorisierung liegen in `src/Backend/Feuerwehr.Server/Authorization/` und `Services/`.
- EF-Core-Datenzugriff und Migrationen liegen in `src/Backend/Feuerwehr.Server/Data/` und `Migrations/`.
- Das React-/TypeScript-Frontend liegt in `src/frontend/`.
- Avalonia-Clients liegen in `src/App/Feuerwehr.App/` (Desktop, Android und iOS).
- Der .NET-Aspire-AppHost liegt in `src/Feuerwehr.AppHost/AppHost.cs` und verdrahtet API, Frontend, PostgreSQL, Redis und Mailpit.

## Datenfluss und Coding-Muster
- Request-Fluss: Controller -> Service -> `FeuerwehrDbContext`.
- API-Einstieg und Dependency Injection: `src/Backend/Feuerwehr.Server/Program.cs`.
- Datenbankmigrationen und Seed-Daten werden beim API-Start in `Program.cs` ausgeführt.
- Änderungen an gemeinsamen Verträgen zuerst in `src/Feuerwehr.Common/Models/` prüfen und danach API- und Client-Projekte anpassen.

## Build, Run, Test
- SDK-Version ist in `src/global.json` festgelegt.
- Restore/Build: `dotnet restore src/Feuerwehr.slnx`, `dotnet build src/Feuerwehr.slnx`.
- Aspire-Umgebung starten: `dotnet run --project src/Feuerwehr.AppHost/Feuerwehr.AppHost.csproj`.
- API starten: `dotnet run --project src/Backend/Feuerwehr.Server/Feuerwehr.Server.csproj`.
- Tests: `dotnet test src/Feuerwehr.slnx`.
- Frontend starten: `cd src/frontend`, danach `npm install` und `npm run dev`.

## Konfiguration und Integrationen
- API-Konfiguration: `src/Backend/Feuerwehr.Server/appsettings.json` und `appsettings.Development.json`.
- Die API erhält Datenbank-, Cache- und Mailpit-Verbindungen im Entwicklungsbetrieb über Aspire.
- JWT-Konfiguration liegt im Abschnitt `JwtSettings` der API-Konfiguration.
- API-Doku im Development ueber OpenAPI + Scalar (`AddOpenApi`, `MapScalarApiReference`).

## Repo-spezifische Regeln fuer Agenten
- Namespace-Praefix beibehalten: `de.openelp.feuerwehr...`.
- Neue Businesslogik nicht direkt im Controller, sondern in den Services unter `src/Backend/Feuerwehr.Server/Services/` umsetzen.
- Neue Persistenzfunktion in `Data/` ergänzen und in `Program.cs` registrieren.
- Automatische Migrationen und Seed-Daten in `Program.cs` nicht still entfernen.
- Gemeinsame Modelle unter `src/Feuerwehr.Common/Models/` pflegen.
- Paketversionen der Avalonia-Clients zentral in `src/App/Feuerwehr.App/Directory.Packages.props` pflegen.
- Test-Stack: xUnit + Moq; Namensschema wie vorhanden (`<ClassName>Tests`, z. B. `HydrantControllerTests.cs`).
