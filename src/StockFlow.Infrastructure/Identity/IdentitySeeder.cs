using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StockFlow.Domain;

namespace StockFlow.Infrastructure.Identity;

/// <summary>
/// Seeds the fixed application roles and, when configured, one test account per role. Safe to run on
/// every startup because it only creates what is missing.
/// </summary>
public static class IdentitySeeder
{
    /// <summary>
    /// Ensures the known roles exist and creates the configured seed users. Each user is described by
    /// an email, a password and a role under the <c>SeedUsers</c> configuration section.
    /// </summary>
    /// <param name="services">Scoped service provider used to resolve the Identity managers.</param>
    public static async Task SeedAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
        var configuration = services.GetRequiredService<IConfiguration>();

        foreach (var role in ApplicationRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        foreach (var seedUser in configuration.GetSection("SeedUsers").GetChildren())
        {
            var email = seedUser["Email"];
            var password = seedUser["Password"];
            var role = seedUser["Role"];

            // Skip entries without credentials (e.g. a Production-like environment without SeedUsers
            // set): never seed a predictable account.
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                continue;
            }

            var user = await userManager.FindByEmailAsync(email);
            if (user is null)
            {
                user = new IdentityUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true
                };

                await userManager.CreateAsync(user, password);
            }

            if (!string.IsNullOrWhiteSpace(role) && !await userManager.IsInRoleAsync(user, role))
            {
                await userManager.AddToRoleAsync(user, role);
            }
        }
    }
}
