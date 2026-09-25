using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockFlow.Domain.Entities;

namespace StockFlow.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for <see cref="Purchase"/>: table name, check constraints, column sizes, the
/// supplier relationship and the indexes.
/// </summary>
public class PurchaseConfiguration : IEntityTypeConfiguration<Purchase>
{
    /// <summary>Configures the purchase entity.</summary>
    /// <param name="builder">Entity type builder supplied by EF Core.</param>
    public void Configure(EntityTypeBuilder<Purchase> builder)
    {
        builder.ToTable("Purchases", table =>
        {
            table.HasCheckConstraint("CK_Purchases_Number", "LEN(LTRIM(RTRIM([Number]))) > 0");
        });

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Number)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(p => p.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(p => p.Note)
            .HasMaxLength(1000);

        builder.Property(p => p.CreatedByUserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(p => p.CreatedByUserName)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(p => p.OrderDate)
            .IsRequired();

        builder.HasIndex(p => p.Number)
            .IsUnique();

        builder.HasIndex(p => p.SupplierId);

        builder.HasIndex(p => p.OrderDate);

        // Restrict: purchases are cancelled, never deleted, so the supplier stays referenced.
        builder.HasOne(p => p.Supplier)
            .WithMany()
            .HasForeignKey(p => p.SupplierId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        // Lines are read back through the backing field, not through the exposed read-only list.
        builder.Navigation(p => p.Lines)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Derived in-memory helper, not a persisted column.
        builder.Ignore(p => p.Total);
    }
}
