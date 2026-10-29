# Authentifizierung und Berechtigungen

Das Backend verwendet ASP.NET Core Identity und JWT-Bearer-Tokens. API-Controller und Services prüfen Rollen und Policies serverseitig. Die Navigation im Webclient kann Funktionen ausblenden, ersetzt aber keine Prüfung durch die API.

## Anmeldung und Tokens

1. Der Client sendet E-Mail und Passwort an `POST /api/Auth/login`.
2. Bei Erfolg erhält der Client ein kurzlebiges Access-Token sowie ein Refresh-Token.
3. Das Backend speichert nicht das Refresh-Token im Klartext, sondern dessen Hash. Beim Refresh wird das Token rotiert.
4. `POST /api/Auth/refresh` erneuert Tokens; `POST /api/Auth/logout` widerruft das Refresh-Token.
5. Access-Tokens werden gegen aktiven Benutzerstatus und Security-Stamp geprüft. Nach einer Kennwortänderung wird das Refresh-Token widerrufen.

Die aktuelle Konfiguration setzt standardmäßig 15 Minuten für Access-Tokens und 7 Tage für Refresh-Tokens. Die Werte werden im Abschnitt `JwtSettings` konfiguriert.

## Erstes Administratorkonto

Beim API-Start werden Datenbankmigrationen und Seed-Daten ausgeführt. Falls das konfigurierte Administratorkonto noch nicht vorhanden ist, muss vor dem ersten Start `SeedAdmin__Password` sicher konfiguriert werden. Es gibt kein eingebautes Standardpasswort.

- `SeedAdmin__Email` wählt das Administratorkonto; ohne Angabe wird `admin@feuerwehr.local` verwendet.
- `SeedAdmin__Password` wird für die erstmalige Anlage benötigt und muss die Identity-Regeln erfüllen: mindestens acht Zeichen, Groß- und Kleinbuchstabe sowie eine Ziffer.
- Existiert das Konto bereits, wird sein Passwort durch eine Änderung der Umgebungsvariable nicht zurückgesetzt.
- Die Seed-Routine versieht das Initialkonto zusätzlich zu `Admin` mit `SituationMapEditor` und `IncidentDiaryEditor`.

Passwort und JWT-Schlüssel gehören in Umgebungsvariablen oder einen Secret Store, nicht in Git. Für JWT wird `JwtSettings__SecretKey` als Umgebungsvariable verwendet.

## Rollen und Policies

Die Rollen bilden keine automatisch vererbte Hierarchie. Ein Benutzer erhält Zugriff nur durch die für eine Funktion vorgesehenen Rollen:

| Bereich | Leserechte | Schreibrechte |
|---|---|---|
| Allgemeine Daten | `Admin`, `Commander`, `Firefighter`, `Viewer` (`CanViewData`) | Je nach Controller und Fachfunktion |
| Hydranten verwalten | — | `Admin`, `Commander` (`CanManageHydrants`) |
| Ausbildung verwalten | — | `Admin`, `Commander` (`CanManageTraining`) |
| Benutzerverwaltung | — | `Admin` |
| Inventar | `Admin`, `EquipmentOfficer`, `InventoryViewer` | `Admin`, `EquipmentOfficer` |
| Einsatz-Lagekarte | `SituationMapViewer`, `SituationMapEditor` | `SituationMapEditor` |
| Einsatztagebuch | `IncidentDiaryViewer`, `IncidentDiaryEditor` | `IncidentDiaryEditor` |
| Kartensymbole ansehen | `Admin`, `SituationMapViewer`, `SituationMapEditor` | Die Pflege wird durch die jeweiligen API-Aktionen eingeschränkt |

Die Lagekarte und das Tagebuch haben getrennte Rollen. Eine Rolle für ein Einsatzmodul verleiht nicht automatisch Rechte für das andere. `Admin` erhält nicht pauschal Inhaltsleserechte auf Einsatzmodule; das initial angelegte Administratorkonto wird jedoch ausdrücklich mit den beiden Editor-Rollen ausgestattet.

## Benutzer einladen und Kennwort setzen

Administratoren legen Benutzer über `POST /api/Users` an. Das Konto erhält dabei kein vom Request geliefertes Kennwort. Der Einladungsablauf sendet einen Link zum Setzen des Kennworts. Ein ausstehendes Kennwort wird durch `PasswordResetRequired` erzwungen; bis zur Kennwortvergabe ist das Konto gesperrt.

`POST /api/Auth/reset-password` ist der anonyme Abschluss des Einladungs-/Kennwortsetzungsablaufs. Ein angemeldeter Benutzer kann sein Kennwort über `POST /api/Auth/change-password` ändern. Das Backend bestimmt das Benutzerkonto dabei aus dem JWT und verlangt das aktuelle Kennwort.

## Kontosperre

Identity sperrt ein Konto nach fünf fehlgeschlagenen Kennwortprüfungen für 15 Minuten. Die Fehlversuchsgrenze gilt gemeinsam für Anmeldung und Kennwortwechsel. Eine aktive Sperre führt auch bei einem noch gültigen Bearer-Token zu HTTP 423.

## Clientverhalten

- Der Webclient verwaltet Authentifizierungszustand über `AuthContext`; der zentrale API-Client behandelt Token-Erneuerung und erneute Requests.
- Geschützte Webrouten verlangen eine Anmeldung. Die Autorisierung der API bleibt maßgeblich.
- Der Avalonia-Client verwendet gemeinsame Authentifizierungs- und API-Services. Die Plattformprojekte müssen für produktive Nutzung hinsichtlich sicherer Tokenablage separat geprüft werden; der allgemeine Client-Service enthält eine In-Memory-Speicherimplementierung für lokale Szenarien.
- Für SignalR-Verbindungen zum Einsatz-Hub kann das Access-Token über `access_token` übermittelt werden. Reverse Proxies müssen WebSocket-Verbindungen unterstützen.

## Relevante Endpunkte

| Methode und Pfad | Zugriff | Aufgabe |
|---|---|---|
| `POST /api/Auth/login` | Anonym | Anmeldung |
| `POST /api/Auth/refresh` | Anonym, Refresh-Token und Access-Token im Request | Token-Erneuerung |
| `POST /api/Auth/logout` | Angemeldet | Refresh-Token widerrufen |
| `GET /api/Auth/me` | Angemeldet | Aktuelles Benutzerprofil |
| `POST /api/Auth/reset-password` | Anonym | Kennwort über Einladungsablauf setzen |
| `POST /api/Auth/change-password` | Angemeldet | Eigenes Kennwort ändern |
| `/api/Users` | `Admin` | Benutzerverwaltung und Einladungen |

Weitere Beispiele ohne Zugangsdaten stehen in der [Kurzübersicht](AUTH_QUICK_REFERENCE.md). Einsatzrollen und Modulgrenzen siehe [Lagekarte und Einsatztagebuch](incident-modules.md).
