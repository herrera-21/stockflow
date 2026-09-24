namespace StockFlow.Application.Categories.Queries;

/// <summary>
/// Query that retrieves a single category by its identifier.
/// </summary>
/// <param name="Id">Identifier of the category.</param>
public record GetCategoryByIdQuery(Guid Id);
