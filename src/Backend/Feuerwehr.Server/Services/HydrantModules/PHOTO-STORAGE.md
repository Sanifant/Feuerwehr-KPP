# Dateispeicher fuer Hydrantenpruefungen

Das Pruefungsmodell speichert fuer ein optionales JPEG nur `PhotoStorageKey` und
`PhotoContentType` (`image/jpeg`). Die Bilddatei liegt ausserhalb der Datenbank.
`PhotoStorageKey` bezeichnet eine relative Datei innerhalb des konfigurierten
Speichers, keinen oeffentlichen Download-Link oder absoluten Dateipfad.

Der Speicherpfad wird ueber `HydrantInspectionPhotos:RootPath` konfiguriert.
Als Umgebungsvariable heisst die Einstellung `HydrantInspectionPhotos__RootPath`.
Der lokale Standard ist `App_Data/hydrant-inspection-photos`, relativ zum
Content-Root der API. Im Docker-Image ist der Pfad
`/app/data/hydrant-inspection-photos` vorbereitet.

## Docker Compose

In `deployment/docker-compose.yml` wird ausschliesslich das API-Service mit dem
persistenten Volume verbunden:

```yaml
services:
  ff-kpp-int-inventory-api:
    environment:
      HydrantInspectionPhotos__RootPath: /app/data/hydrant-inspection-photos
    volumes:
      - hydrant_inspection_photos:/app/data/hydrant-inspection-photos

volumes:
  hydrant_inspection_photos:
```

Compose versieht den tatsaechlichen Volume-Namen standardmaessig mit dem
Projektnamen. Die uebrige vorhandene Service-Konfiguration bleibt erforderlich.

## Docker ohne Compose

Beispiel fuer ein eigenstaendiges benanntes Volume. `/pfad/zur/api.env` steht fuer
die vorhandene API-Konfiguration mit Datenbank-, Cache- und Authentifizierungseinstellungen:

```sh
docker volume create hydrant_inspection_photos
docker run --name feuerwehr-api \
  --env-file /pfad/zur/api.env \
  --env HydrantInspectionPhotos__RootPath=/app/data/hydrant-inspection-photos \
  --mount type=volume,source=hydrant_inspection_photos,target=/app/data/hydrant-inspection-photos \
  --publish 8080:8080 \
  ghcr.io/sanifant/feuerwehr-kpp/feuerwehr-api:integration
```

Container koennen neu erstellt werden, ohne das benannte Volume zu loeschen.
Die Fotos sind zusammen mit der Datenbank zu sichern und wiederherzustellen.
`docker compose down --volumes` entfernt auch das Foto-Volume und dessen Inhalt.
Der Speicherordner ist kein statischer Webordner; Fotoabrufe muessen ueber einen
berechtigten API-Endpunkt erfolgen.

## Stand dieser Erweiterung

Vorbereitet sind Modell, EF-Konfiguration, Migration, Speicherpfad-Optionen und
Volume-Konfiguration. Foto-Upload, Dateiablage und berechtigter Abruf sind noch
nicht implementiert.

## Avalonia-Bildaufbereitung

`IHydrantInspectionPhotoProcessor` ist im Avalonia-Client registriert. Er nimmt
einen lesbaren Kamera- oder Dateistream entgegen und gibt ein
`PreparedHydrantInspectionPhoto` mit JPEG-Bytes, Content-Type und Abmessungen zurueck.
Der Eingabestream bleibt im Besitz des Aufrufers und wird nicht geschlossen.
Die eigentliche Bildverarbeitung laeuft ausserhalb des UI-Threads.

Festgelegte Werte in `HydrantInspectionPhotoLimits` (Common):

- maximal 2048 Pixel an der langen Bildkante;
- Seitenverhaeltnis beibehalten, kleinere Bilder nicht hochskalieren;
- JPEG-Qualitaet 85;
- maximal 5 MiB (5.242.880 Bytes) nach der Umwandlung.

Die Kameradrehung und Spiegelung aus EXIF werden in die Bildpixel uebernommen;
die urspruenglichen EXIF-Metadaten werden nicht in das neue JPEG kopiert.
Transparenz wird auf Weiss abgebildet. Unterstuetzte Bildformate werden anhand
des tatsaechlichen Dateiinhalts erkannt. Ein nicht unterstuetztes oder defektes
Bild fuehrt zu `InvalidDataException`. Eine ueberschrittene Groessengrenze fuehrt
ebenfalls zu einem Fehler; die vereinbarte JPEG-Qualitaet wird nicht still geaendert.
Originale ueber 5 MiB sind zulaessig, sofern das vorbereitete JPEG die Grenze einhaelt.

Beispiel fuer den spaeteren Aufnahme-/Uploadablauf:

```csharp
PreparedHydrantInspectionPhoto photo = await photoProcessor.PrepareAsync(cameraStream, cancellationToken);
using ByteArrayContent content = new ByteArrayContent(photo.Content);
content.Headers.ContentType = new MediaTypeHeaderValue(photo.ContentType);
```

Im Avalonia-Client existiert derzeit noch keine Prueffotoaufnahme oder entsprechende
Upload-Oberflaeche. Die Verarbeitung ist als Service fuer diese Anbindung verfuegbar.
Der spaetere API-Upload muss die 5-MiB-Grenze unabhaengig vom Client ebenfalls pruefen.
