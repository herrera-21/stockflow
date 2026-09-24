using StockFlow.Domain.Entities;

namespace StockFlow.Application.Categories;

/// <summary>
/// Maps category domain entities to their DTO representation.
/// </summary>
internal static class CategoryMapping
{
    /// <summary>Projects a <see cref="Category"/> into a <see cref="CategoryDto"/>.</summary>
    /// <param name="category">Category to project.</param>
    /// <param name="productCount">Number of products associated with the category.</param>
    /// <returns>The projected DTO.</returns>
    public static CategoryDto ToDto(this Category category, int productCount = 0) => new(
        category.Id,
        category.Code,
        category.Name,
        category.IsActive,
        productCount);
}
