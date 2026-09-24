namespace StockFlow.Application.Categories.Commands;

/// <summary>
/// Command that creates a new category.
/// </summary>
/// <param name="Name">Display name; must be unique.</param>
public record CreateCategoryCommand(string Name);
