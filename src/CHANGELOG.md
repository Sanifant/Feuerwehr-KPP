# Changelog

Alle wichtigen Änderungen an diesem Projekt werden in dieser Datei dokumentiert.

Das Format orientiert sich an [Keep a Changelog](https://keepachangelog.com/de/1.0.0/) und [Semantic Versioning](https://semver.org/lang/de/).

## [Unreleased]

### Added
- Changelog-Datei eingeführt.
- README auf die aktuelle Multi-Projekt-Struktur (Web, Desktop, Mobile, Common) angepasst.
- **Geraete-Verwaltungsmodul (Inventory)** mit REST API-Endpunkten implementiert:
  - `InventoryItemController` fuer CRUD-Operationen auf Inventargegenstände (GET, POST, PUT, DELETE).
  - `InventoryService` als Service-Layer fuer Geschaeftslogik der Geraeteverwaltung.
  - `IInventoryRepository` Interface zur Entkopplung von Datenzugriff und Geschaeftslogik.
  - Umfangreiche Unit-Tests fuer `InventoryItemController` mit Edge-Cases und Grenzwertpruefungen.
  - Unterstuetzung fuer Inventargegenstände mit Eigenschaften: Id, Name, Beschreibung, Kaufdatum, Standort.
  - Testprojekt fuer Web-API mit MSTest und Moq angelegt (API.UnitTests).
- **Docker & Container**:
  - Dockerfile fuer .NET 10 Web-API als Multi-Stage-Build mit optimiertem Layer-Caching und Healthcheck.
  - `.dockerignore` fuer kleinere Docker-Images und schnellere Builds.
- **NuGet & Package-Management**:
  - NuGet.config mit .NET 10 Preview/RC-Feeds und PackageSourceMapping fuer gezielte Paketquellen.
- **Plattformunterstuetzung**:
  - Android und Browser (WASM) Plattformen hinzugefuegt.
  - .NET 10 SDK und Multi-Platform-Support in der Projektstruktur.
- **CI/CD-Infrastruktur**:
  - GitLab CI/CD-Pipeline (.gitlab-ci.yml) zur Solution hinzugefuegt.
  - Separate Build- und Test-Jobs fuer alle Kern- und Testprojekte (API, Infrastructure, Domain, Application, Desktop, Mobile).
  - Dedizierte Stages: restore, build, test, analyze, package, deploy.
  - Coverage-Reports pro Testkategorie als Cobertura-Artefakte.
  - Optionaler Docker-Build und GitLab Security-Scans.
  - Android-Workload-Installation mit mehreren Feeds und Fallback-Strategie.
- **GitHub Actions Workflows**:
  - **CI-Workflow** (`.github/workflows/ci.yml`) fuer automatisierte Builds und Tests bei jedem Push:
    - Automatisches Builden der kompletten Solution.
    - Ausfuehrung aller Unit-Tests mit Code Coverage.
    - Upload von Test-Ergebnissen und Coverage-Berichten als Artefakte.
  - **CD-Workflow** (`.github/workflows/cd.yml`) fuer automatisierte Deployments:
    - Container-Publishing: Docker-Image-Erstellung und Push zu GitHub Container Registry.
    - Desktop-App-Publishing: Build fuer Windows x64 und ARM64 als Self-Contained Single-File.
    - Android-App-Publishing: Automatischer Upload zu Google Play Store (Internal Testing bei `main`, Production bei Version-Tags).
  - **Changelog-Workflow** (`.github/workflows/changelog.yml`) fuer automatische CHANGELOG-Verwaltung:
    - Automatische Generierung von CHANGELOG-Einträgen aus Commits (Conventional Commits).
    - Preview-Kommentare in Pull-Requests mit geplanten CHANGELOG-Änderungen.
    - Automatische Release-Sektion-Erstellung beim Publishing eines Releases.
- **Dokumentation**:
  - CI-CD-SETUP.md: Umfassendes Setup, Troubleshooting, Deployment und Best Practices.
  - PIPELINE-ZUSAMMENFASSUNG.md: Uebersicht ueber Pipeline-Struktur und Jobs.
  - ANDROID-WORKLOAD-FIX.md: Workarounds und Diagnose fuer .NET 10 Android-Workload-Probleme (NETSDK1147).
  - CHANGELOG-AUTOMATION.md: Dokumentation zur automatischen CHANGELOG-Verwaltung mit GitHub Actions.

### Changed
- README um kommunalen Einsatzkontext der Feuerwehr und nicht-funktionale Leitplanken erweitert.
- Security Policy um sicherheitsrelevante Anforderungen fuer den kommunalen Betrieb erweitert.
- Support-Dokument um Priorisierung fuer einsatzkritische Stoerungen erweitert.
- Governance um fachliche Abstimmung mit kommunalen Stakeholdern erweitert.
- Dokumentation um Datenschutz- und Compliance-Rahmen inklusive Audit-Logging erweitert.
- **Projektarchitektur grundlegend refaktoriert**:
  - Klare Schichten-Trennung (Web, Desktop, Mobile, Common/Domain/Application/Infrastructure).
  - Dependency Injection (DI) durchgaengig implementiert.
  - Entity Framework Core mit PostgreSQL-Support.
  - Docker-ready Architektur.
- **GitLab CI/CD-Pipeline modularisiert und erweitert**:
  - Von generischer .NET Core-Vorlage zu projektspezifischer Multi-Platform-Pipeline umgebaut.
  - Neue Umgebungsvariablen fuer Infrastructure-, Domain- und Application-Projekte inkl. Tests.
  - Package-Jobs bauen erst nach erfolgreichen Tests.
  - Android-Builds als optional markiert (`allow_failure: true`) wegen .NET 10 Workload-Verfuegbarkeit.
  - Pipeline jetzt modularer, transparenter, wartbar und production-ready.
- **Testabdeckung und Codequalitaet** fuer Inventarverwaltung deutlich verbessert.

### Fixed
- Android-Workload-Installation optimiert mit mehreren Feeds und Fallback-Strategie.
- NETSDK1147-Fehler durch NuGet.config und Feed-Konfiguration adressiert.
- Docker-Build-Prozess stabilisiert mit expliziten Pfaden und Branch-Konfiguration.

### Security
- GitLab Security-Scans in CI/CD-Pipeline integriert.
- Audit-Logging-Anforderungen fuer kommunalen Betrieb dokumentiert.
