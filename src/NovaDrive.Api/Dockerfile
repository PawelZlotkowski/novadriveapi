FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src
COPY NovaDrive.Domain/NovaDrive.Domain.csproj NovaDrive.Domain/
COPY NovaDrive.Application/NovaDrive.Application.csproj NovaDrive.Application/
COPY NovaDrive.Infrastructure/NovaDrive.Infrastructure.csproj NovaDrive.Infrastructure/
COPY NovaDrive.GraphQL/NovaDrive.GraphQL.csproj NovaDrive.GraphQL/
COPY NovaDrive.Api/NovaDrive.Api.csproj NovaDrive.Api/
RUN dotnet restore NovaDrive.Api/NovaDrive.Api.csproj

COPY . .
WORKDIR /src/NovaDrive.Api
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "NovaDrive.Api.dll"]
