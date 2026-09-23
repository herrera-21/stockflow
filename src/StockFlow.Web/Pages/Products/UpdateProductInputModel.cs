using System.ComponentModel.DataAnnotations;

namespace StockFlow.Web.Pages.Products;

/// <summary>
/// Form model for editing a product. Validation error messages are localization resource keys.
/// </summary>
public class UpdateProductInputModel
{
    /// <summary>Identifier of the product being edited.</summary>
    public Guid Id { get; set; }

    /// <summary>Stock keeping unit; required and at most 50 characters.</summary>
    [Required(ErrorMessage = "ValidationRequired")]
    [StringLength(50, ErrorMessage = "ValidationStringLength")]
    [Display(Name = "Sku")]
    public string Sku { get; set; } = string.Empty;

    /// <summary>Display name; required and at most 200 characters.</summary>
    [Required(ErrorMessage = "ValidationRequired")]
    [StringLength(200, ErrorMessage = "ValidationStringLength")]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional description; at most 1000 characters.</summary>
    [StringLength(1000, ErrorMessage = "ValidationStringLength")]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    /// <summary>Category; required and at most 100 characters.</summary>
    [Required(ErrorMessage = "ValidationRequired")]
    [StringLength(100, ErrorMessage = "ValidationStringLength")]
    [Display(Name = "Category")]
    public string Category { get; set; } = string.Empty;

    /// <summary>Supplier price; between 0 and 1,000,000,000.</summary>
    [Range(typeof(decimal), "0", "1000000000", ErrorMessage = "ValidationRange")]
    [Display(Name = "PurchasePrice")]
    public decimal PurchasePrice { get; set; }

    /// <summary>Customer price; between 0 and 1,000,000,000.</summary>
    [Range(typeof(decimal), "0", "1000000000", ErrorMessage = "ValidationRange")]
    [Display(Name = "SalePrice")]
    public decimal SalePrice { get; set; }

    /// <summary>Tax percentage; between 0 and 100.</summary>
    [Range(typeof(decimal), "0", "100", ErrorMessage = "ValidationRange")]
    [Display(Name = "TaxRate")]
    public decimal TaxRate { get; set; }

    /// <summary>Low-stock threshold; between 0 and 1,000,000,000.</summary>
    [Range(0, 1000000000, ErrorMessage = "ValidationRange")]
    [Display(Name = "MinimumStock")]
    public int MinimumStock { get; set; }
}
