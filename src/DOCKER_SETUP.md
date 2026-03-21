# 🐳 Docker Setup für Feuerwehr Inventarverwaltung

## PostgreSQL mit Docker Compose

### 🚀 Schnellstart

```powershell
# PostgreSQL Container starten
docker-compose up -d

# Status prüfen
docker-compose ps

# Logs anzeigen
docker-compose logs -f postgres
```

### 📦 Was wird gestartet?

#### 1. **PostgreSQL Datenbank**
- **Image**: postgres:17-alpine
- **Port**: 5432 → localhost:5432
- **Database**: feuerwehr_inventory
- **User**: feuerwehr_user
- **Password**: feuerwehr_pass
- **Persistent Storage**: Docker Volume `postgres_data`

#### 2. **pgAdmin (Management Tool)**
- **URL**: http://localhost:5050
- **Email**: admin@feuerwehr.local
- **Password**: admin
- **Persistent Storage**: Docker Volume `pgadmin_data`

---

## 🔧 Konfiguration

### Connection String

Die Connection String in `appsettings.json` ist bereits konfiguriert:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=feuerwehr_inventory;Username=feuerwehr_user;Password=feuerwehr_pass;Include Error Detail=true"
  }
}
```

### Sicherheit für Produktion

⚠️ **Wichtig**: Die Credentials in `docker-compose.yml` sind nur für **lokale Entwicklung**!

Für **Produktion**:
1. Verwenden Sie starke Passwörter
2. Speichern Sie Secrets in `.env` Datei (nicht in Git!)
3. Nutzen Sie Docker Secrets oder Azure Key Vault

**Beispiel .env Datei** (nicht in Git committen!):

```env
POSTGRES_DB=feuerwehr_inventory
POSTGRES_USER=feuerwehr_user
POSTGRES_PASSWORD=IHR_SICHERES_PASSWORT_HIER
```

Dann in `docker-compose.yml`:

```yaml
environment:
  POSTGRES_DB: ${POSTGRES_DB}
  POSTGRES_USER: ${POSTGRES_USER}
  POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
```

---

## 🗄️ Datenbank-Setup

### 1. Docker Container starten

```powershell
docker-compose up -d
```

### 2. EF Core Migration erstellen

```powershell
# EF Core Tools installieren (einmalig)
dotnet tool install --global dotnet-ef

# Migration erstellen
dotnet ef migrations add InitialCreate --project Common\de.openelp.feuerwehr.infrastructure\de.openelp.feuerwehr.infrastructure.csproj --startup-project Web\de.openelp.feuerwehr\de.openelp.feuerwehr.Api.csproj

# Datenbank Schema erstellen
dotnet ef database update --project Common\de.openelp.feuerwehr.infrastructure\de.openelp.feuerwehr.infrastructure.csproj --startup-project Web\de.openelp.feuerwehr\de.openelp.feuerwehr.Api.csproj
```

### 3. API starten

```powershell
dotnet run --project Web\de.openelp.feuerwehr\de.openelp.feuerwehr.Api.csproj
```

---

## 🔍 Datenbank-Verwaltung mit pgAdmin

### 1. pgAdmin öffnen

Browser: http://localhost:5050

### 2. Login

- **Email**: admin@feuerwehr.local
- **Password**: admin

### 3. Server hinzufügen

1. Rechtsklick auf "Servers" → Create → Server
2. **General Tab:**
   - Name: `Feuerwehr Local`
3. **Connection Tab:**
   - Host: `postgres` (Container-Name) oder `host.docker.internal`
   - Port: `5432`
   - Database: `feuerwehr_inventory`
   - Username: `feuerwehr_user`
   - Password: `feuerwehr_pass`

### 4. Datenbank erkunden

→ Servers → Feuerwehr Local → Databases → feuerwehr_inventory → Schemas → public → Tables → InventoryItems

---

## 🛠️ Docker Befehle

### Container-Verwaltung

```powershell
# Container starten
docker-compose up -d

# Container stoppen
docker-compose stop

# Container stoppen und löschen
docker-compose down

# Container + Volumes löschen (ACHTUNG: Daten werden gelöscht!)
docker-compose down -v

# Container neu erstellen
docker-compose up -d --build

# Logs anzeigen
docker-compose logs -f

# Nur PostgreSQL Logs
docker-compose logs -f postgres

# Status prüfen
docker-compose ps
```

### Datenbank-Befehle

```powershell
# In PostgreSQL Container einsteigen
docker exec -it feuerwehr_postgres psql -U feuerwehr_user -d feuerwehr_inventory

# Datenbank-Backup erstellen
docker exec -t feuerwehr_postgres pg_dump -U feuerwehr_user feuerwehr_inventory > backup.sql

# Datenbank-Backup wiederherstellen
docker exec -i feuerwehr_postgres psql -U feuerwehr_user -d feuerwehr_inventory < backup.sql

# Alle Tabellen anzeigen
docker exec -it feuerwehr_postgres psql -U feuerwehr_user -d feuerwehr_inventory -c "\dt"

# SQL-Abfrage ausführen
docker exec -it feuerwehr_postgres psql -U feuerwehr_user -d feuerwehr_inventory -c "SELECT * FROM \"InventoryItems\";"
```

---

## 🧪 Test-Daten einfügen

### Über API (empfohlen)

POST `http://localhost:<port>/api/inventory`

```json
{
  "name": "Atemschutzgerät MSA G1",
  "category": 1,
  "description": "Pressluftatmer mit Vollmaske",
  "manufacturer": "MSA Safety",
  "model": "G1",
  "serialNumber": "MSA-2024-001",
  "inventoryNumber": "AS-001",
  "location": "Gerätehaus 1 - Atemschutzwerkstatt",
  "purchaseDate": "2024-01-15T00:00:00Z",
  "purchasePrice": 2500.00,
  "inspectionIntervalMonths": 12,
  "nextInspectionDate": "2025-01-15T00:00:00Z"
}
```

### Direkt in PostgreSQL

```sql
INSERT INTO "InventoryItems" 
  ("Id", "Name", "Category", "Status", "Condition", "CreatedAt", "Location", "InspectionIntervalMonths")
VALUES
  (gen_random_uuid(), 'Atemschutzgerät Dräger PSS 7000', 1, 0, 0, NOW(), 'Gerätehaus 1', 12),
  (gen_random_uuid(), 'Feuerwehrhelm Rosenbauer HEROS-titan', 0, 0, 0, NOW(), 'Fahrzeug HLF1', NULL),
  (gen_random_uuid(), 'Strahlrohr Storz C', 3, 0, 0, NOW(), 'Fahrzeug TLF1', NULL);
```

---

## 🔍 Troubleshooting

### "Could not connect to server"

```powershell
# PostgreSQL Status prüfen
docker-compose ps

# PostgreSQL neu starten
docker-compose restart postgres

# Logs prüfen
docker-compose logs postgres
```

### "Port already in use"

PostgreSQL läuft bereits lokal? Ändern Sie den Port in `docker-compose.yml`:

```yaml
ports:
  - "5433:5432"  # Host:Container
```

Dann Connection String anpassen:

```json
"DefaultConnection": "Host=localhost;Port=5433;Database=..."
```

### "Permission denied"

Windows: Docker Desktop muss laufen!

```powershell
# Docker Status prüfen
docker info
```

### Migration-Fehler "relation does not exist"

```powershell
# Alte Migrations löschen (falls vorhanden)
Remove-Item -Recurse Common\de.openelp.feuerwehr.infrastructure\Migrations

# Neue Migration erstellen
dotnet ef migrations add InitialCreate --project Common\de.openelp.feuerwehr.infrastructure\de.openelp.feuerwehr.infrastructure.csproj --startup-project Web\de.openelp.feuerwehr\de.openelp.feuerwehr.Api.csproj
```

---

## 🎯 Produktions-Setup (Docker)

### Dockerfile für .NET API

Erstellen Sie `Web/de.openelp.feuerwehr/Dockerfile`:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Web/de.openelp.feuerwehr/de.openelp.feuerwehr.Api.csproj", "Web/de.openelp.feuerwehr/"]
COPY ["Common/de.openelp.feuerwehr.infrastructure/de.openelp.feuerwehr.infrastructure.csproj", "Common/de.openelp.feuerwehr.infrastructure/"]
COPY ["Common/de.openelp.feuerwehr.models/de.openelp.feuerwehr.domain.csproj", "Common/de.openelp.feuerwehr.models/"]
COPY ["de.openelp.feuerwehr.application/de.openelp.feuerwehr.application.csproj", "de.openelp.feuerwehr.application/"]
RUN dotnet restore "Web/de.openelp.feuerwehr/de.openelp.feuerwehr.Api.csproj"
COPY . .
WORKDIR "/src/Web/de.openelp.feuerwehr"
RUN dotnet build "de.openelp.feuerwehr.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "de.openelp.feuerwehr.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "de.openelp.feuerwehr.Api.dll"]
```

### docker-compose mit .NET API

```yaml
services:
  api:
    build:
      context: .
      dockerfile: Web/de.openelp.feuerwehr/Dockerfile
    container_name: feuerwehr_api
    ports:
      - "8080:80"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=feuerwehr_inventory;Username=feuerwehr_user;Password=feuerwehr_pass
    depends_on:
      postgres:
        condition: service_healthy
    networks:
      - feuerwehr_network
    restart: unless-stopped
```

---

## 📚 Weitere Ressourcen

- **PostgreSQL Docs**: https://www.postgresql.org/docs/
- **Npgsql EF Core**: https://www.npgsql.org/efcore/
- **Docker Compose Docs**: https://docs.docker.com/compose/

---

**Viel Erfolg mit PostgreSQL! 🚒🐘**
