namespace StockFlow.Application.Categories.Commands;

/// <summary>
/// Command that deactivates (soft-deletes) a category.
/// </summary>
/// <param name="Id">Identifier of the category to deactivate.</param>
public record DeactivateCategoryCommand(Guid Id);
