# Implementierungsstand

Diese Seite fasst den aktuellen Repository-Stand zusammen. Sie ist keine Abnahme oder Produktionsfreigabe; Plattform- und Betriebsanforderungen müssen für den konkreten Einsatz separat geprüft werden.

## Im Repository vorhanden

- **Backend:** ASP.NET-Core-Web-API mit Identity/JWT, EF Core, PostgreSQL, Redis-Integration, scoped Services und SignalR.
- **Webclient:** React-/TypeScript-Anwendung für Dashboard, Feuerwehren, Hydranten, Ausbildung, Benutzer, Einsätze, Lagekarte, Tagebuch und Inventar.
- **Avalonia-Client:** gemeinsame Oberfläche mit Hydrantenkarte, Einsatzkarte und Inventar-/Geräteprüfungsansicht; Desktop-, Android- und iOS-Host-Projekte.
- **Einsatzmodule:** Lagekarte und Tagebuch teilen einen Einsatz, verwenden getrennte Modulrollen sowie Versionierung, Audit-/Outbox-Ereignisse und Liveupdates.
- **Entwicklung:** Aspire-AppHost orchestriert lokale API- und Infrastrukturressourcen.
- **Dokumentation:** [Architektur](architecture.md), [Entwicklungsstart](getting-started.md), [Authentifizierung](AUTHENTICATION.md), [Einsatzmodule](incident-modules.md) und die akzeptierten [Architekturentscheidungen](adr/0001-use-monorepo.md) / [Avalonia-Entscheidung](adr/0002-use-avalonia-for-cross-platform-clients.md).

## Grenzen und offene Prüfungen

- Ein vorhandenes Plattformprojekt bedeutet nicht, dass alle Funktionen auf jeder Plattform gleich umgesetzt oder produktiv freigegeben sind.
- Plattformabhängige Tokenablage, Rechte, Offline-Verhalten, Paketierung und Signierung müssen je Plattform geprüft werden.
- Die Lagekarten-PDF-Ausgabe ist laut bestehender Einsatzmodul-Dokumentation derzeit textbasiert; sie ist keine gerenderte Kartenabbildung.
- Der Kartensymbolkatalog und die fachlichen Anforderungen benötigen die in [Lagekarte und Einsatztagebuch](incident-modules.md) beschriebenen Prüfungen.
- Reconnect-Races und Mehrinstanzbetrieb benötigen zusätzliche Integrationstests.
- OpenAPI und GitBook-API-Referenz müssen nach dem Start der API generiert und geprüft werden; siehe [OpenAPI für GitBook](api/openapi.md).

## Prüfkommandos

Gezielter Backend-Build und Tests:

```powershell
dotnet build src/Backend/Feuerwehr.Server/Feuerwehr.Server.csproj
dotnet test src/Backend/Tests.Feuerwehr.Backend/Tests.Feuerwehr.Backend.csproj
```

Frontend-Build und Tests:

```powershell
Set-Location src/frontend
npm ci
npm run build
npm test
```

Die Gesamt-Solution kann zusätzliche mobile Toolchains erfordern. Testergebnisse sind im jeweiligen CI-Lauf bzw. der lokalen Umgebung zu prüfen.
