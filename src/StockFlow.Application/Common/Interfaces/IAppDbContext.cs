using Microsoft.EntityFrameworkCore;
using StockFlow.Domain.Entities;

namespace StockFlow.Application.Common.Interfaces;

/// <summary>
/// Minimal persistence abstraction. EF Core's <see cref="DbSet{TEntity}"/> already implements the
/// repository and unit-of-work patterns, so use cases depend on this instead of a repository per
/// entity.
/// </summary>
public interface IAppDbContext
{
    /// <summary>Products set.</summary>
    DbSet<Product> Products { get; }

    /// <summary>Categories set.</summary>
    DbSet<Category> Categories { get; }

    /// <summary>Customers set.</summary>
    DbSet<Customer> Customers { get; }

    /// <summary>Suppliers set.</summary>
    DbSet<Supplier> Suppliers { get; }

    /// <summary>Product-supplier associations set.</summary>
    DbSet<ProductSupplier> ProductSuppliers { get; }

    /// <summary>Persists pending changes to the database.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of state entries written to the database.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
