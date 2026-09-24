using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockFlow.Domain;
using StockFlow.Domain.Entities;

namespace StockFlow.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for <see cref="Category"/>: table name, column sizes, the unique name index and
/// the seeded standard categories.
/// </summary>
public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    /// <summary>Configures the category entity and seeds the standard categories.</summary>
    /// <param name="builder">Entity type builder supplied by EF Core.</param>
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Code)
            .HasMaxLength(50);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.IsActive)
            .IsRequired();

        builder.HasIndex(c => c.Name)
            .IsUnique();

        // Standard categories shipped with the app. The Name is the canonical (Spanish) fallback;
        // the Code is used to look up the localized display name.
        builder.HasData(
            new { Id = CategoryIds.Cleaning, Code = "cleaning", Name = "Limpieza", IsActive = true },
            new { Id = CategoryIds.Beverages, Code = "beverages", Name = "Bebidas", IsActive = true },
            new { Id = CategoryIds.Food, Code = "food", Name = "Alimentos", IsActive = true },
            new { Id = CategoryIds.Snacks, Code = "snacks", Name = "Snacks", IsActive = true },
            new { Id = CategoryIds.Dairy, Code = "dairy", Name = "Lácteos", IsActive = true },
            new { Id = CategoryIds.Bakery, Code = "bakery", Name = "Panadería", IsActive = true },
            new { Id = CategoryIds.PersonalCare, Code = "personal_care", Name = "Cuidado personal", IsActive = true },
            new { Id = CategoryIds.Other, Code = "other", Name = "Otros", IsActive = true });
    }
}
