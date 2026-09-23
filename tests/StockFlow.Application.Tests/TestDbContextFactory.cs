using Microsoft.EntityFrameworkCore;
using StockFlow.Infrastructure.Persistence;

namespace StockFlow.Application.Tests;

/// <summary>
/// Creates isolated <see cref="AppDbContext"/> instances backed by the EF Core InMemory provider for
/// handler unit tests.
/// </summary>
internal static class TestDbContextFactory
{
    /// <summary>
    /// Creates a context on a fresh, uniquely named in-memory database.
    /// </summary>
    /// <returns>A new isolated <see cref="AppDbContext"/>.</returns>
    public static AppDbContext Create() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}
