# Monitoring

Dieser Bereich enthält die Grafana-Konfiguration für lokale Observability- und Dashboard-Ansichten im Deployment.

## Struktur

- grafana/dashboards: fertige Dashboard-JSON-Dateien
- grafana/provisioning/dashboards: Automatische Dashboard-Registrierung
- grafana/provisioning/datasources: Datenquellen für Grafana

## Starten

```bash
docker compose -f deployment/docker-compose.yml up -d
```

Danach ist Grafana unter http://localhost:3000 erreichbar.
