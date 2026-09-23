using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Common.Interfaces;
using StockFlow.Domain.Entities;

namespace StockFlow.Infrastructure.Persistence;

/// <summary>
/// EF Core context for the application. It also serves as the Identity context, so it carries the
/// ASP.NET Core Identity tables in addition to the domain sets.
/// </summary>
public class AppDbContext : IdentityDbContext<IdentityUser, IdentityRole, string>, IAppDbContext
{
    /// <summary>Initializes the context with the provided options.</summary>
    /// <param name="options">Context options.</param>
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    /// <summary>Products set.</summary>
    public DbSet<Product> Products => Set<Product>();

    /// <summary>
    /// Applies all entity type configurations declared in this assembly.
    /// </summary>
    /// <param name="modelBuilder">Model builder supplied by EF Core.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
