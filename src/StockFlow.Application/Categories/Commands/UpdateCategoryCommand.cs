namespace StockFlow.Application.Categories.Commands;

/// <summary>
/// Command that renames an existing category.
/// </summary>
/// <param name="Id">Identifier of the category to update.</param>
/// <param name="Name">New display name; must be unique.</param>
public record UpdateCategoryCommand(Guid Id, string Name);
