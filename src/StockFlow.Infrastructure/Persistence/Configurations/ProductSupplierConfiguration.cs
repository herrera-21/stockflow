using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockFlow.Domain.Entities;

namespace StockFlow.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for <see cref="ProductSupplier"/>: the product-supplier join table, its composite
/// key, column sizes and the relationships to <see cref="Product"/> and <see cref="Supplier"/>.
/// </summary>
public class ProductSupplierConfiguration : IEntityTypeConfiguration<ProductSupplier>
{
    /// <summary>Configures the product-supplier association.</summary>
    /// <param name="builder">Entity type builder supplied by EF Core.</param>
    public void Configure(EntityTypeBuilder<ProductSupplier> builder)
    {
        builder.ToTable("ProductSuppliers");

        builder.HasKey(ps => new { ps.ProductId, ps.SupplierId });

        builder.Property(ps => ps.SupplierSku)
            .HasMaxLength(100);

        builder.Property(ps => ps.PurchasePrice)
            .HasPrecision(18, 2);

        builder.Property(ps => ps.IsPreferred)
            .IsRequired();

        builder.HasOne<Product>()
            .WithMany(p => p.Suppliers)
            .HasForeignKey(ps => ps.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Supplier>()
            .WithMany(s => s.Products)
            .HasForeignKey(ps => ps.SupplierId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(ps => ps.SupplierId);
    }
}
