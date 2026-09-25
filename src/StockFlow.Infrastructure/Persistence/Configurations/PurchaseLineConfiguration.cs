using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockFlow.Domain.Entities;

namespace StockFlow.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for <see cref="PurchaseLine"/>: table name, check constraints, column sizes, the
/// snapshotted unit columns, the relationships and the indexes.
/// </summary>
public class PurchaseLineConfiguration : IEntityTypeConfiguration<PurchaseLine>
{
    /// <summary>Configures the purchase line entity.</summary>
    /// <param name="builder">Entity type builder supplied by EF Core.</param>
    public void Configure(EntityTypeBuilder<PurchaseLine> builder)
    {
        builder.ToTable("PurchaseLines", table =>
        {
            // Quantity is expressed in the purchase unit, so the countable-unit rule uses that
            // snapshot column rather than the base unit.
            table.HasCheckConstraint(
                "CK_PurchaseLines_Quantity",
                QuantityCheckSql.PositiveWhole("Quantity", "PurchaseUnit"));
            table.HasCheckConstraint("CK_PurchaseLines_PurchaseUnitCost", "[PurchaseUnitCost] >= 0");
            table.HasCheckConstraint("CK_PurchaseLines_PurchaseUnitFactor", "[PurchaseUnitFactor] > 0");
            table.HasCheckConstraint("CK_PurchaseLines_Subtotal", "[Subtotal] >= 0");
        });

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Quantity)
            .HasPrecision(18, 3);

        builder.Property(l => l.PurchaseUnitCost)
            .HasPrecision(18, 2);

        // Enums are stored as text and the units are snapshotted so later product changes do not alter
        // the historical order.
        builder.Property(l => l.PurchaseUnit)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(l => l.BaseUnit)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(l => l.PurchaseUnitFactor)
            .HasPrecision(18, 4);

        builder.Property(l => l.Subtotal)
            .HasPrecision(18, 2);

        // A product cannot appear twice in the same order; this backs the domain rule.
        builder.HasIndex(l => new { l.PurchaseId, l.ProductId })
            .IsUnique();

        builder.HasIndex(l => l.ProductId);

        // Restrict: an order keeps its lines, and a product with order lines cannot be removed.
        builder.HasOne<Purchase>()
            .WithMany(p => p.Lines)
            .HasForeignKey(l => l.PurchaseId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(l => l.ProductId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();
    }
}
