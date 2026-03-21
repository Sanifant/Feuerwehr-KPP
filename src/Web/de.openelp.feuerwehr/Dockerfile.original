# GitLab CI/CD optimized Dockerfile for .NET 10 Web API
# Multi-stage build for optimal image size

# ============================
# Base Runtime Stage
# ============================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# ============================
# Build Stage
# ============================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy NuGet.config for .NET 10 preview feeds
COPY ["../../NuGet.config", "./"]

# Copy project files for restore (leverage Docker layer caching)
COPY ["Common/de.openelp.feuerwehr.models/de.openelp.feuerwehr.models.csproj", "Common/de.openelp.feuerwehr.models/"]
COPY ["Web/de.openelp.feuerwehr/de.openelp.feuerwehr.Api.csproj", "Web/de.openelp.feuerwehr/"]

# Restore dependencies
RUN dotnet restore "Web/de.openelp.feuerwehr/de.openelp.feuerwehr.Api.csproj" --configfile ./NuGet.config

# Copy remaining source files
COPY ["Common/", "Common/"]
COPY ["Web/", "Web/"]

# Build the project
WORKDIR "/src/Web/de.openelp.feuerwehr"
RUN dotnet build "de.openelp.feuerwehr.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build --no-restore

# ============================
# Publish Stage
# ============================
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "de.openelp.feuerwehr.Api.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    --no-restore \
    --no-build \
    /p:UseAppHost=false

# ============================
# Final Runtime Stage
# ============================
FROM base AS final
WORKDIR /app

# Copy published application
COPY --from=publish /app/publish .

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Run the application
ENTRYPOINT ["dotnet", "de.openelp.feuerwehr.Api.dll"]