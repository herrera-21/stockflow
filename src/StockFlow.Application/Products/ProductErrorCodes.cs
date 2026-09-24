namespace StockFlow.Application.Products;

/// <summary>
/// Stable error codes returned by product use cases. They double as localization resource keys.
/// </summary>
public static class ProductErrorCodes
{
    /// <summary>A product with the same SKU already exists.</summary>
    public const string SkuAlreadyExists = "ProductSkuAlreadyExists";

    /// <summary>The requested product does not exist.</summary>
    public const string NotFound = "ProductNotFound";

    /// <summary>The referenced category does not exist or is not active.</summary>
    public const string CategoryNotFound = "CategoryNotFound";

    /// <summary>The base unit cannot change while the product has stock.</summary>
    public const string BaseUnitLocked = "ProductBaseUnitLocked";

    /// <summary>The product changed between reading and saving; the operation must be retried.</summary>
    public const string ConcurrencyConflict = "ProductConcurrencyConflict";
}
