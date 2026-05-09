# NovaDrive — Autonomous Ride-Sharing Platform (Backend)

A production-style backend for an autonomous vehicle ride-sharing platform, built with **ASP.NET Core (.NET 10)** and clean architecture principles. The system manages the full lifecycle of autonomous rides — from passenger booking and dynamic pricing through real-time vehicle telemetry to payments and invoicing.

---

## Tech Stack

| Layer | Technology |
|---|---|
| Runtime | .NET 10 / ASP.NET Core (Minimal APIs) |
| Relational DB | PostgreSQL 17 (Entity Framework Core) |
| Document DB | MongoDB 8 (telemetry & sensor data) |
| Authentication | Auth0 (JWT Bearer tokens) |
| GraphQL | HotChocolate 15 |
| Validation | FluentValidation |
| Logging | Serilog → Seq (structured logs) |
| Email | SMTP (Mailtrap-compatible) |
| Containerisation | Docker + Docker Compose |
| Testing | xUnit — unit tests & integration tests |

---

## Architecture

The solution follows **Clean Architecture** split across four projects:

```
NovaDrive.Domain          — Entities, enums, MongoDB documents
NovaDrive.Application     — Services, DTOs, validators, AutoMapper
NovaDrive.Infrastructure  — EF Core, MongoDB contexts, repositories, external services
NovaDrive.Api             — Minimal API endpoints, middleware, GraphQL, DI wiring
```

Additional projects:

- **NovaDrive.Simulator** — simulates live telemetry from autonomous vehicles
- **NovaDrive.UnitTests** — unit tests for all services (pricing, rides, discounts, etc.)
- **NovaDrive.IntegrationTests** — end-to-end endpoint tests against a real database

---

## Key Features

### Passenger-facing API
- Register / manage passenger profiles
- Browse available vehicles
- Book rides with real-time **dynamic pricing** (distance, surge, discount codes)
- Track ride status and history
- Payments and invoice download
- Submit support tickets

### Admin API
- Full CRUD for users, passengers, vehicles
- Ride and payment oversight
- Vehicle **maintenance log** management
- Discount code creation and management
- Support ticket resolution

### Vehicle / Telemetry API
- Secured with API-key middleware (separate from JWT)
- Ingest real-time GPS, speed, and sensor diagnostics into MongoDB
- Admin dashboard queries for live and historical telemetry

### Cross-cutting concerns
- **Auth0 JWT** authentication with role-based authorization policies (`admin` / `passenger`)
- **FluentValidation** on all incoming request bodies via an endpoint filter
- **Serilog** structured logging with Seq sink
- **CORS** configured for separate admin and passenger frontends

---

## Running Locally

### Prerequisites
- Docker Desktop
- .NET 10 SDK (for running tests or local development without Docker)

### Start everything with Docker Compose

```bash
docker compose up --build
```

This starts: PostgreSQL, MongoDB, Seq, the API, the vehicle simulator, and both frontend apps.

| Service | URL |
|---|---|
| REST API | http://localhost:5000 |
| Swagger UI | http://localhost:5000/swagger |
| GraphQL playground | http://localhost:5000/graphql |
| Seq log viewer | http://localhost:5341 |
| Admin frontend | http://localhost:3000 |
| Passenger frontend | http://localhost:3001 |

### Configuration

Copy `.env.example` to `.env` and fill in your Auth0, SMTP, and database credentials before first run.

---

## Running Tests

```bash
# Unit tests
dotnet test tests/NovaDrive.UnitTests

# Integration tests (requires running database)
dotnet test tests/NovaDrive.IntegrationTests
```

---

## Project Structure

```
src/
  NovaDrive.Api/            — entry point, endpoints, middleware
  NovaDrive.Application/    — business logic, services, DTOs
  NovaDrive.Domain/         — domain models and enums
  NovaDrive.Infrastructure/ — database, repositories, external integrations
  NovaDrive.Simulator/      — autonomous vehicle telemetry simulator
tests/
  NovaDrive.UnitTests/
  NovaDrive.IntegrationTests/
frontend/
  client/                   — React + Vite (admin dashboard & passenger app)
```

---

## Author

**Pawel Zlotkowski** — [github.com/PawelZlotkowski](https://github.com/PawelZlotkowski)
