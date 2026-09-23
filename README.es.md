# stockflow

**[English](README.md) | [Español](README.es.md)**

stockflow es un mini ERP: un sistema de gestión comercial para una pequeña empresa,
que modela inventario, compras, ventas y facturación electrónica simulada.

## Stack

- ASP.NET Core (Razor Pages)
- Entity Framework Core
- SQL Server (vía Docker, `mcr.microsoft.com/mssql/server`, edición Developer)
- ASP.NET Core Identity
- Bootstrap
- Docker / Docker Compose
- xUnit

## Arquitectura

Monolito modular, un solo repositorio, separado en capas desde el inicio:

- `Domain` — entidades y reglas de negocio (invariantes)
- `Application` — casos de uso / servicios de aplicación
- `Infrastructure` — EF Core, repositorios, Identity
- `Web` — Razor Pages, controllers, vistas

## Reglas de negocio

- **Inventario:** el stock solo puede cambiar mediante movimientos de inventario (compra, venta,
  ajuste, devolución) — nunca se edita directo.
- **Ventas:** una venta confirmada no puede modificarse directamente; cancelarla revierte el
  inventario.
- **Devoluciones:** la cantidad devuelta no puede superar la cantidad originalmente vendida menos
  devoluciones anteriores.
- **Documentos electrónicos:** una factura emitida no puede editarse; debe cancelarse y generar un
  nuevo documento.
- **Auditoría:** las operaciones críticas registran usuario, timestamp, entidad afectada y valores
  relevantes.

## Requisitos

- [.NET SDK 10](https://dotnet.microsoft.com/download)
- [Docker](https://docs.docker.com/get-docker/) con Docker Compose, para SQL Server
- [Herramientas CLI de EF Core](https://learn.microsoft.com/ef/core/cli/dotnet)
  (`dotnet tool install --global dotnet-ef`), solo necesarias para crear o aplicar migraciones

## Compilar y ejecutar

No se requiere configuración manual: los ajustes de SQL Server están en `docker-compose.yml` y se
toman automáticamente. SQL Server escucha en el puerto `14330` del host (no el `1433` por defecto),
para evitar chocar con una instancia local de SQL Server que ya podría estar usándolo en tu máquina.

```bash
# 1. Levantar SQL Server (Docker)
docker compose up -d

# 2. Restaurar y compilar la solución
dotnet restore
dotnet build

# 3. Aplicar las migraciones de la base de datos (crea la base de datos, las tablas de
#    Identity y la tabla __EFMigrationsHistory)
dotnet ef database update --project src/StockFlow.Infrastructure --startup-project src/StockFlow.Web

# 4. Ejecutar la app web
dotnet run --project src/StockFlow.Web
```

La app escucha por defecto en `http://localhost:5124`. En el primer arranque siembra tres roles
(`Administrator`, `Salesperson`, `InventoryManager`) y una cuenta de administrador de prueba, ambos
definidos en `appsettings.Development.json`:

- Correo: `admin@stockflow.local`
- Contraseña: `Admin#2026`

La interfaz está disponible en español (por defecto) e inglés; usa el selector de idioma en la barra
de navegación para cambiar. La autenticación usa el login por cookie de ASP.NET Core Identity
(`HttpOnly`, `SameSite=Lax`), así que refrescar la página mantiene la sesión — no se guarda ningún
token en `localStorage`/`sessionStorage`.

## Estructura del proyecto

```text
stockflow/
├── docker-compose.yml        # SQL Server 2022 edición Developer
├── StockFlow.slnx            # Archivo de solución
└── src/
    ├── StockFlow.Domain/          # Entidades y reglas de negocio
    ├── StockFlow.Application/     # Casos de uso / servicios de aplicación
    ├── StockFlow.Infrastructure/  # EF Core, repositorios, Identity
    └── StockFlow.Web/             # Razor Pages, controllers, vistas
```

## Estado

Configuración del proyecto completa: estructura de la solución, Docker, EF Core y ASP.NET Core
Identity (roles, cuenta de administrador de prueba, login/logout por cookie, interfaz en español e
inglés). Las funcionalidades (inventario, compras, ventas, facturación) aún no están implementadas.
