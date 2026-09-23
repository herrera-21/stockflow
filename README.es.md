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
- htmx (vendorizado, sin paso de build npm)
- Docker / Docker Compose
- xUnit

## Arquitectura

Monolito modular, un solo repositorio, separado en capas desde el inicio:

- `Domain` — entidades y reglas de negocio (invariantes)
- `Application` — casos de uso / servicios de aplicación
- `Infrastructure` — EF Core, persistencia, Identity
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

## Roles

| Capacidad | Administrator | InventoryManager | Salesperson |
|---|---|---|---|
| Ver productos | sí | sí | sí |
| Crear / editar / desactivar productos | sí | sí | no |
| Clientes | sí | no | sí |
| Proveedores | sí | sí | no |
| Ajustes y movimientos de inventario | sí | sí | no |
| Compras | sí | sí | no |
| Ventas | sí | no | sí |
| Facturación electrónica | sí | no | sí |
| Dashboard | sí | sí | sí |
| Usuarios y roles | sí | no | no |

Los productos se gestionan mediante la política de autorización `CanManageProducts`, que da acceso
solo a Administrator e InventoryManager; Salesperson tiene acceso de solo lectura al catálogo. Los
clientes y los proveedores usan las políticas `CanManageCustomers` (Administrator, Salesperson) y
`CanManageSuppliers` (Administrator, InventoryManager) respectivamente.

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

## Tests

La suite de pruebas está separada por capa:

- `tests/StockFlow.Domain.Tests` — invariantes del dominio (unitarias).
- `tests/StockFlow.Application.Tests` — casos de uso / handlers (unitarias, EF Core InMemory).
- `tests/StockFlow.Web.Tests` — flujo HTTP completo (integración). Levantan la app real y usan el
  SQL Server de `docker-compose.yml`, aislado en su propia base de datos `StockFlowDb_Test`.

```bash
# Ejecutar todas las pruebas
dotnet test

# Ejecutar una prueba concreta
dotnet test --filter "FullyQualifiedName~StockFlow.Domain.Tests.Entities.ProductTests"
```

Las pruebas de integración requieren el contenedor de SQL Server levantado (`docker compose up -d`).

## Estructura del proyecto

```text
stockflow/
├── docker-compose.yml        # SQL Server 2022 edición Developer
├── StockFlow.slnx            # Archivo de solución
└── src/
    ├── StockFlow.Domain/          # Entidades y reglas de negocio
    ├── StockFlow.Application/     # Casos de uso / servicios de aplicación
    ├── StockFlow.Infrastructure/  # EF Core, persistencia, Identity
    └── StockFlow.Web/             # Razor Pages, controllers, vistas
```

## Estado

Las Fases 1 (productos) y 2 (clientes y proveedores) están funcionalmente completas.

- **Productos:** modelo, mapeo EF Core y migración, CRUD (listado con paginación, crear, editar, baja
  lógica) con acceso por rol, búsqueda y filtrado en vivo por nombre, SKU y categoría, e indicador de
  stock bajo.
- **Clientes:** modelo, mapeo EF Core y migración, CRUD con acceso por rol, búsqueda en vivo por
  nombre, identificación fiscal o correo, y un marcador de historial de compras (hasta la Fase 5,
  Ventas).
- **Proveedores:** modelo, mapeo EF Core y migración, CRUD con acceso por rol, búsqueda en vivo, y una
  asociación muchos-a-muchos de productos a proveedores que guarda el SKU del proveedor, el precio de
  compra acordado y un único proveedor preferido por producto.

La búsqueda, el filtrado, la paginación y las bajas lógicas funcionan con htmx (vendorizado en
`wwwroot/lib/htmx`, sin paso de build npm), sin recargar la página, y están cubiertos por pruebas
unitarias y de integración. Inventario, compras, ventas y facturación aún no están implementados.
