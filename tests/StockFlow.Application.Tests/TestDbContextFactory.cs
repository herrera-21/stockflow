using Microsoft.EntityFrameworkCore;
using StockFlow.Domain.Entities;
using StockFlow.Infrastructure.Persistence;

namespace StockFlow.Application.Tests;

/// <summary>
/// Creates isolated <see cref="AppDbContext"/> instances backed by the EF Core InMemory provider for
/// handler unit tests.
/// </summary>
internal static class TestDbContextFactory
{
    /// <summary>
    /// Creates a context on a fresh, uniquely named in-memory database. The standard categories are
    /// ensured because the InMemory provider does not run migrations, so <c>HasData</c> seed data is
    /// not guaranteed to be present.
    /// </summary>
    /// <returns>A new isolated <see cref="AppDbContext"/>.</returns>
    public static AppDbContext Create()
    {
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

        if (!db.Categories.Any())
        {
            db.Categories.AddRange(
                Category.Create("Limpieza", "cleaning"),
                Category.Create("Bebidas", "beverages"),
                Category.Create("Otros", "other"));
            db.SaveChanges();
        }

        return db;
    }
}
