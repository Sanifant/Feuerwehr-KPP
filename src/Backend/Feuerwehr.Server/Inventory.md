# Inventar und Geräteprüfung

## Verwendung

Die Weboberfläche ist unter `/inventory` erreichbar. Nach dem API-Neustart
stehen die initialen Kategorien **Löschgeräte**, **Armaturen** und
**Beleuchtung** sowie die beiden neuen Rollen zur Verfügung:

- `EquipmentOfficer`: in der Benutzerverwaltung **Gerätewart**; darf Gerätetypen
  und Geräte anlegen/bearbeiten, Prüfungen dokumentieren und Geräte deaktivieren
  oder wieder aktivieren.
- `InventoryViewer`: **Inventar ansehen**; darf Geräte und Prüfhistorien lesen.
- Administratoren dürfen das Inventar ebenfalls verwalten und zusätzlich neue
  Kategorien anlegen. Andere bestehende Rollen erhalten keinen Inventarzugriff.

Der aufklappbare Menübereich **Inventar** enthält die **Inventarübersicht**
(`/inventory`) und für Gerätewarte/Administratoren die Seite **Gerätetypen und
Prüfpläne** (`/inventory/types`). Die **Inventarkategorien** liegen für
Administratoren unter **Verwaltung** (`/inventory/categories`).

Zuerst einen Gerätetyp mit Kategorie, Prüfanweisung und Prüfintervall in
Kalendermonaten anlegen. Es gibt genau einen Prüfplan pro Gerätetyp. Ein Gerät
übernimmt diesen Plan. Änderungen des Plans wirken auf alle Geräte dieses Typs;
bereits dokumentierte Prüfungen behalten den damaligen Plan als Momentaufnahme.

Prüffristen werden bewusst nicht pauschal aus einer Kategorie abgeleitet.
Grundlage für die konkrete Festlegung sind Geräteausführung, Herstellerangaben
und die einschlägigen Prüfgrundsätze, unter anderem
[DGUV Grundsatz 305-002](https://publikationen.dguv.de/regelwerk/dguv-grundsaetze/84/pruefgrundsaetze-fuer-ausruestungen-geraete-und-fahrzeuge-der-feuerwehr).

Jedes Gerät besitzt eine UUID, einen unveränderlichen Barcode, Gerätetyp,
Seriennummer, Hersteller, Gerätestatus und einen getrennten Aktivzustand.
PostgreSQL vergibt die Barcodenummern atomar über eine nicht zyklische Sequenz
von 1 bis 999. Daraus entstehen `KPP-001` bis `KPP-999`. Deaktivierung gibt keine
Nummer frei. Nach Ausschöpfung antwortet die API mit HTTP 409 und einem Hinweis.
Fehlgeschlagene Transaktionen können Lücken in der Nummerierung hinterlassen.
Die Gerätedetails im Web bieten ein maschinenlesbares Code-128-Etikett als SVG
zur Anzeige und zum Herunterladen/Drucken an.

Die nächste Fälligkeit ist die letzte Prüfung plus Prüfintervall, bei neuen
Geräten das Anlagedatum plus Prüfintervall. Die Berechnung verwendet
Kalendermonate, berücksichtigt also Monatsenden und Schaltjahre. Datumswerte
werden ohne Uhrzeit übertragen; die API verwendet für den aktuellen Tag UTC.

Eine Prüfung enthält Datum, serverseitigen Erfassungszeitpunkt, Prüfer aus der
angemeldeten Sitzung, optionalen Kommentar und einen der Status **Einsatzklar**,
**Defekt** oder **In Reparatur**. Statusänderungen bestehender Geräte erfolgen
über Prüfprotokolle. Die Historie kann nicht über die API überschrieben oder
gelöscht werden. Deaktivierte Geräte bleiben einschließlich Historie lesbar und
werden weder geprüft noch für Erinnerungen berücksichtigt.

`ExpectedVersion` verhindert das unbemerkte Überschreiben paralleler Änderungen.
`CommandId` schützt beim Wiederholen einer Prüfanforderung vor doppelten
Prüfprotokollen. Derselbe Auftrag mit anderer Nutzlast oder anderem Benutzer
wird abgelehnt.

## Erinnerungen

`InventoryReminderWorker` läuft beim Start und danach alle sechs Stunden.
Er berücksichtigt aktive Geräte, die innerhalb der nächsten 14 Tage fällig oder
bereits überfällig sind. Jeder aktive Benutzer mit Rolle `EquipmentOfficer` und
E-Mail-Adresse erhält eine Zusammenfassung seiner bislang nicht benachrichtigten
Geräte. Administratoren ohne Gerätewart-Rolle erhalten diese E-Mail nicht.

Der Versand verwendet die vorhandene `IMailService`-/SMTP-Konfiguration. Eine
persistierte Zustellung pro Empfänger, Gerät und Fälligkeit verhindert erneute
Erinnerungen im nächsten Lauf. Fehlschläge bleiben für den nächsten Versuch
offen. Eine PostgreSQL-Advisory-Sperre koordiniert mehrere API-Instanzen.
Nach einer neuen Prüfung oder Planänderung kann ein neuer Fälligkeitstermin
wieder eine Erinnerung auslösen.

SMTP und Datenbank sind keine gemeinsame Transaktion: Bei einem Prozessabbruch
nach SMTP-Annahme, aber vor Speicherung der Zustellmarkierung kann die Nachricht
noch einmal versendet werden. Es gibt keine Exactly-once-Garantie für E-Mails.

## API

Alle Routen liegen unter `/api/inventory`:

| Methode und Route | Funktion | Rolle |
| --- | --- | --- |
| `GET /`, `GET /{id}`, `GET /barcode/{barcode}` | Übersicht, Details/Prüfhistorie, Barcodesuche | Inventar ansehen, Gerätewart oder Admin |
| `GET /categories`, `GET /types` | Kategorien und Typen/Prüfpläne | Inventar ansehen, Gerätewart oder Admin |
| `GET /{id}/barcode.svg` | Druckbares Barcodeetikett (Code 128) | Inventar ansehen, Gerätewart oder Admin |
| `POST /categories` | Kategorie anlegen | Admin |
| `POST /types`, `PUT /types/{id}` | Gerätetyp/Prüfplan speichern | Gerätewart oder Admin |
| `POST /`, `PUT /{id}` | Gerät anlegen/bearbeiten | Gerätewart oder Admin |
| `PUT /{id}/active` | Gerät deaktivieren/wieder aktivieren | Gerätewart oder Admin |
| `POST /{id}/inspections` | Prüfung dokumentieren | Gerätewart oder Admin |
| `GET /export` | Gesamtes Inventar als CSV exportieren | Inventar ansehen, Gerätewart oder Admin |
| `POST /import` | Inventar aus CSV importieren/aktualisieren | Gerätewart oder Admin |
| `GET /types/export`, `GET /categories/export` | Gerätetypen/Kategorien als JSON exportieren | Inventar ansehen, Gerätewart oder Admin |
| `POST /types/import` | Gerätetypen/Prüfpläne aus JSON importieren | Gerätewart oder Admin |
| `POST /categories/import` | Kategorien aus JSON importieren | Admin |

`GET /?includeInactive=true` zeigt auch deaktivierte Geräte. Die API überträgt
Statuswerte als `Ready`, `Defective` und `InRepair`; die Clients zeigen deutsche
Bezeichnungen an. Gemeinsame Verträge liegen in `Feuerwehr.Common/Models/Inventory`.

## Import und Export

Auf jeder der drei Web-Seiten stehen Export und für berechtigte Benutzer ein
aufklappbarer Importbereich mit Formatbeschreibung und Beispieldatei bereit.
Die API erwartet Uploads als `multipart/form-data` im Feld `file`. Höchstens
5 MB pro Datei; beim Inventar höchstens 999 Geräte, bei JSON höchstens
10.000 Einträge. Ungültige Dateien werden vollständig zurückgewiesen, ohne
teilweise neue oder ersetzte Datensätze. Die Antwort enthält `createdCount`,
`updatedCount` und `errors` mit Zeilen- bzw. Eintragsnummern.

Für einen vollständigen Datenumzug zuerst Kategorien, dann Gerätetypen und
zuletzt Geräte importieren. Die Dateien müssen nicht den gesamten Bestand
enthalten: Nicht enthaltene Datensätze werden nicht gelöscht oder deaktiviert.

### Inventar: CSV

Der Export enthält auch deaktivierte Geräte. Format: UTF-8 mit BOM,
Semikolon als Trennzeichen, CSV-Quoting und Datumswerte `yyyy-MM-dd`.
Potenzielle Excel-Formeln werden beim Export reversibel mit einem Apostroph
entschärft; der Import entfernt dieses Exportpräfix wieder.

| Spalte | Bedeutung beim Import |
| --- | --- |
| `barcode` | Pflichtfeld und Abgleichschlüssel; `KPP-001` bis `KPP-999`, in der Datei eindeutig |
| `equipmentTypeId` | UUID eines vorhandenen Gerätetyps; alternativ Typname und Kategorie angeben |
| `equipmentType`, `category` | Name des Gerätetyps und seiner Kategorie; bei unbekannter UUID dienen sie dem Abgleich, etwa nach einem Datenumzug |
| `serialNumber`, `manufacturer` | Pflichtfelder, je maximal 150 Zeichen |
| `status` | Für neue Geräte: `Einsatzklar`, `Defekt`, `In Reparatur` oder die API-Werte; Standard `Einsatzklar` |
| `isActive` | Für neue Geräte: `true`/`false`; Standard `true` |
| `createdOn` | Für neue Geräte: Anlagedatum; Standard aktuelles UTC-Datum |
| `lastInspectedOn` | Für neue Geräte: optionale bisherige letzte Prüfung; nicht vor `createdOn` und nicht in der Zukunft |
| `id`, `nextInspectionOn` | Geschützte bzw. berechnete Exportinformationen; werden nicht aus der Datei übernommen |

Beispiel mit einem bereits angelegten Typ `Strahlrohr` in `Armaturen`:

```csv
barcode;equipmentType;category;serialNumber;manufacturer
KPP-001;Strahlrohr;Armaturen;SN-001;Hersteller
```

Ein vorhandener Barcode aktualisiert **nur** Gerätetyp, Seriennummer und
Hersteller unter derselben Geräte-ID. Status, Aktivzustand, Anlagedatum,
letztes Prüfdatum, Prüfhistorie und Zustellmarkierungen bleiben erhalten.
Eine Änderung des Gerätetyps berechnet die nächste Fälligkeit aus dessen
Prüfintervall neu. Ein neuer Barcode wird unverändert übernommen und erhält
eine neue Geräte-ID. Es werden keine fiktiven Prüfprotokolle erzeugt.

Nach dem Import wird die PostgreSQL-Sequenz mindestens auf die höchste
vorhandene Barcodenummer vorgerückt und niemals zurückgesetzt. Gleichzeitige
normale Schreibzugriffe werden während des Imports über eine Tabellensperre
koordiniert. Der nächste automatisch erzeugte Barcode liegt dadurch oberhalb
der bereits vergebenen Nummern; bei `KPP-999` bleibt die automatische Vergabe
ausgeschöpft. Sequenzlücken sind weiterhin möglich.

### Kategorien und Gerätetypen: JSON

Beide Dateien enthalten eine JSON-Liste. Exporte können direkt wieder
importiert werden. Kategorien werden über eine bekannte `id`, sonst über den
normalisierten Namen abgeglichen. Neue IDs dürfen mitgegeben werden.

```json
[
  { "name": "Werkzeuge" }
]
```

Gerätetypen werden über eine bekannte `id`, sonst über Kategorie und Typname
abgeglichen. Die Kategorie muss bereits existieren; `categoryId` oder
`categoryName` identifiziert sie. Nicht eindeutige Zuordnungen sowie
widersprüchliche IDs/Namen werden abgelehnt. Die aus dem normalen Export
enthaltene `version` wird nicht als Importversion verwendet. Bestehende
Datensätze erhalten beim Aktualisieren eine neue Version.

```json
[
  {
    "name": "Strahlrohr",
    "categoryName": "Armaturen",
    "inspectionIntervalMonths": 12,
    "inspectionInstructions": "Sicht- und Funktionsprüfung nach Herstellerangaben."
  }
]
```

Geänderte Prüfpläne gelten wie bei der manuellen Bearbeitung für alle
zugeordneten Geräte. Die in alten Prüfprotokollen gespeicherten
Momentaufnahmen werden nicht verändert.

## Avalonia und Kamera

Die Seite **Inventar / Geräteprüfung** bietet Barcodeeingabe und unterstützt
USB-/Bluetooth-Scanner mit Tastaturemulation und abschließendem Enter. Android
verwendet Camera2 und ZXing.Net zum lokalen Lesen von Code 128, Code 39 oder QR;
iOS verwendet AVFoundation. Nur Werte im Format `KPP-001` bis `KPP-999` werden
übernommen. Kamerabilder werden nicht an die API übertragen.

Die Kamera benötigt die Zustimmung des Benutzers. Ablehnung oder fehlende Kamera
lassen die manuelle Erfassung weiter zu. Desktop nutzt Scanner/Tastatur, die
Mobilanwendungen bieten zusätzlich den Kamerabutton. Gerätewarte dokumentieren
die Prüfung über dieselbe API und dieselben Berechtigungen wie im Web.

Ein echter iOS-Build benötigt macOS/Xcode; der Windows-/Linux-Dummy kompiliert
den nativen iOS-Scanner nicht. Vor einer mobilen Veröffentlichung auf echten
Geräten prüfen: erstmalige Kamerafreigabe, verweigerter Zugriff, Abbruch,
App-Wechsel, Quer-/Hochformat und unscharfe/falsche Barcodes.

## Tests und Datenbankänderungen

Migration `AddEquipmentInventory` legt ausschließlich die Inventartabellen,
Barcodesequenz und drei Kategorien an. Sie wird über den bestehenden API-Start
angewandt. Die Rollenerstellung erfolgt über den bestehenden Identity-Seeder.
Es ist kein Aufruf der Design-Time-DbContext-Factory gegen eine lokale Datenbank
für den normalen Start notwendig.

Ab `src`:

```powershell
dotnet test Backend/Tests.Feuerwehr.Backend/Tests.Feuerwehr.Backend.csproj
dotnet test Backend/Tests.Feuerwehr.Backend.Integration/Tests.Feuerwehr.Backend.Integration.csproj
dotnet test App/Feuerwehr.App/Tests.Feuerwehr.App/Tests.Feuerwehr.App.csproj
```

Ab `src/frontend`: `npm test`, `npm run build`.

Die Inventar-HTTP-Tests liegen im bestehenden Integrationsprojekt. Ihre Fixture
startet eigene PostgreSQL-17-, Redis-7.4- und Mailpit-Container mit dynamischen
Ports. Der echte API-Start, Migrationen, Identity/JWT und SMTP bleiben aktiv.
Geprüft werden Rollen, Kategorie-Seeds, Prüftermine, Plan-Momentaufnahmen,
Idempotenz, konkurrierende Prüfungen, Deaktivierung, Barcodegrenze sowie
zusammengefasste E-Mails. Ergänzende Service-Tests prüfen die 14-Tage-Grenze,
Zustellfehler und Wiederholungen; Frontend-/App-Tests prüfen Benutzerführung,
Leserechte, Status/Kommentar, Scannerübergabe und die API-Nutzlast.
