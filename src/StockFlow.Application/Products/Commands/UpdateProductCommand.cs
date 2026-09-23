namespace StockFlow.Application.Products.Commands;

/// <summary>
/// Command that updates an existing product. Current stock is not editable.
/// </summary>
/// <param name="Id">Identifier of the product to update.</param>
/// <param name="Sku">Stock keeping unit; must be unique.</param>
/// <param name="Name">Display name.</param>
/// <param name="Description">Optional description.</param>
/// <param name="Category">Category.</param>
/// <param name="PurchasePrice">Supplier price.</param>
/// <param name="SalePrice">Customer price.</param>
/// <param name="TaxRate">Tax percentage (0-100).</param>
/// <param name="MinimumStock">Low-stock threshold.</param>
public record UpdateProductCommand(
    Guid Id,
    string Sku,
    string Name,
    string? Description,
    string Category,
    decimal PurchasePrice,
    decimal SalePrice,
    decimal TaxRate,
    int MinimumStock);
