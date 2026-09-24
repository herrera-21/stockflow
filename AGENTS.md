# AGENTS.md

Guidance for AI coding agents working in this repository. Keep this file short and repo-specific.
If it conflicts with build/test config, trust the config and update this file.

## Start every session

1. Read the skills in `.claude/skills/` (`ls .claude/skills`) before coding. They carry the stack
   conventions (dotnet-webapi, ef-core, clean-architecture, testing, tdd, bootstrap5-ui, ...).
   Load the ones that match the task.
2. `skills-lock.json` pins the installed skills. Update them with `npx skills update` (project scope).
3. Repo docs are bilingual (English `*.md` + Spanish `*.es.md`); keep both in sync. `AGENTS.md` is
   English-only by convention.

## Hard rules

- No emojis anywhere: code, comments, docs, UI text, commit messages.
- All code comments are written in English. Documentation is bilingual (English + Spanish); see
  Documentation below for the per-member convention.
- Respect clean architecture boundaries. Never add a project reference that violates them.
- Use DTOs for data crossing layer or API boundaries; never leak domain entities into the Web layer.
- The project must run right after cloning with only `docker compose up -d` and `dotnet run`; no
  manual configuration (no `.env`, connection data comes from `docker-compose.yml`).
- The app must be internationalized (i18n) with a visible language selector; never hardcode UI strings.
- Every feature ships with unit tests and integration tests.
- Never run `git commit` or `git push` unless the user explicitly asks for it in that message.
- End every reply to the user with exactly: This is the way

## Documentation

- Document every `public` and `protected` member with XML doc comments (`///`): `<summary>`, plus
  `<param>`, `<typeparam>` and `<returns>` where they apply. This includes types, methods, properties,
  constructors, constants and positional record parameters.
- Use plain `//` comments for private members, trivial properties and non-obvious intent (business
  rules, gotchas, rationale).
- Tests follow the same split: XML docs on the test class and each test method; `//` for helpers and
  the Arrange/Act/Assert sections.
- Generated EF Core migrations and `*.Designer.cs` files are exempt; never hand-edit them.
- Top-level statements (`Program.cs`) cannot carry XML docs, so document them with `//`.

## Architecture

Layered modular monolith with four layers: Domain, Application, Infrastructure and Presentation.
Project references (verified):

- `StockFlow.Domain` -> no project references (entities and business invariants).
- `StockFlow.Application` -> Domain (use cases, DTOs, abstractions).
- `StockFlow.Infrastructure` -> Domain, Application (EF Core, repositories, Identity).
- `StockFlow.Web` -> Application, Infrastructure. This is the presentation layer (Razor Pages, views).

Target framework `net10.0`, with nullable and implicit usings enabled. One type per file; the
existing `Class1.cs` files are placeholders.

## Commands

Run from the repository root.

- Start database: `docker compose up -d` (SQL Server 2022 Developer, host port 14330 -> container 1433;
  14330 avoids clashing with a local SQL Server instance already using 1433 on some machines).
- Restore and build: `dotnet restore` then `dotnet build`.
- Run the web app: `dotnet run --project src/StockFlow.Web`.
- Run all tests: `dotnet test`.
- Run one test: `dotnet test --filter "FullyQualifiedName~<Namespace>.<Class>.<Method>"`.

`StockFlow.slnx` is the new XML solution format (.NET 10); the `dotnet` CLI handles it directly.

## Environment and database

- The SQL Server `SA_PASSWORD` is defined directly in `docker-compose.yml` (a dev-only value). There
  is no `.env` file; do not add one for the database.
- SQL Server data persists in the `sqlserver-data` Docker volume; reset with `docker compose down -v`.
- The EF Core connection string (`DefaultConnection`) lives in `appsettings.Development.json` and
  matches `docker-compose.yml`. The app applies pending migrations on startup (`DatabaseInitializer`).

## Testing conventions

- xUnit. Tests live under `tests/` as `StockFlow.<Layer>.Tests` projects: `Domain.Tests` (unit),
  `Application.Tests` (handlers, EF Core InMemory) and `Web.Tests` (integration, real SQL Server in
  its own `StockFlowDb_Test` database).
- Integration tests must exercise the real SQL Server from `docker-compose`, not an in-memory provider.
- Register new test projects in `StockFlow.slnx`.

## Repo notes

- `.claude/skills/` is committed and shared with the team. `.claude/scheduled_tasks.lock` is runtime
  state and gitignored; never commit it.
- Current status: phases 1-3 done (products, customers/suppliers, inventory). Inventory movements are
  the only way stock changes; there is a per-product `rowversion` for optimistic concurrency, an
  `ICurrentUser` abstraction and an injected `TimeProvider`. Purchases, sales and invoicing are not
  implemented yet.
- UI stack: Razor Pages + Bootstrap 5 + jQuery from `wwwroot/lib`; there is no npm build step.
