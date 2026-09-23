namespace StockFlow.Domain;

/// <summary>
/// Role names stored in the database (AspNetRoles). Kept in English regardless of UI language,
/// since UI labels are localized separately via the i18n resources.
/// </summary>
public static class ApplicationRoles
{
    /// <summary>Administrator role: full access, including product management.</summary>
    public const string Administrator = "Administrator";

    /// <summary>Salesperson role: can view products but not manage them.</summary>
    public const string Salesperson = "Salesperson";

    /// <summary>Inventory manager role: can create, edit and deactivate products.</summary>
    public const string InventoryManager = "InventoryManager";

    /// <summary>All roles known to the application, used to seed them at startup.</summary>
    public static readonly IReadOnlyList<string> All =
    [
        Administrator,
        Salesperson,
        InventoryManager
    ];
}
