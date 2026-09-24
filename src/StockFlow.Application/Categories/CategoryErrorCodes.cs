namespace StockFlow.Application.Categories;

/// <summary>
/// Stable error codes returned by category use cases. They double as localization resource keys.
/// </summary>
public static class CategoryErrorCodes
{
    /// <summary>A category with the same name already exists.</summary>
    public const string NameAlreadyExists = "CategoryNameAlreadyExists";

    /// <summary>The requested category does not exist.</summary>
    public const string NotFound = "CategoryNotFound";

    /// <summary>The category cannot be deactivated because products are associated with it.</summary>
    public const string InUse = "CategoryInUse";
}
