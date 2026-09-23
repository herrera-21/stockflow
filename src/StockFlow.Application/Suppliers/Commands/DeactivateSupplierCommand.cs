namespace StockFlow.Application.Suppliers.Commands;

/// <summary>
/// Command that soft-deletes (deactivates) a supplier.
/// </summary>
/// <param name="Id">Identifier of the supplier to deactivate.</param>
public record DeactivateSupplierCommand(Guid Id);
