# GlobalKinetic Assessment Solution

> **Stack:** .NET 8 · ASP.NET Core Web API · Entity Framework Core 8 · SQL Server · Redis · Docker · Azure IaC

---

## What This Project Does

This is a Customer Management REST API built for the Global Kinetic technical assessment. It implements full CRUD operations for a `Customer` entity with the following functionalities:

- **Create, Read, Update, Delete** customers via a versioned REST API
- **PII encryption at rest** — `FirstName`, `LastName`, and `Email` are encrypted with AES-256-CBC before being stored in SQL Server, and decrypted transparently on read using EF Core Value Converters
- **Redis distributed caching** — all read operations are cached for 5 minutes using a Decorator pattern over the repository, with automatic in-memory fallback when Redis is unavailable
- **Basic Authentication** — all API endpoints are protected with HTTP Basic Auth; Swagger UI and the health endpoint are publicly accessible
- **Paged and filtered queries** — get all customers with optional `firstName` filter and pagination
- **Structured logging** — Serilog writes to console and rolling log files
- **Health monitoring** — `/health` endpoint with EF Core database probe
- **Containerised** — full Docker Compose stack including SQL Server 2022 and Redis 7
- **CI/CD pipeline** — GitHub Actions runs unit and integration tests on every push, then verifies the Docker build

---

## Solution Structure

```
GK.GlobalKineticAssessment/
├── src/
│   ├── GK.GlobalKineticAssessment.Domain          # Entities, interfaces, domain exceptions
│   ├── GK.GlobalKineticAssessment.Application     # DTOs (C# records), service, FluentValidation
│   ├── GK.GlobalKineticAssessment.Infrastructure  # EF Core, repositories, AES encryption, Redis
│   └── GK.GlobalKineticAssessment.API             # Controllers, middleware, Swagger, Program.cs
├── tests/
│   └── GK.GlobalKineticAssessment.Tests           # xUnit unit + integration tests
├── infra/azure/                                   # Bicep IaC (cloud deployment reference)
├── Dockerfile                                     # Multi-stage build
├── docker-compose.yml                             # API + SQL Server 2022 + Redis 7
└── README.md
```

---

## Quick Start — Docker (Recommended)

> **Requirement:** Docker Desktop with at least 4 GB RAM allocated.

```bash
git clone https://github.com/Sandisiwe-Vutula/GK.GlobalKineticAssessment.git
cd GK.GlobalKineticAssessment
docker compose up --build
```

Wait approximately 60 seconds for SQL Server to initialise, then open:

| URL | Description |
|-----|-------------|
| http://localhost:8080/swagger | Swagger UI |
| http://localhost:8080/health  | Health check |

**Credentials:** Username `gkadmin` · Password `GK@Assessment2026!`

### Docker Commands

```bash
docker compose up --build -d       # run in background
docker compose logs -f api         # tail API logs
docker compose down                # stop (keep data)
docker compose down -v             # stop and wipe all data
```

---

## Running Locally Without Docker

### Prerequisites
- .NET 8 SDK — https://dotnet.microsoft.com/download/dotnet/8.0
- SQL Server (LocalDB, Developer, or Express)
- Redis (optional — app falls back to in-memory cache automatically)

### Connection String

The project is pre-configured for LocalDB. `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=GKAssessment;Integrated Security=True;Encrypt=False;TrustServerCertificate=False;",
    "Redis": ""
  }
}
```

Leave `Redis` empty (`""`) to use in-memory cache instead.

### Run

```bash
dotnet restore
dotnet run --project src/GK.GlobalKineticAssessment.API
```

EF Core migrations are applied automatically on startup.

To apply manually:
```bash
dotnet tool install --global dotnet-ef

dotnet ef database update \
  --project src/GK.GlobalKineticAssessment.Infrastructure \
  --startup-project src/GK.GlobalKineticAssessment.API
```

Open Swagger: `http://localhost:5000/swagger`

---

## Running Tests

```bash
# All tests
dotnet test

# Unit tests only (no external services needed)
dotnet test --filter "Category=Unit"

# Integration tests only (uses EF InMemory — no SQL Server or Redis needed)
dotnet test --filter "Category=Integration"
```

---

## API Endpoints

All endpoints require Basic Authentication except `/swagger` and `/health`.

| Method | Path | Description | Success | Errors |
|--------|------|-------------|---------|--------|
| `POST` | `/api/v1/customers` | Create a customer | 201 | 409, 422 |
| `GET` | `/api/v1/customers/{id}` | Get customer by Id | 200 | 404 |
| `GET` | `/api/v1/customers` | Get all (paged + filter) | 200 | 422 |
| `PUT` | `/api/v1/customers/{id}` | Update a customer | 200 | 404, 409, 422 |
| `DELETE` | `/api/v1/customers/{id}` | Delete a customer | 204 | 404 |
| `GET` | `/health` | Health + DB probe | 200 | — |

### Paging and Filtering

```
GET /api/v1/customers?firstName=Sandisiwe&pageNumber=1&pageSize=10
```

### Sample Request — Create Customer

```json
{
  "firstName": "Sandisiwe",
  "lastName": "Vutula",
  "email": "sandisiwevutula28@gmail.com",
  "age": 35
}
```

### Sample Response

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "firstName": "Sandisiwe",
  "lastName": "Vutula",
  "email": "sandisiwevutula28@gmail.com",
  "age": 35,
  "createdAt": "2026-05-22T13:00:00Z",
  "updatedAt": null
}
```

---

## Authentication

HTTP Basic Auth (RFC 7617). Credentials are configured via environment variables.

| Environment | Username | Password |
|---|---|---|
| Local / Docker | `gkadmin` | `GK@Assessment2026!` |

**In Swagger UI:** Click **Authorize 🔒** → enter credentials → **Authorize** → **Close**.

Public paths (no auth): `/swagger/**`, `/health`

---

## Architecture & Design Decisions

### Generic Repository with Type Constraints

```csharp
public abstract class RepositoryBase<T, TKey> : IRepository<T, TKey>
    where T    : class   // must be an EF Core entity
    where TKey : struct  // primary key cannot be null
```

`CustomerRepository` inherits `RepositoryBase<Customer, Guid>` and adds customer-specific queries. The `struct` constraint on `TKey` eliminates an entire class of null-identity bugs at compile time.

### Decorator Pattern — Transparent Caching

```
ICustomerRepository
  └── CachingCustomerRepository  (Redis, 5-min TTL, in-memory fallback)
        └── CustomerRepository   (EF Core — actual data access)
```

The service layer only knows about `ICustomerRepository`. Whether Redis is available or not is completely invisible to business logic. Cache operations are wrapped in try/catch so a Redis outage never causes an API failure.

### PII Encryption via EF Value Converters

`FirstName`, `LastName`, and `Email` are encrypted with AES-256-CBC before every write and decrypted after every read. This is applied as an EF Core `ValueConverter<string, string>` on each column in `CustomerConfiguration` — zero changes needed in business logic.

### Immutable Record DTOs (C# 9+)

All DTOs are `record` types — immutable by default, val and concise syntax. This prevents accidental mutation in service code and makes the intent clear.

### Dependency Injection Composition Root

All service and repository registrations flow through two extension methods:

```csharp
builder.Services.AddApplication();          // validators + service
builder.Services.AddInfrastructure(config); // encryption + EF + cache + repositories
```

`Program.cs` stays clean and each layer owns its own registrations.

---

## Environment Variables

| Variable | Default | Description |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | LocalDB / SQL in compose | SQL Server connection string |
| `ConnectionStrings__Redis` | `redis:6379` | Redis. Empty = in-memory fallback |
| `Encryption__Key` | See appsettings.json | AES-256 key (base64, 32 bytes) |
| `Encryption__IV` | See appsettings.json | AES-256 IV (base64, 16 bytes) |
| `BasicAuth__Username` | `gkadmin` | API username |
| `BasicAuth__Password` | `GK@Assessment2026!` | API password |
| `ASPNETCORE_ENVIRONMENT` | `Production` | `Development` / `Production` / `Testing` |

---

## Database Migrations

Migrations are applied automatically on startup. To manage migrations manually:

```bash
# Install EF tools (once)
dotnet tool install --global dotnet-ef

# Add a new migration after model changes
dotnet ef migrations add <MigrationName> \
  --project src/GK.GlobalKineticAssessment.Infrastructure \
  --startup-project src/GK.GlobalKineticAssessment.API \
  --output-dir Persistence/Migrations

# Apply manually
dotnet ef database update \
  --project src/GK.GlobalKineticAssessment.Infrastructure \
  --startup-project src/GK.GlobalKineticAssessment.API
```

---

## Azure Cloud Deployment

> The `infra/azure/main.bicep` file defines the full infrastructure as code.

### Resources Provisioned

| Resource | Purpose |
|---|---|
| Azure Container Apps | Hosts the API — auto-scales 1 to 5 replicas |
| Azure Container Registry | Stores Docker images |
| Azure SQL Database (Basic) | Persistent customer storage |
| Azure Cache for Redis (C0) | Distributed caching |
| Azure Key Vault | Secrets — encryption keys, DB password, API credentials |
| Log Analytics Workspace | Centralised structured logging |

### Deploy

```bash
az login
az group create --name rg-gkassessment --location southafricanorth

az deployment group create \
  --resource-group rg-gkassessment \
  --template-file infra/azure/main.bicep \
  --parameters infra/azure/parameters.json \
  --parameters sqlAdminPassword="<STRONG_PASSWORD>" \
               basicAuthPassword="GK@Assessment2026!"

# Build and push image
az acr build --registry gkassessmentacr --image gk-assessment:latest .
```

---

## CI/CD Pipeline

GitHub Actions runs on every push and pull request to `main`.

```
push / PR
    │
    ▼
build-and-test
    ├── dotnet restore
    ├── dotnet build (Release)
    ├── Unit tests   (Category=Unit)    
    └── Integration tests (Category=Integration) 
    │
    ▼ on success
docker-verify
    └── docker build --target runtime    — confirms image builds cleanly
```

---