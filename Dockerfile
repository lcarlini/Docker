# syntax=docker/dockerfile:1

ARG DOTNET_VERSION=10.0

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS restore
WORKDIR /source
COPY ["src/Dockyard.Api/Dockyard.Api.csproj", "src/Dockyard.Api/"]
RUN dotnet restore "src/Dockyard.Api/Dockyard.Api.csproj"

FROM restore AS build
COPY . .
RUN dotnet publish "src/Dockyard.Api/Dockyard.Api.csproj" \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS runtime
WORKDIR /app
COPY --from=build --chown=$APP_UID:$APP_UID /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_EnableDiagnostics=0 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1

USER $APP_UID
EXPOSE 8080

HEALTHCHECK --interval=15s --timeout=3s --start-period=10s --retries=3 \
    CMD ["dotnet", "Dockyard.Api.dll", "--health-check"]

ENTRYPOINT ["dotnet", "Dockyard.Api.dll"]
