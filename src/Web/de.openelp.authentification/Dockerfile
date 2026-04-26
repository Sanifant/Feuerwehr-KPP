FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Web/de.openelp.authentification/de.openelp.authentification.csproj", "Web/de.openelp.authentification/"]
RUN dotnet restore "Web/de.openelp.authentification/de.openelp.authentification.csproj"
COPY . .
WORKDIR "/src/Web/de.openelp.authentification"
RUN dotnet build "./de.openelp.authentification.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./de.openelp.authentification.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "de.openelp.authentification.dll"]
