# ── Stage 1: restore ─────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS restore
WORKDIR /src

COPY NovaDrive.Domain/NovaDrive.Domain.csproj   NovaDrive.Domain/
COPY NovaDrive.Simulator/NovaDrive.Simulator.csproj NovaDrive.Simulator/
RUN dotnet restore NovaDrive.Simulator/NovaDrive.Simulator.csproj \
      --locked-mode 2>/dev/null || dotnet restore NovaDrive.Simulator/NovaDrive.Simulator.csproj

# ── Stage 2: build & publish ──────────────────────────────────────────────────
FROM restore AS publish
COPY . .
WORKDIR /src/NovaDrive.Simulator
RUN dotnet publish -c Release -o /app/publish \
      --no-restore \
      -p:UseAppHost=false

# ── Stage 3: runtime (non-root) ───────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final

USER $APP_UID

WORKDIR /app
COPY --from=publish /app/publish .

ENV DOTNET_RUNNING_IN_CONTAINER=true

ENTRYPOINT ["dotnet", "NovaDrive.Simulator.dll"]
