using StockFlow.Domain.Exceptions;

namespace StockFlow.Domain.Entities;

/// <summary>
/// Product category used to standardize the catalog so the same category is not entered twice with
/// different casing or spelling. Categories are never deleted physically; they are deactivated.
/// </summary>
public class Category
{
    // EF Core materialization constructor.
    private Category()
    {
    }

    private Category(Guid id, string? code, string name, bool isActive)
    {
        Id = id;
        Code = code;
        Name = name;
        IsActive = isActive;
    }

    /// <summary>Unique identifier (UUID v7).</summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Stable code of a seeded category, used to look up its localized display name. Null for
    /// categories created by a user, which are shown with <see cref="Name"/>.
    /// </summary>
    public string? Code { get; private set; }

    /// <summary>Display name; unique across categories.</summary>
    public string Name { get; private set; } = null!;

    /// <summary>Whether the category is active (soft-delete flag).</summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Creates a new active category.
    /// </summary>
    /// <param name="name">Display name; must not be blank.</param>
    /// <param name="code">Optional stable code for seeded categories.</param>
    /// <returns>The created category.</returns>
    /// <exception cref="DomainException">When the name is blank.</exception>
    public static Category Create(string name, string? code = null)
    {
        ValidateName(name);

        return new Category(Guid.CreateVersion7(), Normalize(code), name.Trim(), isActive: true);
    }

    /// <summary>
    /// Renames the category.
    /// </summary>
    /// <param name="name">New display name; must not be blank.</param>
    /// <exception cref="DomainException">When the name is blank.</exception>
    public void Rename(string name)
    {
        ValidateName(name);
        Name = name.Trim();
    }

    /// <summary>
    /// Deactivates the category. This is a soft delete: categories are never removed physically.
    /// </summary>
    public void Deactivate() => IsActive = false;

    /// <summary>Reactivates a previously deactivated category.</summary>
    public void Activate() => IsActive = true;

    // Ensures the category name is not blank.
    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Category name is required.");
        }
    }

    // Trims the value and turns blank strings into null.
    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
