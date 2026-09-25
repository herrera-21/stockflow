using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Common.Interfaces;
using StockFlow.Domain.Entities;

namespace StockFlow.Infrastructure.Persistence;

/// <summary>
/// EF Core context for the application. It also serves as the Identity context, so it carries the
/// ASP.NET Core Identity tables in addition to the domain sets.
/// </summary>
public class AppDbContext : IdentityDbContext<IdentityUser, IdentityRole, string>, IAppDbContext
{
    /// <summary>Initializes the context with the provided options.</summary>
    /// <param name="options">Context options.</param>
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    /// <summary>Products set.</summary>
    public DbSet<Product> Products => Set<Product>();

    /// <summary>Categories set.</summary>
    public DbSet<Category> Categories => Set<Category>();

    /// <summary>Customers set.</summary>
    public DbSet<Customer> Customers => Set<Customer>();

    /// <summary>Suppliers set.</summary>
    public DbSet<Supplier> Suppliers => Set<Supplier>();

    /// <summary>Product-supplier associations set.</summary>
    public DbSet<ProductSupplier> ProductSuppliers => Set<ProductSupplier>();

    /// <summary>Inventory movements set.</summary>
    public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();

    /// <summary>Purchase orders set.</summary>
    public DbSet<Purchase> Purchases => Set<Purchase>();

    /// <summary>Purchase order lines set.</summary>
    public DbSet<PurchaseLine> PurchaseLines => Set<PurchaseLine>();

    /// <summary>
    /// Applies all entity type configurations declared in this assembly and declares the database
    /// sequences used to number documents.
    /// </summary>
    /// <param name="modelBuilder">Model builder supplied by EF Core.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Atomic, gap-free-enough counter for purchase order numbers (OC-000001). The numbering
        // format lives in the domain; this only provides the monotonic value.
        modelBuilder.HasSequence<long>("PurchaseOrderNumberSequence")
            .StartsAt(1)
            .IncrementsBy(1);

        // SQL Server exposes <see cref="Product.RowVersion"/> as a real rowversion column, which EF
        // Core uses for optimistic concurrency. The InMemory provider used by handler unit tests does
        // not generate rowversion values, so the token is only marked as a concurrency token when the
        // provider supports it; otherwise every child insert would be flagged as a conflict.
        if (Database.IsSqlServer())
        {
            modelBuilder.Entity<Product>()
                .Property(p => p.RowVersion)
                .IsRowVersion();
        }
    }
}
