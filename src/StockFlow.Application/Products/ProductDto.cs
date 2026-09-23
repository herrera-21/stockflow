namespace StockFlow.Application.Products;

/// <summary>
/// Product data transferred across the Application boundary.
/// </summary>
/// <param name="Id">Unique identifier.</param>
/// <param name="Sku">Stock keeping unit.</param>
/// <param name="Name">Display name.</param>
/// <param name="Description">Optional description.</param>
/// <param name="Category">Category.</param>
/// <param name="PurchasePrice">Price paid to the supplier.</param>
/// <param name="SalePrice">Price charged to the customer.</param>
/// <param name="TaxRate">Tax percentage (0-100).</param>
/// <param name="CurrentStock">Quantity currently in stock.</param>
/// <param name="MinimumStock">Low-stock threshold.</param>
/// <param name="IsActive">Whether the product is active.</param>
/// <param name="HasLowStock">True when stock is at or below the minimum.</param>
public record ProductDto(
    Guid Id,
    string Sku,
    string Name,
    string? Description,
    string Category,
    decimal PurchasePrice,
    decimal SalePrice,
    decimal TaxRate,
    int CurrentStock,
    int MinimumStock,
    bool IsActive,
    bool HasLowStock);
