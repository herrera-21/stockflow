namespace StockFlow.Application.Suppliers;

/// <summary>
/// Stable error codes returned by supplier use cases. They double as localization resource keys.
/// </summary>
public static class SupplierErrorCodes
{
    /// <summary>A supplier with the same tax identification already exists.</summary>
    public const string TaxIdAlreadyExists = "SupplierTaxIdAlreadyExists";

    /// <summary>The requested supplier does not exist.</summary>
    public const string NotFound = "SupplierNotFound";
}
