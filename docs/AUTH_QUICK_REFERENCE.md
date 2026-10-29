# Kurzübersicht: Authentifizierung

## Endpunkte

| Methode | Pfad | Zugriff | Zweck |
|---|---|---|---|
| `POST` | `/api/Auth/login` | Anonym | Anmelden |
| `POST` | `/api/Auth/refresh` | Anonym | Access- und Refresh-Token erneuern |
| `POST` | `/api/Auth/logout` | Angemeldet | Refresh-Token widerrufen |
| `GET` | `/api/Auth/me` | Angemeldet | Eigenes Profil laden |
| `POST` | `/api/Auth/reset-password` | Anonym | Eingeladenes Konto aktivieren / Kennwort setzen |
| `POST` | `/api/Auth/change-password` | Angemeldet | Eigenes Kennwort ändern |
| `GET`, `POST`, `PUT`, `DELETE` | `/api/Users` | `Admin` | Benutzer verwalten |

## Zugriff nach Bereich

| Rollen/Policy | Zugriff |
|---|---|
| `Admin`, `Commander`, `Firefighter`, `Viewer` | Allgemeine Daten lesen (`CanViewData`) |
| `Admin`, `Commander` | Hydranten und Ausbildung verwalten |
| `Admin` | Benutzerverwaltung |
| `Admin`, `EquipmentOfficer`, `InventoryViewer` | Inventar ansehen |
| `Admin`, `EquipmentOfficer` | Inventar verwalten |
| `SituationMapViewer`, `SituationMapEditor` | Lagekarte lesen |
| `SituationMapEditor` | Lagekarte bearbeiten |
| `IncidentDiaryViewer`, `IncidentDiaryEditor` | Einsatztagebuch lesen |
| `IncidentDiaryEditor` | Einsatztagebuch bearbeiten |

Rollen werden nicht automatisch vererbt. Lagekarte und Tagebuch haben getrennte Rechte; eine Modulrolle ersetzt keine Rolle des anderen Moduls.

## Kontoschutz und Einrichtung

- Access-Token: standardmäßig 15 Minuten; Refresh-Token: standardmäßig 7 Tage.
- Fünf fehlgeschlagene Kennwortprüfungen führen zu einer Sperre von 15 Minuten.
- Initiales Administratorkonto: `SeedAdmin__Email` (Standard-E-Mail: `admin@feuerwehr.local`) und `SeedAdmin__Password` über sichere Konfiguration setzen.
- Es gibt **kein** fest eingebautes Standardpasswort. Beim ersten Anlegen des Kontos ist `SeedAdmin__Password` erforderlich.
- Einladungslinks müssen über die konfigurierte `Frontend__BaseUrl` auf das richtige Frontend verweisen.
- JWT-Schlüssel, Mail-Zugangsdaten und Initialpasswörter gehören in einen Secret Store bzw. Umgebungsvariablen, nie in Quellcode oder öffentliche Beispiele.

## Initialen Administrator konfigurieren

In PowerShell vor dem ersten Start setzen:

```powershell
$env:SeedAdmin__Email = 'admin@example.org'
$env:SeedAdmin__Password = '<privates-initialpasswort>'
dotnet run --project src/Feuerwehr.AppHost/Feuerwehr.AppHost.csproj
```

Platzhalter durch ein privates Kennwort ersetzen und nicht in Dateien oder Logs speichern. Wenn das Konto bereits angelegt wurde, ändert eine neue Umgebungsvariable dessen Kennwort nicht.

Ausführliche Details: [Authentifizierung und Berechtigungen](AUTHENTICATION.md).
