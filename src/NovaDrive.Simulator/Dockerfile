FROM mcr.microsoft.com/dotnet/runtime:10.0-preview AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src
COPY NovaDrive.Domain/NovaDrive.Domain.csproj NovaDrive.Domain/
COPY NovaDrive.Simulator/NovaDrive.Simulator.csproj NovaDrive.Simulator/
RUN dotnet restore NovaDrive.Simulator/NovaDrive.Simulator.csproj

COPY . .
WORKDIR /src/NovaDrive.Simulator
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "NovaDrive.Simulator.dll"]
