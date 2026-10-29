# API-Übersicht

Die Web-API ist ein ASP.NET-Core-Controller-API im Projekt `src/Backend/Feuerwehr.Server/`. Die API-Verträge liegen im Projekt `src/Feuerwehr.Common/`; sie werden nicht automatisch als TypeScript-Typen in den Webclient übertragen.

Im Development-Modus werden die OpenAPI-Dokumente über `/openapi/v1.json` und die interaktive Scalar-Oberfläche unter `/scalar/v1` bereitgestellt. Beide Endpunkte werden nur in der Development-Umgebung gemappt.

Eine GitBook-API-Referenz sollte aus der zur Laufzeit generierten OpenAPI-Spezifikation erstellt werden. Erzeuge die YAML-Datei mit der Anleitung [OpenAPI für GitBook erzeugen](openapi.md), prüfe die Ausgabe und registriere die Spezifikation anschließend in GitBook. Die Eintragsform für `SUMMARY.md` ist dort dokumentiert.

Eine manuell gepflegte Liste einzelner API-Operationen wird bewusst vermieden, damit sie nicht von den Controller-Routen und DTOs abweicht.
