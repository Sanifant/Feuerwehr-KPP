# Modulparameter definieren

Parameter werden ausschliesslich durch Attribute im Backend definiert. Die Anwendung
enthaelt bewusst noch keine konkreten Modul- oder Parameterdefinitionen.

Eine Definitionsklasse liegt im jeweiligen Modul. Beispiel (nicht vorregistriert):

```csharp
namespace de.openelp.feuerwehr.Server.Services.ExampleModules
{
    using de.openelp.feuerwehr.Server.Services.Settings;
    using de.openelp.feuerwehr.Server.Services.Settings.Validators;

    [SettingsModule("example", "Beispiel",
        ApiPaths = new[] { "/api/example" },
        PagePaths = new[] { "/example" })]
    [SettingDefinition("EXAMPLE_NOTIFICATION_EMAIL", "Benachrichtigung",
        typeof(EmailSettingValidator), Mandatory = true)]
    public sealed class ExampleSettingsDefinitions
    {
    }
}
```

Alle API- und Seitenpfade des Moduls muessen in `ApiPaths` bzw. `PagePaths` stehen.
Unterpfade sind eingeschlossen; aehnlich beginnende Pfadnamen sind es nicht.
Die Definitionsklasse muss in der Server-Assembly liegen; eine DI-Registrierung
dieser Klasse ist nicht erforderlich. Validator-Klassen werden automatisch als
scoped Services registriert und koennen selbst DI-Abhaengigkeiten verwenden.

Der Schluessel ist unveraenderlich, eindeutig und gleichzeitig der exakte Name der
Umgebungsvariable. Er besteht aus Buchstaben, Ziffern und Unterstrichen, beginnt
nicht mit einer Ziffer und hat maximal 200 Zeichen. Die Datenbank speichert ihn
im bestehenden Feld `Setting.Name`. Anzeigename und Eingabetyp stammen aus der
Definition bzw. der Validator-Klasse. Bestehende Zeilen ohne Definition werden
ignoriert. Eine Schemaaenderung ist fuer diese Erweiterung nicht erforderlich.

## Validierung

Jede Definition referenziert eine konkrete Klasse mit `ISettingValidator`.
`Validate` liefert `null` fuer einen gueltigen Wert, andernfalls eine Fehlermeldung.
Die Klasse verantwortet saemtliche Wertregeln, auch Leerwerte. `Type` beschreibt
nur die Eingabedarstellung und waehlt keine weitere Validierung aus.
Vorhanden sind `EmailSettingValidator`, `TextSettingValidator` und
`PositiveIntegerSettingValidator`. Dieselbe Klasse prueft API-Eingaben,
Umgebungsvariablen und Werte fuer die Modulverfuegbarkeit.

## Start und Vorrang der Datenbank

Nach Migrationen und Seed-Daten werden die deklarierten Umgebungsvariablen einmal
gelesen. Existiert ein Datenbankwert, wird er weder geaendert noch durch einen
Umgebungswert ersetzt. Fehlende, gueltige Werte werden mit UTC-Zeitpunkt und einer
Referenz auf den gesperrten technischen Benutzer mit Anzeigename `Installer`
gespeichert. Dieser Benutzer hat kein Passwort und keine Rollen. Fehlt die Variable
oder ist ihr Wert ungueltig, entsteht kein Eintrag; die Anwendung startet weiter.
Ungueltige Werte werden nicht protokolliert.

Loeschen entfernt nur den Datenbankwert. Der Parameter ist wieder im Dropdown
auswaehlbar. Erst beim naechsten Start kann die Umgebungsvariable ihn erneut anlegen.

## Module und interner Zugriff

Fehlt ein gueltiger Wert fuer einen `Mandatory`-Parameter, werden alle deklarierten
API-Pfade dieses Moduls mit HTTP 503 gesperrt. Die Web-Seiten bleiben erreichbar,
zeigen aber einen Konfigurationshinweis anstelle des Modulinhalts. Settings bleiben
fuer Administratoren erreichbar. Speichern und Loeschen wirken bei der naechsten
Verfuegbarkeitspruefung ohne Neustart; Umgebungsvariablen werden dabei nicht gelesen.

Modulservices koennen `ISettingsRuntime.GetValueAsync` fuer validierte Werte nutzen.
Fehlende oder ungueltige Werte liefern `null`; unbekannte Schluessel sind ein
Programmierfehler. Hintergrundaufgaben muessen den Modulstatus ueber
`GetModuleStatusesAsync` pruefen, bevor sie gesperrte Modulfunktionen ausfuehren;
die HTTP-Middleware schuetzt ausschliesslich HTTP-Zugriffe.

`GET /api/Settings/definitions` liefert den Admin-Katalog.
`GET /api/Settings/modules` liefert angemeldeten Benutzern nur Modulmetadaten und
fehlende Parameternamen, niemals gespeicherte Werte. Die uebrige Settings-API ist
ausschliesslich fuer Administratoren zugelassen. Beim Anlegen wird nur
`{ "key": "EXAMPLE_NOTIFICATION_EMAIL", "value": "name@example.org" }` gesendet.
