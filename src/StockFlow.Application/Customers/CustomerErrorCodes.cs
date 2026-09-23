namespace StockFlow.Application.Customers;

/// <summary>
/// Stable error codes returned by customer use cases. They double as localization resource keys.
/// </summary>
public static class CustomerErrorCodes
{
    /// <summary>A customer with the same tax identification already exists.</summary>
    public const string TaxIdAlreadyExists = "CustomerTaxIdAlreadyExists";

    /// <summary>The requested customer does not exist.</summary>
    public const string NotFound = "CustomerNotFound";
}
