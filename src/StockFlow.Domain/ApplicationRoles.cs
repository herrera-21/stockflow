namespace StockFlow.Domain;

// Role names stored in the database (AspNetRoles). Kept in English regardless of UI language,
// since UI labels are localized separately via the i18n resources.
public static class ApplicationRoles
{
    public const string Administrator = "Administrator";
    public const string Salesperson = "Salesperson";
    public const string InventoryManager = "InventoryManager";

    public static readonly IReadOnlyList<string> All =
    [
        Administrator,
        Salesperson,
        InventoryManager
    ];
}
