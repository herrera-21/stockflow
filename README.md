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
- Tom Select (vendored, searchable category selector)
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
  return) — it is never edited directly. A movement records the user, timestamp, type, reason, the
  stock before and after, and the unit; the stock and the movement are saved atomically. Stock never
  goes negative, quantities are always positive, countable units only take whole numbers, and
  inactive products accept no movements. Creating a product with opening stock records an
  initial-balance movement.
- **Concurrency:** each product carries a rowversion; two simultaneous operations cannot silently
  overwrite each other's stock. The second save is rejected with a clear, localized "reload and try
  again" message.
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
| Categories | yes | no | no |
| Inventory adjustments and movements | yes | yes | no |
| Purchases | yes | yes | no |
| Sales | yes | no | yes |
| Electronic invoicing | yes | no | yes |
| Dashboard | yes | yes | yes |
| Users and roles | yes | no | no |

Products are managed through the `CanManageProducts` authorization policy, which grants access only
to Administrator and InventoryManager; Salesperson has read-only access to the catalog. Customers
and suppliers use the `CanManageCustomers` (Administrator, Salesperson) and `CanManageSuppliers`
(Administrator, InventoryManager) policies respectively. Categories use `CanManageCategories`, which
is restricted to Administrator because they define the shared catalog. Stock adjustments use
`CanAdjustInventory` (Administrator, InventoryManager); the movement history is visible to anyone who
can see products.

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

The app listens on `http://localhost:5124` by default. On first run it seeds the three roles
(`Administrator`, `Salesperson`, `InventoryManager`) and one test account per role, all defined under
the `SeedUsers` section of `appsettings.Development.json`. Sign in with any of them to try the
role-based access:

| Role | Email | Password |
|---|---|---|
| Administrator | `admin@stockflow.local` | `Admin#2026` |
| InventoryManager | `inventory@stockflow.local` | `Inventory#2026` |
| Salesperson | `sales@stockflow.local` | `Sales#2026` |

The UI is available in Spanish (default) and English; use the language selector in the header to
switch. The app uses a sidebar navigation and a dark header. Money and percentages use the dot as the
decimal separator (for example `1234.56`). Authentication uses ASP.NET Core Identity's cookie sign-in
(HttpOnly, `SameSite=Lax`), so a page refresh keeps the session — there is no token stored in
`localStorage`/`sessionStorage`.

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

Phases 1 (products), 2 (customers and suppliers) and 3 (inventory) are functionally complete.

- **Products:** model, EF Core mapping and migration, CRUD (list with pagination, create, edit,
  soft-delete) with role-based access, live search/filtering by name, SKU and category, and a
  low-stock indicator. The SKU field explains what it is and shows an example, and the category is a
  searchable selector fed from the database.
- **Units of measure (products):** each product has a sales unit (used for stock and the sale
  price) and a purchase unit with a conversion factor, for example bought by the box of 24 and sold
  by the unit. The purchase price is per purchase unit and the cost per sales unit is derived from
  it. Stock is decimal, so bulk products can hold fractions (2.5 lb); countable units (unit, dozen,
  box, pack) only accept whole quantities, enforced in the domain, the form and the database. The
  sales unit can only change while the product has no stock.
- **Inventory:** every stock change is an `InventoryMovement` (opening balance, purchase, sale,
  sale cancellation, positive/negative adjustment, customer/supplier return) with the user,
  timestamp, reason, note, reference and the stock before and after. The product list offers
  "Adjust stock" (increase/decrease, quantity in the base unit, reason and note, with a live
  "current stock to resulting stock" preview) and "Movements" (per-product history paginated with
  htmx). Stock is never edited directly, never goes negative and only changes together with its
  movement in a single save; products created before this phase have no opening movement.
- **Categories:** their own table and CRUD (list, create, edit, deactivate/reactivate), restricted to
  Administrator. A category cannot be deactivated while products are associated with it. The seeded
  categories (cleaning, beverages, food, snacks, dairy, bakery, personal care, other) are shown with
  a localized name; user-created ones use their own name.
- **Customers:** model, EF Core mapping and migration, CRUD with role-based access, live search by
  name, document or email, and a purchase-history placeholder (until Phase 5, Sales). The name is
  validated as a person name and the document uses a type selector.
- **Suppliers:** model, EF Core mapping and migration, CRUD with role-based access, live search, and
  a many-to-many association of products to suppliers that stores the supplier's SKU, the agreed
  purchase price and a single preferred supplier per product.
- **Identity documents (customers and suppliers):** a document type (DUI, NIT, passport, other) plus
  the number, unique per type. DUI is `00000000-0`; NIT is 9 digits (homologated DUI for natural
  persons) or 14 digits `0000-000000-000-0` for legal entities; passport and other stay free. The
  number is formatted automatically as it is typed.

Search, filtering, pagination and soft deletes run over htmx (vendored in `wwwroot/lib/htmx`, no npm
build step), without full page reloads, and are covered by unit and integration tests. Purchases,
sales and invoicing are not implemented yet.

