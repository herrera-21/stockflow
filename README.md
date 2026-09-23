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
- htmx (vendored, no npm build step)
- Docker / Docker Compose
- xUnit

## Architecture

Modular monolith, single repository, layered from the start:

- `Domain` — entities and business rules (invariants)
- `Application` — use cases / application services
- `Infrastructure` — EF Core, persistence, Identity
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

## Roles

| Capability | Administrator | InventoryManager | Salesperson |
|---|---|---|---|
| View products | yes | yes | yes |
| Create / edit / deactivate products | yes | yes | no |
| Customers | yes | no | yes |
| Suppliers | yes | yes | no |
| Inventory adjustments and movements | yes | yes | no |
| Purchases | yes | yes | no |
| Sales | yes | no | yes |
| Electronic invoicing | yes | no | yes |
| Dashboard | yes | yes | yes |
| Users and roles | yes | no | no |

Products are managed through the `CanManageProducts` authorization policy, which grants access only
to Administrator and InventoryManager; Salesperson has read-only access to the catalog. Customers
and suppliers use the `CanManageCustomers` (Administrator, Salesperson) and `CanManageSuppliers`
(Administrator, InventoryManager) policies respectively.

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

## Tests

The test suite is split by layer:

- `tests/StockFlow.Domain.Tests` — domain invariants (unit).
- `tests/StockFlow.Application.Tests` — use case handlers (unit, EF Core InMemory).
- `tests/StockFlow.Web.Tests` — full HTTP flow (integration). They boot the real app and exercise
  the SQL Server from `docker-compose.yml`, isolated in their own `StockFlowDb_Test` database.

```bash
# Run every test
dotnet test

# Run a single test
dotnet test --filter "FullyQualifiedName~StockFlow.Domain.Tests.Entities.ProductTests"
```

Integration tests require the SQL Server container to be running (`docker compose up -d`).

## Project structure

```text
stockflow/
├── docker-compose.yml        # SQL Server 2022 Developer edition
├── StockFlow.slnx            # Solution file
└── src/
    ├── StockFlow.Domain/          # Entities and business rules
    ├── StockFlow.Application/     # Use cases / application services
    ├── StockFlow.Infrastructure/  # EF Core, persistence, Identity
    └── StockFlow.Web/             # Razor Pages, controllers, views
```

## Status

Phases 1 (products) and 2 (customers and suppliers) are functionally complete.

- **Products:** model, EF Core mapping and migration, CRUD (list with pagination, create, edit,
  soft-delete) with role-based access, live search/filtering by name, SKU and category, and a
  low-stock indicator.
- **Customers:** model, EF Core mapping and migration, CRUD with role-based access, live search by
  name, tax id or email, and a purchase-history placeholder (until Phase 5, Sales).
- **Suppliers:** model, EF Core mapping and migration, CRUD with role-based access, live search, and
  a many-to-many association of products to suppliers that stores the supplier's SKU, the agreed
  purchase price and a single preferred supplier per product.

Search, filtering, pagination and soft deletes run over htmx (vendored in `wwwroot/lib/htmx`, no npm
build step), without full page reloads, and are covered by unit and integration tests. Inventory,
purchases, sales and invoicing are not implemented yet.

