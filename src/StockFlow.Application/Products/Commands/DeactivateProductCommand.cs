namespace StockFlow.Application.Products.Commands;

/// <summary>
/// Command that soft-deletes (deactivates) a product.
/// </summary>
/// <param name="Id">Identifier of the product to deactivate.</param>
public record DeactivateProductCommand(Guid Id);
