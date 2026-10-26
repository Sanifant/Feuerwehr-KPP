# Lokales Debugging in Visual Studio

Die AppHost-Startprofile öffnen den Browser nicht automatisch (`launchBrowser: false`).
Nach dem Start mit F5 die Dashboard-Adresse aus der Ausgabe manuell im Browser öffnen.
Im Dashboard führt die Frontend-Ressource zur Weboberfläche.

Grund: Beim automatischen Browserstart kann Visual Studio das Schließen eines
Dateidialogs als Ende der Browsersitzung behandeln und die gesamte Debug-Sitzung
beenden. AppHost, API und Desktop können dann mit `0xffffffff` (`-1`) enden,
ohne dass die Anwendung eine Ausnahme protokollieren kann. Dieser Ablauf ist im
[Aspire-Fehlerbericht #17888](https://github.com/microsoft/aspire/issues/17888)
beschrieben; dort wird `launchBrowser: false` als Umgehung genannt.

Update-Prüfung vom 26.10.2026: Das Projekt verwendet Aspire-Integrationen 13.6.1.
Die [Release Notes für 13.6.2](https://github.com/microsoft/aspire/releases/tag/v13.6.2),
[13.6.3](https://github.com/microsoft/aspire/releases/tag/v13.6.3) und
[13.6.4](https://github.com/microsoft/aspire/releases/tag/v13.6.4) nennen keine
Korrektur dieses Dateidialog-/Debuggerfehlers. Der obige Fehlerbericht ist noch
offen. Ein Aspire-Update ist daher derzeit kein belegter Ersatz für die Umgehung.

Zur Kontrolle nach einem neuen Debug-Start in der Symbolverwaltung eine SVG-Datei
auswählen und den Dateidialog zusätzlich einmal abbrechen. In beiden Fällen soll
die Debug-Sitzung weiterlaufen. Das Speichern des Symbols erfolgt erst über den
Speichern-Button. Eine PNG-Datei soll sofort eine Formatfehlermeldung anzeigen.

Wenn Visual Studio die Sitzung weiterhin beendet, unter **Extras → Optionen**
nach **Stop debugger when browser window is closed** suchen und diese Option
deaktivieren. Bei aktivem **Enable JavaScript debugging for ASP.NET** auch diese
Option testweise deaktivieren; JavaScript kann mit den Browser-Entwicklertools
untersucht werden. Microsoft beschreibt die Kopplung zwischen Browser und
Debugger in der [Dokumentation zum Script-Debugging](https://devblogs.microsoft.com/dotnet/changes-to-script-debugging-in-visual-studio-15-7/).

Die Prozessdiagnose liegt jeweils unter `bin/Debug/net10.0/logs/process-diagnostics.log`
im AppHost- und Server-Projekt. Die Symbolverwaltung zeigt ihre Browserdiagnose
unter „Debug-Diagnose“ an.
