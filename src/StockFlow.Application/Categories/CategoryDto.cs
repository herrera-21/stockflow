namespace StockFlow.Application.Categories;

/// <summary>
/// Category data transferred across the Application boundary.
/// </summary>
/// <param name="Id">Unique identifier.</param>
/// <param name="Code">Stable code of a seeded category; null for user-created ones.</param>
/// <param name="Name">Display name.</param>
/// <param name="IsActive">Whether the category is active.</param>
/// <param name="ProductCount">Number of products associated with the category.</param>
public record CategoryDto(
    Guid Id,
    string? Code,
    string Name,
    bool IsActive,
    int ProductCount);
