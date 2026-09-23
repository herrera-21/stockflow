using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockFlow.Domain.Entities;

namespace StockFlow.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for <see cref="Product"/>: table name, check constraints, column sizes and
/// indexes.
/// </summary>
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    /// <summary>Configures the product entity.</summary>
    /// <param name="builder">Entity type builder supplied by EF Core.</param>
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products", table =>
        {
            table.HasCheckConstraint("CK_Products_PurchasePrice", "[PurchasePrice] >= 0");
            table.HasCheckConstraint("CK_Products_SalePrice", "[SalePrice] >= 0");
            table.HasCheckConstraint("CK_Products_TaxRate", "[TaxRate] >= 0 AND [TaxRate] <= 100");
            table.HasCheckConstraint("CK_Products_CurrentStock", "[CurrentStock] >= 0");
            table.HasCheckConstraint("CK_Products_MinimumStock", "[MinimumStock] >= 0");
        });

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Sku)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Description)
            .HasMaxLength(1000);

        builder.Property(p => p.Category)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.PurchasePrice)
            .HasPrecision(18, 2);

        builder.Property(p => p.SalePrice)
            .HasPrecision(18, 2);

        builder.Property(p => p.TaxRate)
            .HasPrecision(5, 2);

        builder.Property(p => p.CurrentStock)
            .IsRequired();

        builder.Property(p => p.MinimumStock)
            .IsRequired();

        builder.Property(p => p.IsActive)
            .IsRequired();

        builder.HasIndex(p => p.Sku)
            .IsUnique();

        builder.HasIndex(p => p.Category);

        // Derived in-memory helper, not a persisted column.
        builder.Ignore(p => p.HasLowStock);
    }
}
