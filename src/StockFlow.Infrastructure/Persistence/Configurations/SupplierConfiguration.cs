using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockFlow.Domain.Entities;

namespace StockFlow.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for <see cref="Supplier"/>: table name, column sizes and indexes.
/// </summary>
public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    /// <summary>Configures the supplier entity.</summary>
    /// <param name="builder">Entity type builder supplied by EF Core.</param>
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("Suppliers");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.TaxId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(s => s.ContactName)
            .HasMaxLength(200);

        builder.Property(s => s.Phone)
            .HasMaxLength(50);

        builder.Property(s => s.Email)
            .HasMaxLength(200);

        builder.Property(s => s.Address)
            .HasMaxLength(300);

        builder.Property(s => s.IsActive)
            .IsRequired();

        builder.HasIndex(s => s.TaxId)
            .IsUnique();

        builder.HasIndex(s => s.Name);
    }
}
