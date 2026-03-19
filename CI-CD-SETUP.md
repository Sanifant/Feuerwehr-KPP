# GitLab CI/CD Setup für Feuerwehr-Projekt (.NET 10)

## 🔧 Voraussetzungen

### 1. NuGet-Konfiguration

Die `NuGet.config` im Repository-Root enthält die notwendigen Paketquellen für .NET 10 Preview:

```xml
- nuget.org (Standard)
- dotnet10-preview (Azure DevOps Feed)
- dotnet-eng (Engineering Services)
- dotnet-public (Public Builds)
```

**Wichtig:** Diese Datei wird automatisch von der CI/CD-Pipeline verwendet.

### 2. Docker-Image

Die Pipeline verwendet `mcr.microsoft.com/dotnet/sdk:10.0`. Falls dieses Image noch nicht verfügbar ist:

**Alternative Images:**
```yaml
# Nightly Builds
image: mcr.microsoft.com/dotnet/nightly/sdk:10.0-preview

# Spezifische Preview-Version
image: mcr.microsoft.com/dotnet/sdk:10.0-preview.1
```

### 3. GitLab CI/CD Variablen

Konfigurieren Sie folgende Variablen unter **Settings → CI/CD → Variables**:

#### Erforderlich für Docker-Builds:
```
CI_REGISTRY_USER = <gitlab-username>
CI_REGISTRY_PASSWORD = <access-token>
```

#### Optional für Android App Signing:
```
ANDROID_KEYSTORE_FILE = <base64-encoded-keystore>
ANDROID_KEYSTORE_PASSWORD = <keystore-password>
ANDROID_KEY_ALIAS = <key-alias>
ANDROID_KEY_PASSWORD = <key-password>
```

#### Optional für Deployment:
```
STAGING_SERVER = <staging-server-url>
STAGING_SSH_KEY = <base64-encoded-ssh-key>
PRODUCTION_SERVER = <production-server-url>
PRODUCTION_SSH_KEY = <base64-encoded-ssh-key>
```

## 🚀 Pipeline-Stages

### 1. **Restore** (NuGet-Pakete wiederherstellen)
- Verwendet `NuGet.config` für .NET 10 Feeds
- Installiert Android & WASM Workloads
- Cached Dependencies für nachfolgende Stages

### 2. **Build** (Projekte kompilieren)
Parallele Builds für:
- ✅ Common Models Library
- ✅ Web API
- ✅ Desktop Application (MAUI)
- ✅ Mobile Core
- ✅ Mobile Android
- ✅ Mobile Browser (Blazor WASM)
- ✅ Mobile Desktop

### 3. **Test** (Unit-Tests ausführen)
- Führt alle Tests aus
- Generiert Code Coverage Reports (Cobertura)
- GitLab zeigt Coverage-Badge im MR

### 4. **Analyze** (Code-Qualität prüfen)
- **Code Quality:** `dotnet format` Analyse
- **Security Scan:** Vulnerable Dependencies Check
- **SAST:** GitLab Security Scanning
- **Secret Detection:** Credential Leaks prüfen

### 5. **Package** (Artefakte erstellen)
Nur für Branches: `main`, `master`, `develop`, `tags`

- **Web API:** Publishable Package
- **Android APK/AAB:** Play Store ready
- **Browser WASM:** Blazor Deployment Package
- **Desktop:** Executable

Artefakte: 30 Tage verfügbar

### 6. **Deploy** (Bereitstellung)

#### Staging (automatisch)
- Branch: `develop`
- URL: `https://staging.feuerwehr.example.com`

#### Production (manuell)
- Branch: `main`, `master`, `tags`
- URL: `https://feuerwehr.example.com`
- Erfordert manuelle Freigabe

## 🐛 Troubleshooting

### Problem: `Unable to find package Microsoft.NETCore.App.Runtime.Mono.linux-x64`

**Lösung 1:** NuGet.config korrekt platziert
```bash
# Im Repository-Root
ls -la NuGet.config
```

**Lösung 2:** Preview-Feeds manuell hinzufügen
```bash
dotnet nuget add source https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet10/nuget/v3/index.json -n dotnet10
```

**Lösung 3:** Docker-Image aktualisieren
```yaml
# .gitlab-ci.yml
image: mcr.microsoft.com/dotnet/nightly/sdk:10.0-preview
```

### Problem: Workload Installation schlägt fehl

**Lösung:** Feed explizit angeben
```bash
dotnet workload install android \
  --source https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet10/nuget/v3/index.json
```

### Problem: Android Build Fehler

**Lösung:** Workload-Version prüfen
```bash
dotnet workload list
dotnet workload update
```

### Problem: iOS-Projekt wird gebaut (sollte übersprungen werden)

**iOS-Builds erfordern macOS-Runner:**
```yaml
build:ios:
  tags:
    - macos
  image: macos-13-xcode-15
```

## 📊 Code Coverage Badge

Fügen Sie den Coverage-Badge zu Ihrer README hinzu:

```markdown
![Coverage](https://gitlab.opencode.de/oc000130667082/feuerwehr/badges/master/coverage.svg)
```

## 🔐 Security Scans

Die Pipeline inkludiert automatisch:
- **SAST** (Static Application Security Testing)
- **Secret Detection** (Credential Leaks)
- **Dependency Scanning** (Vulnerable Packages)

Berichte: **Security → Vulnerability Report**

## 🌐 Deployment-Strategien

### Option 1: SSH-Deployment
```yaml
deploy:staging:
  script:
    - apt-get update && apt-get install -y openssh-client
    - eval $(ssh-agent -s)
    - echo "$STAGING_SSH_KEY" | base64 -d | ssh-add -
    - scp -r $ARTIFACTS_PATH/web-api/* user@$STAGING_SERVER:/var/www/api/
    - ssh user@$STAGING_SERVER "systemctl restart api.service"
```

### Option 2: Docker Deployment
```yaml
deploy:staging:
  script:
    - docker login -u $CI_REGISTRY_USER -p $CI_REGISTRY_PASSWORD $CI_REGISTRY
    - docker pull $DOCKER_IMAGE_API:$CI_COMMIT_SHORT_SHA
    - docker run -d -p 80:8080 $DOCKER_IMAGE_API:$CI_COMMIT_SHORT_SHA
```

### Option 3: Kubernetes Deployment
```yaml
deploy:staging:
  image: bitnami/kubectl:latest
  script:
    - kubectl config use-context staging
    - kubectl set image deployment/api api=$DOCKER_IMAGE_API:$CI_COMMIT_SHORT_SHA
    - kubectl rollout status deployment/api
```

## 📝 Best Practices

### Merge Requests
- CI/CD muss erfolgreich durchlaufen ✅
- Code Coverage darf nicht sinken
- Alle Security Scans müssen grün sein

### Branching Strategy
```
main/master  → Production (Protected)
   ↑
develop      → Staging (Auto-Deploy)
   ↑
feature/*    → Feature Branches
```

### Versioning
- **main/master:** Semantic Versioning (1.0.0)
- **develop:** `1.0.0-ci-{pipeline-id}`
- **Tags:** Release-Versionen

## 🛠️ Anpassungen

### Package-Namen anpassen
```yaml
variables:
  PROJECT_WEB_API: 'Web/YourProjectName/YourProjectName.csproj'
```

### Deployment-URLs ändern
```yaml
deploy:production:
  environment:
    url: https://your-domain.com
```

### Weitere Projekte hinzufügen
```yaml
build:new-project:
  stage: build
  needs: ['restore']
  script:
    - dotnet build Path/To/NewProject.csproj --no-restore
```

## 📚 Weitere Ressourcen

- [.NET 10 Release Notes](https://github.com/dotnet/core/tree/main/release-notes/10.0)
- [GitLab CI/CD Dokumentation](https://docs.gitlab.com/ee/ci/)
- [.NET MAUI GitLab CI](https://docs.microsoft.com/en-us/dotnet/maui/deployment/)
- [Android Signing](https://developer.android.com/studio/publish/app-signing)

---

**Projekt:** Feuerwehr Multi-Platform Application  
**Target Framework:** .NET 10  
**GitLab:** https://gitlab.opencode.de/oc000130667082/feuerwehr
