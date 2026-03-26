# Feuerwehr

Monorepo fuer die Feuerwehr-Anwendung auf Basis von .NET 10 und Avalonia.

## Einsatzkontext

Die Software ist fuer den kommunalen Einsatz bei Feuerwehren vorgesehen.
Der Fokus liegt auf einem stabilen und nachvollziehbaren Betrieb in Behoerden- und Leitstellenumgebungen.

## Nicht-funktionale Leitplanken

- Hohe Verfuegbarkeit im Einsatzbetrieb
- Nachvollziehbarkeit von Aenderungen und Prozessen
- Datenschutzgerechte Verarbeitung einsatzrelevanter Daten
- Rollenbasierte Nutzung und klare Berechtigungskonzepte

## Projektueberblick

Die Solution liegt unter `src/de.openelp.feuerwehr.slnx` und umfasst:

- Common: Geteilte Modelle und Kernlogik (inkl. Inventory-Service und Repository-Interfaces)
- Desktop: Avalonia Desktop-Anwendung
- Mobile: Avalonia Shared UI plus Plattform-Hosts (Android, iOS, Browser, Desktop)
- Web: ASP.NET Core Web API (inkl. Geraete-Verwaltung REST API)

## Funktionsumfang

### Geraete-Verwaltung (Inventory Management)
- REST API zur Verwaltung von Feuerwehr-Inventar und Geraeten
- CRUD-Operationen fuer Inventargegenstände:
  - `GET /api/InventoryItem` - Alle Geraete abrufen
  - `GET /api/InventoryItem/{id}` - Einzelnes Geraet abrufen
  - `POST /api/InventoryItem` - Neues Geraet anlegen
  - `PUT /api/InventoryItem/{id}` - Geraet aktualisieren
  - `DELETE /api/InventoryItem/{id}` - Geraet loeschen
- Repository-Pattern zur Entkopplung von Datenzugriff und Geschaeftslogik
- Service-Layer fuer Geschaeftslogik und Orchestrierung

## Voraussetzungen

- .NET SDK 10.0
- Docker und Docker Compose fuer das Development Environment
- Fuer Mobile-Targets zusaetzlich:
    - Android SDK/Workloads
    - Xcode/Apple Tooling fuer iOS (unter macOS)

## Build

Aus dem Repo-Root:

```bash
dotnet restore src/de.openelp.feuerwehr.slnx
dotnet build src/de.openelp.feuerwehr.slnx
```

## Starten

Development Environment in VS Code:

```bash
code .
```

Anschliessend den Ordner im Devcontainer neu oeffnen. Dabei werden der Workspace-Container sowie `postgres`, `redis` und `pgadmin` automatisch ueber Docker Compose gestartet.

Manueller Start der relevanten Services ohne Devcontainer:

```bash
docker compose -f src/docker-compose.yml -f src/docker-compose.override.yml up -d postgres redis pgadmin de.openelp.feuerwehr.api
```

Web API:

```bash
dotnet run --project src/Web/de.openelp.feuerwehr/de.openelp.feuerwehr.Api.csproj
```

Desktop-App:

```bash
dotnet run --project src/Desktop/de.openelp.feuerwehr.desktop/de.openelp.feuerwehr.desktop.csproj
```

Mobile Browser Host:

```bash
dotnet run --project src/Mobile/de.openelp.feuerwehr.mobile.Browser/de.openelp.feuerwehr.mobile.Browser.csproj
```

## Changelog

Historie und relevante Aenderungen findest du in `CHANGELOG.md`.

## Weitere Dokumente

- Sicherheit: `Security.md`
- Support: `Support.md`
- Governance: `Governance.md`
- Datenschutz und Compliance: `Datenschutz.md`

## Beitrag leisten

Siehe `CONTRIBUTING.md` fuer Richtlinien zur Mitarbeit.