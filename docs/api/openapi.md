# OpenAPI für GitBook erzeugen

Das Backend registriert OpenAPI mit `AddOpenApi()` und stellt das Dokument in Development über `MapOpenApi()` bereit. Der Standard-Endpunkt ist `/openapi/v1.json`; Scalar ist unter `/scalar/v1` verfügbar. Der API-Start führt außerdem Datenbankmigrationen und Seed-Daten aus, daher müssen die Backend-Abhängigkeiten und erforderlichen Entwicklungswerte verfügbar sein.

## Voraussetzungen

- API in der Development-Umgebung gestartet
- API-Basis-URL aus dem Aspire-Dashboard oder vom gestarteten Server
- Node.js/npm über `npx`

Der AppHost setzt im Development-Betrieb lokale Infrastruktur auf. Starte ihn aus dem Repository-Root:

```powershell
dotnet run --project src/Feuerwehr.AppHost/Feuerwehr.AppHost.csproj
```

## YAML-Datei erzeugen

Das Repository enthält `scripts/Export-OpenApi.ps1`. Das Skript lädt das echte JSON-Dokument vom laufenden Development-Server und bündelt es mit der Redocly CLI als YAML. Es fügt keine selbst gepflegten API-Endpunkte hinzu.

```powershell
.\scripts\Export-OpenApi.ps1 -ApiBaseUrl 'https://localhost:7538'
```

Die Standardausgabe ist `docs/api/openapi.yml`. Wenn der API-Endpunkt im Aspire-Dashboard eine andere URL hat, verwende stattdessen diese Basis-URL:

```powershell
.\scripts\Export-OpenApi.ps1 -ApiBaseUrl 'https://localhost:<port>'
```

Das Skript verwendet `npx --yes @redocly/cli`; die CLI wird nicht als dauerhafte Abhängigkeit zum Frontend hinzugefügt. Es schlägt fehl, wenn das API-Dokument nicht abgerufen oder die YAML-Ausgabe nicht erzeugt werden kann. Die fertige Datei sollte vor dem Commit bzw. Upload geprüft werden, insbesondere auf versehentlich enthaltene interne URLs oder ausschließlich lokale Entwicklungsinformationen.

## GitBook verwenden

GitBook kann eine OpenAPI-Spezifikation hochladen oder von einer erreichbaren URL abrufen. Eine generierte Datei lässt sich in der GitBook-Organisation als OpenAPI-Spezifikation registrieren. Die resultierende Spec-Slug-Referenz wird in `SUMMARY.md` als `builtin:openapi` eingebunden, statt API-Endpunkte manuell nachzuschreiben.

Beispiel für einen SUMMARY-Eintrag, nachdem die Spec in GitBook registriert wurde:

````markdown
## API-Referenz

* [Überblick](api/README.md)
* ```yaml
  type: builtin:openapi
  props:
    models: true
    downloadLink: true
  dependencies:
    spec:
      ref:
        kind: openapi
        spec: <gitbook-spec-slug>
  ```
````

Bei direktem Upload müssen spätere Änderungen an `openapi.yml` erneut in GitBook hochgeladen werden. Wenn die Datei an einer für GitBook erreichbaren Stelle veröffentlicht wird, kann GitBook die URL als Quelle verwenden; dadurch wird die Spezifikation getrennt vom Seiteninhalt gepflegt und GitBook aktualisiert sie nach seinem Abrufplan.

## Dokumentation bei einem Release aktualisieren

Der CI-Workflow aktualisiert die vorhandenen Dokumentationsseiten nach einem veröffentlichten GitHub-Release, sofern Backend-, Frontend- und Integrationstests erfolgreich sind. Er erstellt dafür einen GitBook-Change-Request, aktualisiert die Seiten aus `docs/` und führt den Change-Request anschließend zusammen. Die Änderung an der CI wird durch `scripts/publish_gitbook_docs.py` ausgeführt.

Für die GitHub-Repository-Einstellungen ist ein Actions-Secret `GITBOOK_TOKEN` mit Bearbeitungsrechten auf den Dokumentationsbereich erforderlich. Das Token darf nicht in Workflow-Dateien oder im Quellcode gespeichert werden. Die Space-ID ist im Skript hinterlegt und kann bei Bedarf über `GITBOOK_SPACE_ID` überschrieben werden.

Der Release-Job aktualisiert Seiteninhalte und verlinkt lokale Screenshots aus dem jeweiligen Release-Commit. Er erzeugt **keine** neue `openapi.yml` und registriert keine neue OpenAPI-Spezifikation; dafür bleibt der Export oben erforderlich.

## Geltungsbereich

Das Dokument entspricht den von ASP.NET Core erfassten API-Metadaten. Die API-Spezifikation ersetzt weder Zugriffskontrollen noch eine fachliche Prüfung der Beschreibungen und Beispiele. Da der Endpunkt nur in Development verfügbar ist, sollte die YAML-Datei in einem kontrollierten Build erzeugt und geprüft werden, bevor sie öffentlich bereitgestellt wird.
