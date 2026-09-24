using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StockFlow.Domain;
using StockFlow.Infrastructure.Persistence;

namespace StockFlow.Web.Tests;

/// <summary>
/// Boots the real web app against the SQL Server from docker-compose.yml, isolated in its own
/// database (StockFlowDb_Test). AGENTS.md requires integration tests to use the real database.
/// </summary>
public class StockFlowWebFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    /// <summary>Connection string for the dedicated test database.</summary>
    public const string ConnectionString =
        "Server=localhost,14330;Database=StockFlowDb_Test;User Id=sa;Password=StockDev1;TrustServerCertificate=True";

    /// <summary>Email of the seeded administrator used by the tests.</summary>
    public const string AdminEmail = "admin@stockflow.local";

    /// <summary>Password of the seeded administrator used by the tests.</summary>
    public const string AdminPassword = "Admin#2026";

    /// <summary>Email of the seeded inventory manager used by the tests.</summary>
    public const string InventoryEmail = "inventory@stockflow.local";

    /// <summary>Password of the seeded inventory manager used by the tests.</summary>
    public const string InventoryPassword = "Inventory#2026";

    /// <summary>Email of the seeded salesperson used by the tests.</summary>
    public const string SalesEmail = "sales@stockflow.local";

    /// <summary>Password of the seeded salesperson used by the tests.</summary>
    public const string SalesPassword = "Sales#2026";

    /// <summary>Overrides the host configuration so tests use the test database and seed admin.</summary>
    /// <param name="builder">Host builder supplied by the test framework.</param>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = ConnectionString,
                ["SeedUsers:0:Email"] = AdminEmail,
                ["SeedUsers:0:Password"] = AdminPassword,
                ["SeedUsers:0:Role"] = ApplicationRoles.Administrator,
                ["SeedUsers:1:Email"] = InventoryEmail,
                ["SeedUsers:1:Password"] = InventoryPassword,
                ["SeedUsers:1:Role"] = ApplicationRoles.InventoryManager,
                ["SeedUsers:2:Email"] = SalesEmail,
                ["SeedUsers:2:Password"] = SalesPassword,
                ["SeedUsers:2:Role"] = ApplicationRoles.Salesperson
            });
        });
    }

    /// <summary>Applies migrations before the host starts so the database schema is ready.</summary>
    /// <returns>A task that completes once migrations have run.</returns>
    public async Task InitializeAsync()
    {
        // Migrate before the host starts so IdentitySeeder finds its tables.
        await using var db = CreateDbContext();
        await db.Database.MigrateAsync();
    }

    /// <summary>Disposes the factory and its host.</summary>
    /// <returns>A task that completes once the factory is disposed.</returns>
    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
    }

    /// <summary>Creates a context pointing at the test database.</summary>
    /// <returns>A new <see cref="AppDbContext"/> for the test database.</returns>
    public AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        return new AppDbContext(options);
    }

    /// <summary>
    /// Ensures a user exists with the given role, creating the role and user when missing.
    /// </summary>
    /// <param name="email">User email.</param>
    /// <param name="password">Password used when the user has to be created.</param>
    /// <param name="role">Role to assign.</param>
    /// <returns>A task that completes once the user is in the role.</returns>
    public async Task EnsureUserInRoleAsync(string email, string password, string role)
    {
        using var scope = Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
            await userManager.CreateAsync(user, password);
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            await userManager.AddToRoleAsync(user, role);
        }
    }
}
