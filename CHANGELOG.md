# Changelog

Alle wichtigen Änderungen an diesem Projekt werden in dieser Datei dokumentiert.

Das Format orientiert sich an Keep a Changelog und Semantic Versioning.

## [Unreleased]

### Added
- Changelog-Datei eingeführt.
- README auf die aktuelle Multi-Projekt-Struktur (Web, Desktop, Mobile, Common) angepasst.
- Geraete-Verwaltungsmodul (Inventory) mit REST API-Endpunkten implementiert.
- `InventoryItemController` fuer CRUD-Operationen auf Inventargegenstände (GET, POST, PUT, DELETE).
- `InventoryService` als Service-Layer fuer Geschaeftslogik der Geraeteverwaltung.
- `IInventoryRepository` Interface zur Entkopplung von Datenzugriff und Geschaeftslogik.
- Umfangreiche Unit-Tests fuer `InventoryItemController` mit Edge-Cases und Grenzwertpruefungen.
- Unterstuetzung fuer Inventargegenstände mit Eigenschaften: Id, Name, Beschreibung, Kaufdatum, Standort.

### Changed
- README um kommunalen Einsatzkontext der Feuerwehr und nicht-funktionale Leitplanken erweitert.
- Security Policy um sicherheitsrelevante Anforderungen fuer den kommunalen Betrieb erweitert.
- Support-Dokument um Priorisierung fuer einsatzkritische Stoerungen erweitert.
- Governance um fachliche Abstimmung mit kommunalen Stakeholdern erweitert.
- Dokumentation um Datenschutz- und Compliance-Rahmen inklusive Audit-Logging erweitert.
- **CI-Workflow** (.github/workflows/ci.yml) fuer automatisierte Builds und Tests bei jedem Push.
  - Automatisches Builden der kompletten Solution.
  - Ausfuehrung aller Unit-Tests mit Code Coverage.
  - Upload von Test-Ergebnissen und Coverage-Berichten als Artefakte.
- **CD-Workflow** (.github/workflows/cd.yml) fuer automatisierte Deployments.
  - Container-Publishing: Docker-Image-Erstellung und Push zu GitHub Container Registry.
  - Desktop-App-Publishing: Build fuer Windows x64 und ARM64 als Self-Contained Single-File.
  - Android-App-Publishing: Automatischer Upload zu Google Play Store (Internal Testing bei `main`, Production bei Version-Tags).
