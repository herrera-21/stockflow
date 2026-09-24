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

    /// <summary>Policy name for managing customers. Only administrators and salespeople pass.</summary>
    public const string CanManageCustomers = "CanManageCustomers";

    /// <summary>Policy name for managing suppliers. Only administrators and inventory managers pass.</summary>
    public const string CanManageSuppliers = "CanManageSuppliers";

    /// <summary>Policy name for managing categories. Only administrators pass.</summary>
    public const string CanManageCategories = "CanManageCategories";

    /// <summary>Policy name for adjusting stock. Only administrators and inventory managers pass.</summary>
    public const string CanAdjustInventory = "CanAdjustInventory";

    /// <summary>
    /// Registers the application authorization policies.
    /// </summary>
    /// <param name="options">Authorization options to configure.</param>
    public static void Configure(AuthorizationOptions options)
    {
        options.AddPolicy(CanManageProducts, policy => policy
            .RequireAuthenticatedUser()
            .RequireRole(ApplicationRoles.Administrator, ApplicationRoles.InventoryManager));

        options.AddPolicy(CanManageCustomers, policy => policy
            .RequireAuthenticatedUser()
            .RequireRole(ApplicationRoles.Administrator, ApplicationRoles.Salesperson));

        options.AddPolicy(CanManageSuppliers, policy => policy
            .RequireAuthenticatedUser()
            .RequireRole(ApplicationRoles.Administrator, ApplicationRoles.InventoryManager));

        // Categories define the shared catalog, so only administrators may change them.
        options.AddPolicy(CanManageCategories, policy => policy
            .RequireAuthenticatedUser()
            .RequireRole(ApplicationRoles.Administrator));

        // Stock adjustments move inventory, so they follow the same roles as product management.
        options.AddPolicy(CanAdjustInventory, policy => policy
            .RequireAuthenticatedUser()
            .RequireRole(ApplicationRoles.Administrator, ApplicationRoles.InventoryManager));
    }
}
