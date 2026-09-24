namespace StockFlow.Application.Categories.Commands;

/// <summary>
/// Command that reactivates a previously deactivated category.
/// </summary>
/// <param name="Id">Identifier of the category to activate.</param>
public record ActivateCategoryCommand(Guid Id);
