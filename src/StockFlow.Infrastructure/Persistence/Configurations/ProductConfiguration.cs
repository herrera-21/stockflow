using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockFlow.Domain;
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
            table.HasCheckConstraint("CK_Products_CurrentStock", QuantityCheck("CurrentStock"));
            table.HasCheckConstraint("CK_Products_MinimumStock", QuantityCheck("MinimumStock"));
            table.HasCheckConstraint("CK_Products_PurchaseUnitFactor",
                $"[PurchaseUnitFactor] > 0 AND ([BaseUnit] NOT IN ({CountableUnits()}) " +
                "OR [PurchaseUnitFactor] = FLOOR([PurchaseUnitFactor]))");
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

        // Units are stored as text; existing rows default to a plain unit bought and sold as is.
        builder.Property(p => p.BaseUnit)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(UnitOfMeasure.Unit);

        builder.Property(p => p.PurchaseUnit)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(UnitOfMeasure.Unit);

        builder.Property(p => p.PurchaseUnitFactor)
            .HasPrecision(18, 4)
            .HasDefaultValue(1m);

        builder.Property(p => p.PurchasePrice)
            .HasPrecision(18, 2);

        builder.Property(p => p.SalePrice)
            .HasPrecision(18, 2);

        builder.Property(p => p.TaxRate)
            .HasPrecision(5, 2);

        builder.Property(p => p.CurrentStock)
            .HasPrecision(18, 3)
            .IsRequired();

        builder.Property(p => p.MinimumStock)
            .HasPrecision(18, 3)
            .IsRequired();

        builder.Property(p => p.IsActive)
            .IsRequired();

        builder.HasIndex(p => p.Sku)
            .IsUnique();

        builder.HasIndex(p => p.CategoryId);

        // Restrict: a category in use cannot be deleted at the database level either.
        builder.HasOne(p => p.Category)
            .WithMany()
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        // Derived in-memory helpers, not persisted columns.
        builder.Ignore(p => p.HasLowStock);
        builder.Ignore(p => p.UnitCost);
    }

    // SQL check for a stock column: never negative and whole for countable base units, mirroring
    // the domain invariant.
    private static string QuantityCheck(string column) =>
        $"[{column}] >= 0 AND ([BaseUnit] NOT IN ({CountableUnits()}) OR [{column}] = FLOOR([{column}]))";

    // Quoted SQL list of the units that only take whole quantities, derived from the domain rule so
    // both stay in sync.
    private static string CountableUnits() =>
        string.Join(", ", Enum.GetValues<UnitOfMeasure>()
            .Where(unit => !unit.AllowsFractions())
            .Select(unit => $"'{unit}'"));
}
