using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockFlow.Domain.Entities;

namespace StockFlow.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for <see cref="InventoryMovement"/>: table name, check constraints, column sizes,
/// the product relationship and the history index.
/// </summary>
public class InventoryMovementConfiguration : IEntityTypeConfiguration<InventoryMovement>
{
    /// <summary>Configures the inventory movement entity.</summary>
    /// <param name="builder">Entity type builder supplied by EF Core.</param>
    public void Configure(EntityTypeBuilder<InventoryMovement> builder)
    {
        builder.ToTable("InventoryMovements", table =>
        {
            table.HasCheckConstraint(
                "CK_InventoryMovements_Quantity",
                QuantityCheckSql.PositiveWhole("Quantity"));
            table.HasCheckConstraint(
                "CK_InventoryMovements_StockBefore",
                QuantityCheckSql.NonNegativeWhole("StockBefore"));
            table.HasCheckConstraint(
                "CK_InventoryMovements_StockAfter",
                QuantityCheckSql.NonNegativeWhole("StockAfter"));
        });

        builder.HasKey(m => m.Id);

        // Enums are stored as text and the unit is snapshotted so later product changes do not alter
        // the history.
        builder.Property(m => m.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(m => m.BaseUnit)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(m => m.Reason)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(m => m.ReferenceType)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(m => m.Quantity)
            .HasPrecision(18, 3);

        builder.Property(m => m.StockBefore)
            .HasPrecision(18, 3);

        builder.Property(m => m.StockAfter)
            .HasPrecision(18, 3);

        builder.Property(m => m.Note)
            .HasMaxLength(1000);

        builder.Property(m => m.UserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(m => m.UserName)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(m => m.OccurredAt)
            .IsRequired();

        // Restrict: a product keeps its movement history; products are soft-deleted, never removed.
        builder.HasOne(m => m.Product)
            .WithMany(p => p.Movements)
            .HasForeignKey(m => m.ProductId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        // Covers the product history page, which reads a product's movements newest first.
        builder.HasIndex(m => new { m.ProductId, m.OccurredAt });
    }
}
