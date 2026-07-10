# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files for layer-cached restore
COPY ["Backend/Feuerwehr.Server/Feuerwehr.Server.csproj", "Backend/Feuerwehr.Server/"]
COPY ["Feuerwehr.Common/Feuerwehr.Common.csproj", "Feuerwehr.Common/"]
RUN dotnet restore "Backend/Feuerwehr.Server/Feuerwehr.Server.csproj"

# Copy remaining sources and publish
COPY . .
WORKDIR "/src/Backend/Feuerwehr.Server"
RUN dotnet publish "Feuerwehr.Server.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Feuerwehr.Server.dll"]
