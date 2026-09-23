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
}
