# AGENTS.md

## Scope
- Gilt fuer das gesamte Repository `Feuerwehr-KPP/` (Quellcode unter `src/`).
- Haupt-Einstieg: `src/de.openelp.feuerwehr.slnx` (Common, Web, Desktop, Mobile, Tests).

## Architektur in 30 Sekunden
- Schichten in `src/Common`: `domain` -> `application` -> `infrastructure`.
- `domain`: Entitaeten ohne Persistenzlogik (`src/Common/domain/InventoryItem.cs`, `src/Common/domain/Hydrant.cs`).
- `application`: Services + Repo-Interfaces (`src/Common/application/inventory/*`, `src/Common/application/hydrant/*`).
- `infrastructure`: EF Core/Npgsql + Repo-Implementierungen (`src/Common/infrastructure/*Repository.cs`, `AppDbContext.cs`).
- Web API verdrahtet alles in `src/Web/api/Program.cs`; Controller liegen in `src/Web/api/Controllers/`.
- DB-Migrationen laufen beim API-Start automatisch via `app.ApplyMigrations()` (`MigrationExtensions.cs`).

## Datenfluss und Coding-Muster
- Request-Fluss: Controller -> Service -> Repository -> `AppDbContext`.
- Beispiel: `InventoryItemController` -> `IInventoryService`/`InventoryService` -> `IInventoryRepository`/`InventoryRepository`.
- Repositories arbeiten aktuell synchron (`SaveChanges()`), Services kapseln teils mit `Task.FromResult(...)`.
- Controller greifen teils synchron auf Service-Tasks zu (`.Result`); nur konsistent end-to-end auf async umbauen.

## Build, Run, Test
- SDK/Test-Runner sind gepinnt in `src/global.json` (`net10.0`, `Microsoft.Testing.Platform`).
- Restore/Build: `dotnet restore src/de.openelp.feuerwehr.slnx`, `dotnet build src/de.openelp.feuerwehr.slnx`.
- API starten: `dotnet run --project src/Web/api/de.openelp.feuerwehr.Api.csproj`.
- Tests: `dotnet test src/de.openelp.feuerwehr.slnx`.
- Dev-Services per Docker Compose: `src/docker/docker-compose.yml` (`postgres`, `redis`, `pgadmin`, `de.openelp.feuerwehr.api`).

## Konfiguration und Integrationen
- API-ConnectionString: `ConnectionStrings:DefaultConnection` in `src/Web/api/appsettings.json` (Standardhost `postgres`).
- EF Design-Time nutzt `src/Common/infrastructure/appSettings.json` + Umgebungsvariablen (`AppDbContextFactory`).
- Separater Auth-Service: `src/Web/de.openelp.authentification/` (JWT in `appsettings.json`).
- API-Doku im Development ueber OpenAPI + Scalar (`AddOpenApi`, `MapScalarApiReference`).

## Repo-spezifische Regeln fuer Agenten
- Namespace-Praefix beibehalten: `de.openelp.feuerwehr...`.
- Neue Businesslogik zuerst in `Common/application`, nicht direkt in Controller/EF.
- Neue Persistenzfunktion immer als Interface + Implementation + DI-Registration in `Program.cs`.
- Automatische Migrationen nicht still entfernen (Deployment-Strategie explizit abstimmen).
- Mobile-Paketversionen zentral in `src/Mobile/Directory.Packages.props` pflegen.
- Test-Stack: xUnit + Moq; Namensschema wie vorhanden (`<ClassName>Tests`, z. B. `HydrantControllerTests.cs`).
