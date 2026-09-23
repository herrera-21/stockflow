# stockflow

**[English](README.md) | [Español](README.es.md)**

stockflow es un mini ERP de portafolio: un sistema de gestión comercial para una pequeña empresa,
que modela inventario, compras, ventas y facturación electrónica simulada.

> Diseñé y desarrollé un sistema de gestión comercial para una pequeña empresa, modelando
> inventario, compras, ventas, facturación y reglas de negocio.

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

## Compilar y ejecutar

```bash
# 1. Levantar SQL Server (Docker)
docker compose up -d

# 2. Restaurar y compilar la solución
dotnet restore
dotnet build

# 3. Ejecutar la app web
dotnet run --project src/StockFlow.Web
```

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

Configuración del proyecto en progreso (estructura de la solución, Docker, EF Core, Identity). Las
funcionalidades (inventario, compras, ventas, facturación) aún no están implementadas.
