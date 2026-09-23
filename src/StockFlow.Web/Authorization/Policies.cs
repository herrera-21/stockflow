using Microsoft.AspNetCore.Authorization;
using StockFlow.Domain;

namespace StockFlow.Web.Authorization;

/// <summary>
/// Authorization policies used by the web layer.
/// </summary>
public static class Policies
{
    /// <summary>Policy name for managing products. Only administrators and inventory managers pass.</summary>
    public const string CanManageProducts = "CanManageProducts";

    /// <summary>
    /// Registers the application authorization policies.
    /// </summary>
    /// <param name="options">Authorization options to configure.</param>
    public static void Configure(AuthorizationOptions options)
    {
        options.AddPolicy(CanManageProducts, policy => policy
            .RequireAuthenticatedUser()
            .RequireRole(ApplicationRoles.Administrator, ApplicationRoles.InventoryManager));
    }
}
