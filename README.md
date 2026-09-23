# stockflow

**[English](README.md) | [Español](README.es.md)**

stockflow is a mini-ERP: a commercial management system for a small business, modeling inventory,
purchases, sales and simulated electronic invoicing.

## Stack

- ASP.NET Core (Razor Pages)
- Entity Framework Core
- SQL Server (via Docker, `mcr.microsoft.com/mssql/server`, Developer edition)
- ASP.NET Core Identity
- Bootstrap
- Docker / Docker Compose
- xUnit

## Architecture

Modular monolith, single repository, layered from the start:

- `Domain` — entities and business rules (invariants)
- `Application` — use cases / application services
- `Infrastructure` — EF Core, repositories, Identity
- `Web` — Razor Pages, controllers, views

## Business rules

- **Inventory:** stock can only change through inventory movements (purchase, sale, adjustment,
  return) — it is never edited directly.
- **Sales:** a confirmed sale cannot be modified directly; canceling it reverts the inventory.
- **Returns:** the returned quantity cannot exceed the originally sold quantity minus previous
  returns.
- **Electronic documents:** an issued invoice cannot be edited; it must be canceled and a new
  document generated.
- **Auditing:** critical operations record the user, timestamp, affected entity and relevant
  values.

## Requirements

- [.NET SDK 10](https://dotnet.microsoft.com/download)
- [Docker](https://docs.docker.com/get-docker/) with Docker Compose, for SQL Server
- [EF Core CLI tools](https://learn.microsoft.com/ef/core/cli/dotnet)
  (`dotnet tool install --global dotnet-ef`), only needed to add or apply migrations

## Build and run

No manual configuration is required: the SQL Server settings live in `docker-compose.yml` and are
picked up automatically. SQL Server listens on host port `14330` (not the default `1433`), to avoid
clashing with a local SQL Server instance that might already be using it on your machine.

```bash
# 1. Start SQL Server (Docker)
docker compose up -d

# 2. Restore and build the solution
dotnet restore
dotnet build

# 3. Apply database migrations (creates the database, the Identity tables and the
#    __EFMigrationsHistory table)
dotnet ef database update --project src/StockFlow.Infrastructure --startup-project src/StockFlow.Web

# 4. Run the web app
dotnet run --project src/StockFlow.Web
```

The app listens on `http://localhost:5124` by default. On first run it seeds three roles
(`Administrator`, `Salesperson`, `InventoryManager`) and a seed admin account, both defined in
`appsettings.Development.json`:

- Email: `admin@stockflow.local`
- Password: `Admin#2026`

The UI is available in Spanish (default) and English; use the language selector in the navbar to
switch. Authentication uses ASP.NET Core Identity's cookie sign-in (HttpOnly, `SameSite=Lax`), so a
page refresh keeps the session — there is no token stored in `localStorage`/`sessionStorage`.

## Project structure

```text
stockflow/
├── docker-compose.yml        # SQL Server 2022 Developer edition
├── StockFlow.slnx            # Solution file
└── src/
    ├── StockFlow.Domain/          # Entities and business rules
    ├── StockFlow.Application/     # Use cases / application services
    ├── StockFlow.Infrastructure/  # EF Core, repositories, Identity
    └── StockFlow.Web/             # Razor Pages, controllers, views
```

## Status

Project scaffolding complete: solution structure, Docker, EF Core, and ASP.NET Core Identity (roles,
a seed admin account, cookie-based login/logout, Spanish/English UI). Features (inventory,
purchases, sales, invoicing) have not been implemented yet.

