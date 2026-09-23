using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StockFlow.Domain;

namespace StockFlow.Infrastructure.Identity;

/// <summary>
/// Seeds the fixed application roles and, when configured, a single administrator account. Safe to
/// run on every startup because it only creates what is missing.
/// </summary>
public static class IdentitySeeder
{
    /// <summary>
    /// Ensures the known roles exist and creates the seed administrator when credentials are set.
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

        var adminEmail = configuration["SeedAdmin:Email"];
        var adminPassword = configuration["SeedAdmin:Password"];

        // No seed credentials configured (e.g. a Production-like environment without SeedAdmin
        // set): skip user creation instead of seeding a predictable account.
        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            return;
        }

        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser is null)
        {
            adminUser = new IdentityUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };

            await userManager.CreateAsync(adminUser, adminPassword);
        }

        if (!await userManager.IsInRoleAsync(adminUser, ApplicationRoles.Administrator))
        {
            await userManager.AddToRoleAsync(adminUser, ApplicationRoles.Administrator);
        }
    }
}
