# CHANGELOG Automation

Dieses Projekt verwendet automatisierte CHANGELOG-Verwaltung durch GitHub Actions.

## Wie funktioniert es?

### 1. **Bei Push/Pull-Request**
Der Workflow (`.github/workflows/changelog.yml`) analysiert alle Commits seit dem letzten Tag und generiert automatisch CHANGELOG-Einträge basierend auf:

#### Conventional Commits Format
- `feat:` → **Added**-Sektion
- `fix:` → **Fixed**-Sektion
- `chore:`, `refactor:` → **Changed**-Sektion
- `docs:` → **Changed**-Sektion (mit "Dokumentation:"-Präfix)
- `perf:` → **Changed**-Sektion (mit "Performance:"-Präfix)
- `security:` → **Security**-Sektion
- `remove:` → **Removed**-Sektion

#### Fallback für Standard-Commits
Wenn Commits nicht dem Conventional Commits Format folgen, werden sie anhand von Schlüsselwörtern kategorisiert:
- Enthält "add", "new", "implement" → **Added**
- Enthält "fix", "bug" → **Fixed**
- Enthält "update", "change", "refactor" → **Changed**

### 2. **Bei Pull-Requests**
- Erstellt automatisch einen **Comment** mit einer Vorschau der CHANGELOG-Änderungen
- Keine automatischen Commits (zur Vermeidung von Merge-Konflikten)

### 3. **Bei Releases**
Wenn ein neues Release erstellt wird:
1. Der gesamte **[Unreleased]**-Bereich wird in eine neue versionierte Sektion verschoben
2. Format: `## [v1.2.3] - 2024-01-15`
3. Ein neuer leerer **[Unreleased]**-Bereich wird erstellt
4. Änderungen werden automatisch committed und gepusht

## Best Practices

### Empfohlenes Commit-Format
Verwenden Sie [Conventional Commits](https://www.conventionalcommits.org/) für beste Ergebnisse:

```bash
feat: Neue Funktion für Geräte-Export
fix: Behebung des Login-Problems
chore: Abhängigkeiten aktualisiert
docs: README erweitert
refactor: Code-Struktur verbessert
perf: Datenbankabfragen optimiert
security: XSS-Schwachstelle behoben
```

### Mit Scopes (optional)
```bash
feat(inventory): Exportfunktion hinzugefügt
fix(api): Fehlerhafte Validierung korrigiert
chore(deps): .NET auf 10.0.1 aktualisiert
```

### Breaking Changes
Für Breaking Changes fügen Sie `!` hinzu:
```bash
feat!: API-Endpunkt umbenannt
```

## Manuelles CHANGELOG-Update

Falls Sie das CHANGELOG manuell bearbeiten möchten:
1. Bearbeiten Sie `CHANGELOG.md` direkt
2. Fügen Sie Einträge unter `## [Unreleased]` hinzu
3. Commit mit `[skip ci]` um den Workflow zu überspringen:
   ```bash
   git commit -m "docs: Manuelles CHANGELOG-Update [skip ci]"
   ```

## Workflow-Trigger

Der Workflow wird ausgeführt bei:
- ✅ Push auf beliebigen Branch
- ✅ Pull-Request zu `main`, `master`, oder `develop`
- ✅ Release (published/created)

## Konfiguration

### Branches anpassen
Bearbeiten Sie `.github/workflows/changelog.yml`:
```yaml
on:
  push:
    branches: [main, master, develop, 'feature/**']
  pull_request:
    branches: [main, master]
```

### Auto-Commit deaktivieren
Entfernen oder kommentieren Sie den Schritt "Commit and Push Changes" aus.

### Kategorien erweitern
Bearbeiten Sie den Abschnitt "Generate Changelog Entry" und fügen Sie weitere Patterns hinzu.

## Troubleshooting

### Workflow wird nicht ausgeführt
- Prüfen Sie, ob GitHub Actions aktiviert sind
- Stellen Sie sicher, dass `permissions: contents: write` gesetzt ist

### Commits werden nicht erkannt
- Verwenden Sie Conventional Commits Format
- Prüfen Sie, ob der letzte Tag korrekt erkannt wurde
- Führen Sie `git describe --tags --abbrev=0` lokal aus

### CHANGELOG wird nicht committed
- Der Workflow überspringt Commits bei `[skip ci]`
- Bei Pull-Requests werden keine Commits erstellt (nur Kommentare)

## Beispiel-Workflow

1. **Feature entwickeln** mit Commits:
   ```bash
   git commit -m "feat: Exportfunktion hinzugefügt"
   git commit -m "fix: Validierung korrigiert"
   ```

2. **Pull-Request erstellen**:
   - Workflow generiert Preview-Kommentar mit CHANGELOG-Änderungen

3. **Merge zu main**:
   - Workflow aktualisiert CHANGELOG im [Unreleased]-Bereich

4. **Release erstellen** (v1.2.0):
   - Workflow verschiebt [Unreleased] → [1.2.0]
   - Erstellt neuen leeren [Unreleased]-Bereich
   - Committed und pushed Änderungen

## Siehe auch

- [Keep a Changelog](https://keepachangelog.com/)
- [Semantic Versioning](https://semver.org/)
- [Conventional Commits](https://www.conventionalcommits.org/)
