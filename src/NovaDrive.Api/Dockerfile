# ── Stage 1: restore ─────────────────────────────────────────────────────────
# Separate restore step maximises layer-cache hits — only re-runs when .csproj
# files change, not on every source edit.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS restore
WORKDIR /src

COPY NovaDrive.Domain/NovaDrive.Domain.csproj             NovaDrive.Domain/
COPY NovaDrive.Application/NovaDrive.Application.csproj   NovaDrive.Application/
COPY NovaDrive.Infrastructure/NovaDrive.Infrastructure.csproj NovaDrive.Infrastructure/
COPY NovaDrive.GraphQL/NovaDrive.GraphQL.csproj           NovaDrive.GraphQL/
COPY NovaDrive.Api/NovaDrive.Api.csproj                   NovaDrive.Api/
RUN dotnet restore NovaDrive.Api/NovaDrive.Api.csproj \
      --locked-mode 2>/dev/null || dotnet restore NovaDrive.Api/NovaDrive.Api.csproj

# ── Stage 2: build & publish ──────────────────────────────────────────────────
FROM restore AS publish
COPY . .
WORKDIR /src/NovaDrive.Api
RUN dotnet publish -c Release -o /app/publish \
      --no-restore \
      -p:UseAppHost=false \
      -p:PublishTrimmed=false

# ── Stage 3: runtime (non-root) ───────────────────────────────────────────────
# mcr.microsoft.com/dotnet/aspnet is distroless-lite; use it for a smaller,
# harder-to-attack image. The app user (uid 1654) is created by the base image.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

# Install wget for health checks (must be done as root before USER drop)
RUN apt-get update && apt-get install -y --no-install-recommends wget \
    && rm -rf /var/lib/apt/lists/*

# Run as non-root
USER $APP_UID

WORKDIR /app
COPY --from=publish /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

ENTRYPOINT ["dotnet", "NovaDrive.Api.dll"]
