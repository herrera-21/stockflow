using System.ComponentModel.DataAnnotations;

namespace StockFlow.Web.Pages.Categories;

/// <summary>
/// Form model for editing a category. Validation error messages are localization resource keys.
/// </summary>
public class UpdateCategoryInputModel
{
    /// <summary>Identifier of the category being edited.</summary>
    public Guid Id { get; set; }

    /// <summary>Display name; required and at most 100 characters.</summary>
    [Required(ErrorMessage = "ValidationRequired")]
    [StringLength(100, ErrorMessage = "ValidationStringLength")]
    [Display(Name = "CategoryName")]
    public string Name { get; set; } = string.Empty;
}
