# AGENTS.md

## Scope
- Gilt fuer das gesamte Repository `Feuerwehr-KPP/` (Quellcode unter `src/`).
- Haupt-Einstieg: `src/Feuerwehr.slnx` (Common, Backend, App, Frontend, AppHost und Tests).

## Architektur in 30 Sekunden
- Ausschliesslich gemeinsam genutzte Web-API-Vertraege (Requests, Responses, DTOs und deren Enums/Wertobjekte) liegen in `src/Feuerwehr.Common/Models/`. Keine EF-Entities oder UI-Modelle in Common.
- Die ASP.NET-Core-API liegt in `src/Backend/Feuerwehr.Server/`.
- API-Controller liegen in `src/Backend/Feuerwehr.Server/Controller/`.
- Authentifizierung und Autorisierung liegen in `src/Backend/Feuerwehr.Server/Authorization/` und `Services/`.
- EF-Core-Datenzugriff und Migrationen liegen in `src/Backend/Feuerwehr.Server/Data/` und `Migrations/`.
- Das React-/TypeScript-Frontend liegt in `src/frontend/`.
- Avalonia-Clients liegen in `src/App/Feuerwehr.App/` (Desktop, Android und iOS).
- Der .NET-Aspire-AppHost liegt in `src/Feuerwehr.AppHost/AppHost.cs` und verdrahtet API, Frontend, Desktop-App, PostgreSQL mit PgWeb, Redis und Mailpit.

## Projekte und Abhaengigkeiten
- `Feuerwehr.Common`: unabhaengige Vertragsbibliothek fuer .NET 10.
- `Feuerwehr.Server` -> Common; ASP.NET Core, Identity/JWT, EF Core/PostgreSQL, Redis und SignalR.
- `Tests.Feuerwehr.Backend` -> Server und Common.
- `Feuerwehr.App` -> Common; gemeinsame Avalonia-Oberflaeche mit MVVM, Authentifizierung und Mapsui.
- Desktop, Android und iOS -> App. Android nutzt `net10.0-android`; iOS nutzt auf macOS `net10.0-ios`, sonst einen `net10.0`-Dummy.
- `Feuerwehr.AppHost` -> Server und Desktop; orchestriert das Frontend als Vite-Ressource.
- `frontend` kommuniziert zur Laufzeit ueber HTTP/SignalR mit der API; keine .NET-Projektreferenz und keine automatische TypeScript-Vertragsgenerierung.

## Datenfluss und Coding-Muster
- Request-Fluss: Controller -> Service -> `FeuerwehrDbContext`.
- Controller enthalten Routing, HTTP-Validierung, Berechtigungsattribute und die Abbildung von Service-Ergebnissen auf HTTP-Antworten. Kein direkter EF-, Identity-, Redis- oder SMTP-Zugriff in Controllern.
- Fachliche Ablaeufe und Datenzugriffe in scoped Services mit Interfaces unter `Services/` implementieren und in `Program.cs` registrieren. `ServiceResult` bleibt ein internes Anwendungsergebnis; die MVC-Abbildung liegt in `Controller/ServiceResultExtensions.cs`.
- Persistenzmodelle liegen im Server unter `Models/`; UI-spezifische Modelle im jeweiligen Client. DTOs nicht von Entities ableiten und keine Entities als API-Antworten serialisieren.
- Hydranten besitzen getrennte DTO-/Entity-Typen; Abbildung in `Services/HydrantService.cs`. Bestehende HTTP-Routen und JSON-Feldnamen bei Refactorings beibehalten.
- API-Einstieg und Dependency Injection: `src/Backend/Feuerwehr.Server/Program.cs`.
- Datenbankmigrationen und Seed-Daten werden beim API-Start in `Program.cs` ausgeführt.
- Änderungen an gemeinsamen Verträgen zuerst in `src/Feuerwehr.Common/Models/` prüfen und danach API- und Client-Projekte anpassen.

## Funktionsbereiche und Einstiegspunkte
- Hydranten, Feuerwehren, Benutzer/Rollen und Ausbildung: jeweilige Controller und Services.
- Einsaetze, Lagekarte, Bearbeitungssperren, Tagebuch und Kategorien: `Controller/IncidentModules/`, `Services/IncidentModules/`, `Models/IncidentModules/` und Common `Models/IncidentModules/`.
- Idempotenz ueber CommandId und Nutzlast-Hash, Outbox und Hintergrundversand: `Services/IncidentModules/IncidentModuleHelpers.cs`. Hash-Nutzlast und gespeicherte Antworten bei Vertragsaenderungen beruecksichtigen.
- Tagebuchrevisionen, Audit-Ereignisse, Versionspruefungen und Sperren nicht umgehen; Events mit den Daten-/Command-Aenderungen konsistent persistieren.
- SignalR: `Hubs/IncidentHub.cs`, Endpoint `/hubs/incidents`; JWT-Token fuer diesen Pfad auch aus `access_token`. Live-Client: `frontend/src/api/incidentLive.ts`.
- API-Start/DI: `Program.cs`; ServiceDefaults, Health Checks und OpenTelemetry direkt in `Extensions.cs`, kein separates ServiceDefaults-Projekt.
- Avalonia: Desktop `Program.cs`, Android `Application.cs`/`MainActivity.cs`, iOS `Main.cs`/`AppDelegate.cs`; gemeinsame DI und Plattformauswahl in `App.axaml.cs`.
- React: `src/main.tsx` -> `src/App.tsx`; Anmeldung in `AuthContext.tsx`, API-Zugriff und Token-Erneuerung in `src/api/apiClient.ts`.

## Build, Run, Test
- Alle .NET-Projekte nutzen .NET 10. Es gibt derzeit keine `global.json`; die CI waehlt `10.0.x`.
- Node laut `frontend/package.json`: `^20.19.0 || >=22.12.0`; CI: `22.12.0`.
- Restore/Build: `dotnet restore src/Feuerwehr.slnx`, `dotnet build src/Feuerwehr.slnx`.
- Ein Solution-Build benoetigt die Android-Toolchain. Der iOS-Dummy unter Windows/Linux prueft keinen echten iOS-Build; dieser benoetigt macOS, Xcode und die iOS-Workload.
- Android Release benoetigt die konfigurierte Signierung/Keystore. Fuer lokale Gesamtpruefungen standardmaessig Debug verwenden.
- Backend ohne Mobile-Toolchain: `dotnet build src/Backend/Feuerwehr.Server/Feuerwehr.Server.csproj`.
- Desktop: `dotnet build src/App/Feuerwehr.App/Feuerwehr.App.Desktop/Feuerwehr.App.Desktop.csproj`; Start mit `dotnet run --project` und demselben Pfad.
- Aspire-Umgebung starten: `dotnet run --project src/Feuerwehr.AppHost/Feuerwehr.AppHost.csproj`.
- API starten: `dotnet run --project src/Backend/Feuerwehr.Server/Feuerwehr.Server.csproj`.
- Tests: `dotnet test src/Feuerwehr.slnx`.
- Gezielt Backend: `dotnet test src/Backend/Tests.Feuerwehr.Backend/Tests.Feuerwehr.Backend.csproj`.
- Nach erfolgreichem Build derselben Konfiguration: `dotnet test src/Feuerwehr.slnx --no-build --no-restore`.
- Frontend separat: `cd src/frontend`, `npm ci`, `npm run build`, `npm run lint`; Entwicklungsserver: `npm run dev`.
- `frontend.esproj` deaktiviert NPM-Installation und Build-Skript. Ein erfolgreicher Solution-Build validiert das Web-Frontend nicht.
- Kein Frontend-Testskript/Test-Runner eingerichtet. Die CI prueft Backend-Build/Tests, Web-Build und Desktop auf Windows/Linux; Mobile-Jobs sind auskommentiert.
- Bestehender ESLint-Bestand ist noch nicht fehlerfrei (u. a. `no-explicit-any` und Hook-Abhaengigkeiten); neue Aenderungen sollen keine weiteren Befunde hinzufuegen.

## Konfiguration und Integrationen
- API-Konfiguration: `src/Backend/Feuerwehr.Server/appsettings.json` und `appsettings.Development.json`.
- Die API erhält Datenbank-, Cache- und Mailpit-Verbindungen im Entwicklungsbetrieb über Aspire.
- API-Verbindungen heissen `ConnectionStrings:postgresdb` und `ConnectionStrings:cache`; SMTP liest `MAIL_HOST`/`MAIL_PORT` (Fallback localhost:1025). Ein separater API-Start benoetigt diese Infrastruktur und Konfiguration.
- Avalonia priorisiert `webapi`, danach Aspire `services__server__https__0`/`services__server__http__0`; ohne Konfiguration wird die Integrationsumgebung angesprochen.
- Web-API-Basis: `VITE_API_URL`, sonst gleiche Origin. Vite leitet `/api` und `/hubs` (WebSockets) weiter; Ziel aus `API_HTTPS`/`API_HTTP`, danach Aspire-Servicevariablen, sonst `https://localhost:7538`. AppHost setzt derzeit `VITE_API_URL` explizit auf diese HTTPS-Adresse.
- JWT-Konfiguration liegt im Abschnitt `JwtSettings` der API-Konfiguration.
- API-Doku im Development ueber OpenAPI + Scalar (`AddOpenApi`, `MapScalarApiReference`).
- `/health` und `/alive` sind nur in Development gemappt; Prometheus unter `/metrics`.
- EF-Design-Time-Einstieg: `Data/FeuerwehrDbContextFactory.cs`; dessen feste lokale Verbindung vor echten Datenbankoperationen pruefen. Namespaces und EF-Snapshots bei Modellverschiebungen konsistent halten; reine Umbenennungen sollen keine Schemaaenderungen erzeugen.

## Repo-spezifische Regeln fuer Agenten
- Namespace-Praefix: `de.openelp.feuerwehr`, z. B. `.Server`, `.Common`, `.App` und `.Tests.Backend`. Projektnamen, Assemblynamen und kurze Verzeichnispfade beibehalten; AXAML-Klassen und CLR-Namespace-Verweise passend aktualisieren.
- Neue Businesslogik nicht direkt im Controller, sondern in den Services unter `src/Backend/Feuerwehr.Server/Services/` umsetzen.
- EF-Konfiguration und DbContext-Factory in `Data/`, Entities in Server `Models/`, Datenzugriffe in Services pflegen; neue Services in `Program.cs` registrieren.
- Automatische Migrationen und Seed-Daten in `Program.cs` nicht still entfernen.
- Alle benannten API- und Live-Event-DTOs ausschliesslich in `src/Feuerwehr.Common/Models/` pflegen; keine doppelten Auth-DTOs oder serverlokalen Requests/Responses einfuehren.
- Paketversionen der Avalonia-Clients zentral in `src/App/Feuerwehr.App/Directory.Packages.props` pflegen.
- Die zentrale Paketverwaltung gilt fuer Desktop, Android und iOS; keine `Version` an deren PackageReferences. Zusammengehoerige Avalonia-Pakete kompatibel halten.
- Die gemeinsam verwendete Avalonia-Version wird einmal ueber `AvaloniaVersion` in der zentralen Datei gesetzt.
- NuGet-Abhaengigkeitsgrenzen beachten: insbesondere `Microsoft.OpenApi` passend zu `Microsoft.AspNetCore.OpenApi` waehlen. NU1608 nicht bloss unterdruecken.
- Test-Stack: xUnit + Moq, EF InMemory und SQLite; vorhandene Controller-Tests: `UsersControllerTests` und `IncidentDiaryControllerTests`. Service-Tests unter `Services/`, Architekturregeln in `ArchitectureTests.cs`.
- Relevante Service-/Controller-Tests und bei Common-Aenderungen betroffene Clients pruefen. EF-InMemory/SQLite ersetzen keine PostgreSQL-Integrationstests, etwa fuer Sequenzen.
- Bekannte Grenzen: Passwort-Reset im AuthService ist noch nicht implementiert; `UnitTest1` ist ein leerer Platzhalter, einige Tests sind auskommentiert. Ein gruener Testlauf bedeutet keine vollstaendige Funktionsabdeckung.
