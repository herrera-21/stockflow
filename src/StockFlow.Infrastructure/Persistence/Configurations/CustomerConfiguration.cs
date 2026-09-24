using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockFlow.Domain.Entities;

namespace StockFlow.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for <see cref="Customer"/>: table name, column sizes and indexes.
/// </summary>
public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    /// <summary>Configures the customer entity.</summary>
    /// <param name="builder">Entity type builder supplied by EF Core.</param>
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.DocumentType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(c => c.TaxId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(c => c.Phone)
            .HasMaxLength(50);

        builder.Property(c => c.Email)
            .HasMaxLength(200);

        builder.Property(c => c.Address)
            .HasMaxLength(300);

        builder.Property(c => c.IsActive)
            .IsRequired();

        // A document is unique per type: the same digits can be a DUI for one customer and, in
        // theory, part of a NIT for another.
        builder.HasIndex(c => new { c.DocumentType, c.TaxId })
            .IsUnique();

        builder.HasIndex(c => c.Name);
    }
}
