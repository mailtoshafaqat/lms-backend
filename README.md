# Backend (.NET 10 API)

ASP.NET Core modular monolith for the white-label LMS.

## Layout

```
backend/
├── Lms.slnx
├── docker-compose.yml      # SQL Server for local dev
├── src/
│   ├── Lms.Api/            # HTTP host, middleware, startup
│   ├── Lms.Shared/         # Shared kernel (tenancy, auth helpers)
│   └── Modules/            # Feature modules (Identity, Platform, Content, …)
└── tests/
    └── Lms.Tests/
```

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Docker (for SQL Server)

## Quick start

```bash
# 1. Start SQL Server (from this folder)
docker compose up -d

# 2. Run API (applies EF migrations on startup)
dotnet run --project src/Lms.Api
```

API: `http://localhost:5237`

Swagger: `http://localhost:5237/swagger`

**Postman:** import `postman/openapi.json` + `postman/White-Label-LMS.local.postman_environment.json` — see [postman/README.md](./postman/README.md).

Connection string is in `src/Lms.Api/appsettings.json` (port `14330`, password `Password123!`).

## Dev logins

| Role | Email | Password |
|------|-------|----------|
| SuperAdmin | `superadmin@platform.com` | `SuperAdmin123!` |
| Institute admin (demo) | `admin@demo.com` | `Admin123!` |

## Tests

```bash
dotnet test
```
