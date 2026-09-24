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
| Categorías | sí | no | no |
| Ajustes y movimientos de inventario | sí | sí | no |
| Compras | sí | sí | no |
| Ventas | sí | no | sí |
| Facturación electrónica | sí | no | sí |
| Dashboard | sí | sí | sí |
| Usuarios y roles | sí | no | no |

Los productos se gestionan mediante la política de autorización `CanManageProducts`, que da acceso
solo a Administrator e InventoryManager; Salesperson tiene acceso de solo lectura al catálogo. Los
clientes y los proveedores usan las políticas `CanManageCustomers` (Administrator, Salesperson) y
`CanManageSuppliers` (Administrator, InventoryManager) respectivamente. Las categorías usan
`CanManageCategories`, restringida a Administrator porque definen el catálogo compartido.

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

La app escucha por defecto en `http://localhost:5124`. En el primer arranque siembra los tres roles
(`Administrator`, `Salesperson`, `InventoryManager`) y una cuenta de prueba por rol, todas definidas
en la sección `SeedUsers` de `appsettings.Development.json`. Inicia sesión con cualquiera de ellas
para probar el acceso por rol:

| Rol | Correo | Contraseña |
|---|---|---|
| Administrator | `admin@stockflow.local` | `Admin#2026` |
| InventoryManager | `inventory@stockflow.local` | `Inventory#2026` |
| Salesperson | `sales@stockflow.local` | `Sales#2026` |

La interfaz está disponible en español (por defecto) e inglés; usa el selector de idioma en el
encabezado para cambiar. La app usa una navegación lateral (sidebar) y un encabezado oscuro. Los
montos y porcentajes usan el punto como separador decimal (por ejemplo `1234.56`). La autenticación
usa el login por cookie de ASP.NET Core Identity (`HttpOnly`, `SameSite=Lax`), así que refrescar la
página mantiene la sesión — no se guarda ningún token en `localStorage`/`sessionStorage`.

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
  stock bajo. El campo SKU explica qué es y muestra un ejemplo, y la categoría es un selector
  buscable alimentado desde la base de datos.
- **Unidades de medida (productos):** cada producto tiene una unidad de venta (en la que se lleva
  el stock y se cobra el precio de venta) y una unidad de compra con un factor de conversión, por
  ejemplo se compra por caja de 24 y se vende por unidad. El precio de compra es por unidad de compra
  y de él se deriva el costo por unidad de venta. El stock es decimal, así que los productos a granel
  admiten fracciones (2.5 lb); las unidades contables (unidad, docena, caja, paquete) solo aceptan
  cantidades enteras, regla que se valida en el dominio, el formulario y la base de datos. La unidad
  de venta solo se puede cambiar si el producto no tiene stock.
- **Categorías:** tabla propia y CRUD (listar, crear, editar, desactivar/reactivar), restringido a
  Administrator. Una categoría no se puede desactivar mientras tenga productos asociados. Las
  categorías sembradas (limpieza, bebidas, alimentos, snacks, lácteos, panadería, cuidado personal,
  otros) se muestran con nombre localizado; las creadas por el usuario usan su propio nombre.
- **Clientes:** modelo, mapeo EF Core y migración, CRUD con acceso por rol, búsqueda en vivo por
  nombre, documento o correo, y un marcador de historial de compras (hasta la Fase 5, Ventas). El
  nombre se valida como nombre de persona y el documento usa un selector de tipo.
- **Proveedores:** modelo, mapeo EF Core y migración, CRUD con acceso por rol, búsqueda en vivo, y una
  asociación muchos-a-muchos de productos a proveedores que guarda el SKU del proveedor, el precio de
  compra acordado y un único proveedor preferido por producto.
- **Documento de identidad (clientes y proveedores):** un tipo de documento (DUI, NIT, pasaporte,
  otro) más el número, único por tipo. El DUI es `00000000-0`; el NIT es de 9 dígitos (DUI homologado
  para personas naturales) o de 14 dígitos `0000-000000-000-0` para personas jurídicas; pasaporte y
  otro quedan libres. El número se formatea automáticamente mientras se escribe.

La búsqueda, el filtrado, la paginación y las bajas lógicas funcionan con htmx (vendorizado en
`wwwroot/lib/htmx`, sin paso de build npm), sin recargar la página, y están cubiertos por pruebas
unitarias y de integración. Inventario, compras, ventas y facturación aún no están implementados.
