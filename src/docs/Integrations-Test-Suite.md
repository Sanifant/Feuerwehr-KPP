# Testcontainers-Integrationssuite für die API

Stand: 29.10.2026. Grundlage: statische Prüfung von API, bestehenden Tests,
Migrationen, AppHost, API-Dockerfile und GitHub-Workflows. Dies ist ein
Implementierungsvorschlag; die Suite und die Action wurden noch nicht umgesetzt
oder ausgeführt. Aufwand und Laufzeiten sind Schätzungen.

## Empfehlung und bestehender Stand

Ein eigenes Projekt `src/Backend/Tests.Feuerwehr.Integration` mit xUnit,
`WebApplicationFactory<Program>` und echten PostgreSQL-, Redis- und
Mailpit-Containern aufbauen. Bestehende schnelle Tests weiterführen. Ergänzend
wenige Tests gegen das tatsächlich gebaute API-Containerimage ausführen.

Das Backend umfasst derzeit 11 Controller mit 56 HTTP-Aktionen sowie den
SignalR-Hub `/hubs/incidents`. Die Zahl der Aktionen beschreibt die Breite,
nicht die notwendige Zahl der Testfälle.

| Befund im Repository | Konsequenz für die Suite |
| --- | --- |
| `Program` ist bereits `public partial` | Einstieg für `WebApplicationFactory` vorhanden; zunächst kein Startumbau nötig |
| Controller-Tests instanziieren Controller direkt | Routing, JSON-Verarbeitung, Middleware und Berechtigungsattribute werden dort nicht als HTTP-Kette geprüft |
| `UserInvitationApiTests` verwendet echte Identity-Komponenten, aber EF InMemory und eigene Mail-/Redis-Ersatzimplementierungen | Gute fachliche Vorlage; SMTP-Zustellung, Redis-Serialisierung/TTL und Host-Konfiguration bleiben offen |
| PostgreSQL-spezifisches SQL und Sequenzen für Tagebuch/Audit | Gegen PostgreSQL prüfen; SQLite bildet diese Semantik nicht vollständig ab |
| `Program` migriert und seedet vor dem Start | Leere Datenbank durch den normalen Start migrieren lassen; kein `EnsureCreated` in dieser Suite |
| `SeedAdmin:Email` und `SeedAdmin:Password` konfigurieren den initialen Admin | Eigene Testwerte setzen und Anmeldung über `/api/Auth/login` durchführen |
| `OutboxDispatcher` ist ein echter Hosted Service mit festem 20-Sekunden-Intervall | Polling mit Deadline; optional das Intervall konfigurierbar machen, Produktionsdefault beibehalten |
| Nur Teile der Zeitlogik verwenden `TimeProvider` | Eine Fake-Uhr verändert weder Redis-TTL noch alle JWT-, Identity- oder Lease-Fristen |
| `/health` und `/alive` existieren nur in Development | Unter `Testing` andere Startprobe wählen; z. B. erwartetes 401 von `/api/Auth/me` plus erfolgreiche Anmeldung |
| `ci.yml` entdeckt rekursiv alle Testprojekte | Bestehenden Job auf konkrete Projekte umstellen, sonst wird die neue Suite zusätzlich dort ausgeführt |
| `cd.yml` wartet auf erfolgreichen Abschluss von `CI - Build and Test` | Integrationstest-Job in diesen Workflow aufnehmen, damit er das bestehende Deployment-Gate mit abdeckt |

## Möglichkeiten und Grenzen

| Variante | Prüft | Bewertung |
| --- | --- | --- |
| WebApplicationFactory + Testcontainers | HTTP-Pipeline, DI, Identity/JWT, Services, EF/Migrationen und echte Infrastruktur | Empfohlene Hauptsuite; vergleichsweise schnelle Diagnose |
| API-Image + Infrastruktur in Testcontainers-Netzwerk | Zusätzlich Dockerfile, veröffentlichte Dateien, Container-Konfiguration, Kestrel und echte Netzwerktransporte | Ergänzende Image-/WebSocket-Tests; bei Bedarf gleiche HTTP-Szenarien wiederverwenden |
| Gesamter Aspire-AppHost | Zusätzlich Verdrahtung mehrerer Anwendungen und Ressourcen | Für spätere Systemtests sinnvoll; für die API-Suite größere Start- und Toolchain-Abhängigkeiten |

`WebApplicationFactory` verwendet einen Testserver im Prozess: Requests durchlaufen
die echte ASP.NET-Core-Pipeline, aber nicht die produktive Kestrel-/TCP-Strecke.
Dieses Muster wird von [Microsoft](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0)
und [Testcontainers](https://dotnet.testcontainers.org/examples/aspnet/) dokumentiert.

„Vollständig“ als überprüfbares Ziel definieren: Jede HTTP-Aktion und Hub-Methode
hat einen Eintrag in einer Testmatrix mit Erfolgsfall, relevanten Fehlerfällen
und Berechtigungen. Kritische Geschäftsabläufe werden zusätzlich über mehrere
Requests und ihre externen Auswirkungen geprüft. Das ist keine Garantie für
alle möglichen Eingaben oder jede zeitliche Verschachtelung.

Frontend, Avalonia, Reverse Proxy, öffentliche TLS-Konfiguration und reale
SMTP-Provider benötigen eigene Systemtests. Mailpit prüft lokale SMTP-Zustellung,
aber nicht die Authentifizierung/TLS-Konfiguration eines externen Mailanbieters.

## Architektur und Isolation

```text
xUnit / HttpClient / SignalR-Client
                |
       WebApplicationFactory<Program>
       echte Middleware und Services
          |          |          |
     PostgreSQL     Redis      Mailpit
     Migrationen    TTL        SMTP -> REST-Prüfung
          |
       OutboxDispatcher -> SignalR-Ereignis

Zusatztests: HttpClient -> gebautes API-Image -> dieselben Infrastrukturtypen
```

Pro Testlauf ein Container-Set als xUnit-Collection-Fixture starten. PostgreSQL,
Redis und Mailpit können unabhängig voneinander starten. Erst nach erfolgreicher
Bereitschaftsprüfung den API-Host erzeugen. Docker-Ports dynamisch zuweisen,
keine vorhandene Aspire-Instanz oder Entwicklungsdatenbank verwenden.

Für den Einstieg alle HTTP-Testklassen derselben Collection zuordnen und
sequenziell ausführen. Jeder Test erzeugt eigene Benutzer, CommandIds und
fachliche Datensätze. Tests dürfen weder die Ausführungsreihenfolge noch bereits
vorhandene Daten eines anderen Tests voraussetzen. Gemeinsame Seed-Daten nicht
ändern; für Sperr-/Passworttests jeweils eigene Konten verwenden. Mailpit nach
eindeutigem Empfänger durchsuchen und Redis nur über testbezogene Schlüssel prüfen.

Tests mit Infrastrukturausfällen oder globalen Listen-/Zählassertionen erhalten
einen eigenen Host mit eigener Datenbank und bei Bedarf eigenen Containern.
Spätere Parallelisierung: je Testklasse/Worker eigene Datenbank und getrennte
Redis-/Mail-Daten. Erst bei gemessenem Bedarf optimieren.

Eine äußere Datenbanktransaktion um den Test isoliert HTTP-Requests und den
Hintergrunddienst nicht zuverlässig: Sie verwenden eigene DI-Scopes und
Verbindungen. Falls ein Reset-Verfahren wie Respawn verwendet wird, zunächst
Host/Dispatcher stoppen, dann Daten zurücksetzen und notwendige Seeds herstellen.
Nie parallel zu laufendem Outbox-Versand Tabellen leeren.

## Geplanter Testumfang

| Bereich | Wesentliche Szenarien |
| --- | --- |
| Start/Migrationen | Leere PostgreSQL-Datenbank, erkannte Migrationen angewandt, keine ausstehenden Migrationen, Rollen/Admin/Kategorien vorhanden; zweiter Hoststart auf derselben DB ohne doppelte Seeds |
| Migration-Upgrades | Datenbank auf einer unterstützten älteren Migration mit synthetischen Daten vorbereiten, danach normalen API-Start ausführen und Datenerhalt prüfen |
| Authentifizierung | Login, `/me`, falsches Passwort, inaktives Konto, ungültige/abgelaufene JWTs, Refresh-Ablauf und Widerruf, Logout |
| Passwort/Einladungen | Admin legt Konto an; echte E-Mail in Mailpit; UUID-Link verwenden; Redis-TTL nahe 60 Minuten; falsche E-Mail; Wiederverwendung; erneute Einladung entwertet alte Sitzung/Links; fünf Fehlversuche und Kontosperre |
| Fehler der Infrastruktur | SMTP-Verbindungsfehler und Rücknahme der Benutzeranlage; Redis-Ausfall; Cache-Löschfehler nach Passwortvergabe darf erfolgreichen Reset nicht zurücknehmen |
| Berechtigungen | Je Aktion anonym und fachlich relevante Rollen prüfen; 401/403 unterscheiden; ebenfalls Hub-Abonnements und abgeschlossene Einsätze berücksichtigen |
| Hydranten | CRUD, Fehlerfälle, PostgreSQL-ID-Vergabe, Import mit ungültigen Zeilen/CSV-Daten, Exportinhalt und Header |
| Feuerwehren/Ausbildung | Anlegen/Lesen/Ändern soweit angeboten, Zuordnungen und Validierungsfehler |
| Benutzerverwaltung | Anlage, Rollenwechsel, Deaktivierung/Löschung, neue Sitzung nach Änderungen; sensible Daten fehlen in Antworten |
| Einsätze | Anlegen/Lesen/Abschließen, Revisionen, unzulässige Folgeänderungen, archivierte Daten |
| Tagebuch | Echte `nextval`-Nummerierung, Revisionen, Korrektur/Storno, Kategorien, Audit und PDF-Inhalt |
| Lagekarte/Symbole | Lease erwerben/erneuern/freigeben, fremde und abgelaufene Lease, Versionenkonflikte, Symbol-/Kategorieverwaltung, SVG-/Bildvalidierung, Audit/CSV/PDF |
| Idempotenz/Parallelität | Gleiche CommandId mit gleicher Nutzlast wiederholen; abweichende Nutzlast/Benutzer ablehnen; parallele Befehle dürfen keine doppelten Fachänderungen verursachen |
| Outbox/SignalR | Vor Command abonnieren, Gruppenaufnahme abwarten; korrektes Event und Nutzlast empfangen; passender Outbox-Datensatz verarbeitet; unerlaubte Gruppen und fremde Einsatzgruppen erhalten keine Daten |
| HTTP-Vertrag | JSON-Feldnamen, Statuscodes, ProblemDetails, Content-Type/Download-Header, Model-Binding/Validierung, CORS mit konfigurierter Test-Origin |

Erwartetes Verhalten anhand der Verträge und fachlichen Anforderungen festlegen;
unerwartete 500er nicht als Sollzustand übernehmen. Für parallele Requests separate
Clients/Sitzungen und eine Startbarriere verwenden. Sequenzen müssen eindeutig
und aufsteigend sein, dürfen nach Rollbacks aber Lücken haben.

Redis-TTL einmal direkt auf dem echten Schlüssel prüfen und dessen Ablauf für
einen gezielten Infrastrukturtest verkürzen. Fachliche Zeitgrenzen separat mit
kontrollierter Uhr testen. Redis folgt nicht der .NET-Testuhr. Auch Identity- und
JWT-Zeitprüfungen müssen gesondert berücksichtigt werden; keine 60-Minuten-Wartezeit
in der CI einbauen.

Mailpit stellt eine [REST-API](https://mailpit.axllent.org/docs/api-v1/) zum Lesen
und Suchen zugestellter Nachrichten bereit. Den Einladungslink aus der tatsächlich
empfangenen Nachricht gewinnen, nicht aus einem ersetzten Mailservice.

Beim Outbox-Test korrelierte Events anhand von Einsatz/Command verfolgen und mit
Deadline auf Zustellung sowie Persistenz warten. Ohne Intervalländerung mindestens
einen vollständigen 20-Sekunden-Zyklus plus Puffer einplanen. Keine feste kurze
Wartezeit und kein Anspruch auf Exactly-once-Zustellung bei einem Outbox-Verfahren.

## Implementierungsskizze

```text
src/Backend/Tests.Feuerwehr.Integration/
  Tests.Feuerwehr.Integration.csproj
  Infrastructure/
    ApiCollection.cs
    ApiFixture.cs                 # Container-Lebenszyklus und Fehlerdiagnose
    ApiFactory.cs                 # ausschließlich Testkonfiguration
    AuthenticatedClients.cs       # echter Login, eigener Client je Sitzung
    MailpitClient.cs              # Zustellung mit Deadline abfragen
    Eventually.cs                 # begrenztes Polling
  Startup/MigrationTests.cs
  Auth/AuthTests.cs
  Auth/InvitationFlowTests.cs
  Authorization/PolicyTests.cs
  Hydrants/HydrantApiTests.cs
  Administration/AdministrationApiTests.cs
  Incidents/IncidentApiTests.cs
  Incidents/DiaryApiTests.cs
  Incidents/SituationMapApiTests.cs
  Live/OutboxSignalRTests.cs
  Image/ApiImageTests.cs
```

Ziel: `net10.0`, Namespace `de.openelp.feuerwehr.Tests.Integration`, Referenzen
auf Server und Common. Das Projekt in `src/Feuerwehr.slnx` aufnehmen. Alle
benannten API-Verträge weiterhin aus Common verwenden.

Pakete: bestehende xUnit-2-/Test-SDK-/Runner-/Coverlet-Versionen aus dem
Backend-Testprojekt übernehmen; dazu `Microsoft.AspNetCore.Mvc.Testing` und
`Microsoft.AspNetCore.SignalR.Client` passend zur verwendeten ASP.NET-Core-Version
10.0.13. `Testcontainers`, `Testcontainers.PostgreSql` und `Testcontainers.Redis`
einheitlich versionieren; als recherchierte Ausgangsbasis eignet sich
[Testcontainers 4.16.0](https://github.com/testcontainers/testcontainers-dotnet/releases/tag/4.16.0).
Beim Restore die Auflösung insbesondere von `Microsoft.OpenApi` prüfen; keine
Versionswarnungen unterdrücken. Ein Wechsel auf xUnit 3 ist dafür nicht nötig.

Container-Bausteine, exemplarisch:

```csharp
// image-Werte zentral als geprüfte Tags/Digests hinterlegen.
var postgres = new PostgreSqlBuilder(images.Postgres).Build();
var redis = new RedisBuilder(images.Redis).Build();
var mailpit = new ContainerBuilder(images.Mailpit)
    .WithPortBinding(1025, true)
    .WithPortBinding(8025, true)
    .WithWaitStrategy(Wait.ForUnixContainer()
        .UntilHttpRequestIsSucceeded(r => r.ForPort(8025)))
    .Build();

await Task.WhenAll(postgres.StartAsync(), redis.StartAsync(), mailpit.StartAsync());
// Erst danach Factory/HttpClient erzeugen: Program migriert beim Hoststart.
```

Die Modul-Builder werden in der offiziellen Dokumentation für
[PostgreSQL](https://dotnet.testcontainers.org/modules/postgres/) und
[Redis](https://dotnet.testcontainers.org/modules/redis/) beschrieben;
[Wait-Strategien](https://dotnet.testcontainers.org/api/wait_strategies/)
ersetzen pauschale Startverzögerungen.

PostgreSQL 17 entspricht derzeit `deployment/docker-compose.yml`.
Redis verwendet dort noch `latest`; vor Festlegung der Testversion die tatsächlich
eingesetzte Version ermitteln und für Deployment und Tests abstimmen. Für alle
Testimages geprüfte Versionen/Digests festlegen, ohne persistente Entwicklungsvolumes
und ohne Container-Reuse in CI.

Konfiguration der Factory, exemplarisch (Namespaces/Imports und Fixture-Aufbau
sind in dieser Skizze ausgelassen):

```csharp
internal sealed class ApiFactory(IReadOnlyDictionary<string, string> settings)
    : WebApplicationFactory<de.openelp.feuerwehr.Server.Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        foreach (var pair in settings)
            builder.UseSetting(pair.Key, pair.Value);
    }
}
```

Die Fixture übergibt vor dem ersten Hoststart mindestens diese Werte:

| Schlüssel | Testwert/Herkunft |
| --- | --- |
| `ConnectionStrings:postgresdb` | `postgres.GetConnectionString()` |
| `ConnectionStrings:cache` | `redis.GetConnectionString()` |
| `MAIL_HOST` / `MAIL_PORT` | `mailpit.Hostname` / `mailpit.GetMappedPublicPort(1025)` |
| `MAIL_SECURITY` | `None` für das isolierte Mailpit |
| `MAIL_USERNAME` / `MAIL_PASSWORD` | Leer, damit keine geerbten Provider-Zugangsdaten wirken |
| `MAIL_FROM` | `noreply@integration.test` |
| `MAIL_TIMEOUT_SECONDS` | Begrenzter Wert; Ausfalltests mit kleinerem Timeout |
| `Frontend:BaseUrl` | `https://frontend.integration.test` |
| `SeedAdmin:Email` / `SeedAdmin:Password` | Nur für diese Suite definierte Werte, Passwort erfüllt Identity-Regeln |
| `JwtSettings:SecretKey`, `Issuer`, `Audience` | Eigener Testschlüssel mit mindestens 32 zufälligen Bytes und eigene Bezeichner |
| `AllowedOrigins:0` | `https://frontend.integration.test` |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | Leer; keine geerbte externe Telemetrieverbindung |

`UseSetting` folgt dem oben verlinkten Testcontainers-Beispiel. Frühe Konfiguration
ist hier wichtig, weil `Program` JWT-Werte schon vor `builder.Build()` liest.
Keine globalen Prozess-Umgebungsvariablen zwischen Tests umschalten und keine
Authentifizierung, Businessservices oder EF-Provider austauschen.

Fixture-Lebenszyklus mit dem bestehenden xUnit 2: `IAsyncLifetime.InitializeAsync()`
und `DisposeAsync()` liefern jeweils `Task`. Bei Startfehlern bereits erzeugte
Ressourcen ebenfalls aufräumen. Vor dem Dispose Containerlogs sichern; anschließend
zuerst Clients/Host, danach Container stoppen. Das Logverzeichnis über
`TEST_LOG_DIRECTORY` festlegen und sensible Mail-/Tokeninhalte nicht in CI-Logs
übernehmen. Testcontainers Resource Reaper aktiviert lassen.

Erster vertikaler Testablauf:

```text
Leere PostgreSQL-DB -> API-Start mit Migration/Seeds
-> Admin-Login per HTTP
-> POST /api/Users mit eindeutigem Empfänger
-> zugestellte Mail in Mailpit lesen und Link extrahieren
-> POST /api/Auth/reset-password
-> Login mit neuem Passwort -> GET /api/Auth/me
-> denselben Link erneut verwenden: abgelehnt
-> Persistenz/Redis-TTL bzw. Entfernung prüfen
```

Weitere Testdaten können über einen expliziten Arrange-Helfer mit echtem
`UserManager`/DbContext vorbereitet werden. Der jeweils geprüfte Ablauf muss über
HTTP gehen. Assertions dürfen zusätzlich Datenbank, Redis und Mailpit lesen.

SignalR zunächst über den Testserver anbinden, z. B. Long Polling mit dessen
HTTP-Handler. Für echte WebSockets zusätzlich das API-Image testen: eigenes
Docker-Netzwerk, Aliase `postgres`, `redis`, `mailpit`, interne Containerports für
die API und dynamischer Hostport 8080 für den Testclient. Container-`localhost`
bezeichnet nicht den Testhost. Image aus dem aktuellen Checkout bauen
(`src/Backend/Feuerwehr.Server/Dockerfile`, Build-Kontext `src`), keinen bereits
veröffentlichten Integrations-Tag testen.

## GitHub Action

Als zusätzlichen Job unter `jobs:` in `.github/workflows/ci.yml` ergänzen.
Das ist bewusst ein Job-Sketch für die vorhandene `CI - Build and Test`, kein
bereits aktivierter separater Workflow. Die SHA-Pins entsprechen den derzeit im
Repository verwendeten Actions.

```yaml
  api-integration:
    name: API Integration (Linux)
    runs-on: ubuntu-24.04
    timeout-minutes: 20
    permissions:
      contents: read
    env:
      TEST_LOG_DIRECTORY: ${{ github.workspace }}/artifacts/api-integration/logs
    steps:
      - name: Checkout
        uses: actions/checkout@11d5960a326750d5838078e36cf38b85af677262

      - name: Setup .NET
        uses: actions/setup-dotnet@67a3573c9a986a3f9c594539f4ab511d57bb3ce9
        with:
          dotnet-version: '10.0.x'

      - name: Check Docker
        run: docker info

      - name: Restore integration tests
        run: dotnet restore src/Backend/Tests.Feuerwehr.Integration/Tests.Feuerwehr.Integration.csproj

      - name: Build integration tests
        run: dotnet build src/Backend/Tests.Feuerwehr.Integration/Tests.Feuerwehr.Integration.csproj -c Release --no-restore

      - name: Test API and infrastructure
        shell: bash
        run: |
          dotnet test src/Backend/Tests.Feuerwehr.Integration/Tests.Feuerwehr.Integration.csproj \
            -c Release --no-build --no-restore \
            --filter 'Category!=Image' \
            --logger 'trx;LogFileName=api-integration.trx' \
            --results-directory artifacts/api-integration/results \
            --collect 'XPlat Code Coverage' \
            --blame-hang --blame-hang-timeout 5m

      - name: Upload test results and logs
        if: always()
        uses: actions/upload-artifact@ea165f8d65b6e75b540449e92b4886f43607fa02
        with:
          name: api-integration-results
          path: artifacts/api-integration/
          if-no-files-found: warn
          retention-days: 14
```

Voraussetzung für dieses Beispiel: `coverlet.collector` ist im Testprojekt
referenziert und die Fixture schreibt Logs in das vereinbarte Verzeichnis.
Image-Tests mit `[Trait("Category", "Image")]` markieren. Coverage dieser
Hauptsuite erfasst den Server im Testprozess; API-Container benötigen eine
gesonderte Instrumentierung, falls dafür Coverage verlangt wird.

GitHub-hosted Linux-Runner bieten Docker für Testcontainers. Zusätzliche
`services:` für PostgreSQL/Redis oder Docker-in-Docker sind dafür nicht nötig;
den Lebenszyklus verwaltet die Fixture. Grundlage:
[Testcontainers CI-Dokumentation](https://dotnet.testcontainers.org/cicd/).

Zusätzlich im bestehenden Workflow ändern:

1. Den rekursiven PowerShell-Testloop durch explizite Aufrufe der bisherigen
   Backend- und App-Testprojekte ersetzen. In getrennten Steps schlagen Fehler
   zuverlässig fehl; die Integrationstests laufen ausschließlich im neuen Job.
2. Zum Push-Pfadfilter neben `src/**` auch `.github/workflows/ci.yml` hinzufügen,
   damit Änderungen am Workflow selbst einen Lauf auslösen.
3. Den neuen Check für Pull Requests als erforderlich konfigurieren. Da der Job
   in derselben CI liegt, berücksichtigt der vorhandene `workflow_run`-Trigger
   in `cd.yml` dessen Fehlschlagen bereits.
4. Kein `continue-on-error`, kein automatisches Überspringen bei fehlendem Docker,
   keine Produktionssecrets und keine pauschalen Wiederholungen fehlgeschlagener
   Tests. Fehlende Infrastruktur muss als Fehler sichtbar sein.

Für die Image-Ergänzung einen weiteren verpflichtenden Job in derselben CI
aufnehmen: Image einmal aus dem Checkout bauen, dem Image-Fixture den lokalen Tag
übergeben, `Category=Image` testen und API-/Containerlogs hochladen. Die
Image-Fixture startet ausschließlich die benötigten Container. Später können
umfangreichere Störungs-/Upgrade-Matrizen zusätzlich nachts laufen; die zentralen
HTTP- und Image-Tests bleiben im PR-/Deployment-Gate.

Lokaler Aufruf ab Repositorywurzel, mit laufendem Docker und Linux-Containern:

```powershell
dotnet test src/Backend/Tests.Feuerwehr.Integration/Tests.Feuerwehr.Integration.csproj --filter "Category!=Image"
```

## Aufwand und Einführung

Annahmen: eine mit C#/.NET vertraute Person, vorhandenes Docker, ein PT = acht
Stunden. Die Schätzung umfasst Tests und notwendige Testbarkeit, aber keine
größeren fachlichen Fehlerkorrekturen, die die neue Suite erst aufdeckt.

| Arbeitspaket | Aufwand |
| --- | ---: |
| Testprojekt, Container-Fixture, Factory, Isolation und erster durchgängiger Test | 2–3 PT |
| Auth, echte Einladungen, Rollenmatrix und Passwort-/Sitzungsabläufe | 2–3 PT |
| Hydranten, Import/Export, Feuerwehren, Ausbildung und Benutzerverwaltung | 2–3 PT |
| Einsätze, Tagebuch, Lagekarte, Idempotenz, Parallelität und Outbox/SignalR | 3–5 PT |
| CI-Integration, Artefakte und lokale Anleitung | 0,5–1 PT |
| Stabilisierung, Migration-/Fehlerfälle und Prüfung der Abdeckungsmatrix | 1–2 PT |
| **Breite API-Suite insgesamt, gerundet** | **11–17 PT** |
| Ergänzende Tests des gebauten API-Images und echter WebSockets | **zusätzlich 2–3 PT** |

Ein MVP mit PostgreSQL/Redis/Mailpit, Start-/Login-/Einladungsablauf, einem
PostgreSQL-spezifischen Fachablauf und CI ist in etwa **3–5 PT** realistisch und
Teil des Gesamtaufwands. Die vollständiger abgesicherte Kombination aus breiter
API-Suite und Image-Tests liegt bei **13–20 PT**. Für entdeckte Produktfehler
zusätzlich etwa 20–30 % Reserve einplanen; bei großen Befunden neu schätzen.

Als erste Laufzeitziele: Hauptsuite 2–6 Minuten nach verfügbarem Imagecache,
Image-Ergänzung weitere 2–5 Minuten. Das sind Planungswerte, keine Messungen;
Image-Downloads, Builds und 20-Sekunden-Outbox-Zyklen können dominieren.

Abnahme: Alle 56 HTTP-Aktionen und Hub-Methoden sind einer dokumentierten
Testmatrix zugeordnet; wesentliche Erfolge, Fehler und Berechtigungen geprüft;
echte Migrationen und externe Zustände nachgewiesen; wiederholte Läufe auf
sauberem Runner ohne Reihenfolgeabhängigkeit; Fehlschläge liefern verwertbare
Logs und blockieren den bestehenden CI-/Deployment-Pfad.
