using System.ComponentModel.DataAnnotations;
using StockFlow.Domain;
using StockFlow.Web.Validation;

namespace StockFlow.Web.Pages.Products;

/// <summary>
/// Form model for creating a product. Validation error messages are localization resource keys.
/// </summary>
public class CreateProductInputModel
{
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

    /// <summary>Category identifier; required.</summary>
    [Required(ErrorMessage = "ValidationRequired")]
    [Display(Name = "Category")]
    public Guid? CategoryId { get; set; }

    /// <summary>Unit for stock and sale price; required.</summary>
    [Required(ErrorMessage = "ValidationRequired")]
    [Display(Name = "BaseUnit")]
    public UnitOfMeasure? BaseUnit { get; set; }

    /// <summary>Unit used when buying from suppliers; required.</summary>
    [Required(ErrorMessage = "ValidationRequired")]
    [Display(Name = "PurchaseUnit")]
    public UnitOfMeasure? PurchaseUnit { get; set; }

    /// <summary>
    /// Base units per purchase unit; required, between 0.0001 and 1,000,000, 1 when both units match
    /// and whole for countable base units.
    /// </summary>
    [Required(ErrorMessage = "ValidationRequired")]
    [Range(typeof(decimal), "0.0001", "1000000", ErrorMessage = "ValidationRange")]
    [PurchaseUnitFactor(nameof(BaseUnit), nameof(PurchaseUnit), ErrorMessage = "ValidationSameUnitFactor")]
    [QuantityForUnit(nameof(BaseUnit), ErrorMessage = "ValidationWholeQuantity")]
    [Display(Name = "PurchaseUnitFactor")]
    public decimal? PurchaseUnitFactor { get; set; }

    /// <summary>Supplier price per purchase unit; required and between 0 and 1,000,000,000.</summary>
    [Required(ErrorMessage = "ValidationRequired")]
    [Range(typeof(decimal), "0", "1000000000", ErrorMessage = "ValidationRange")]
    [Display(Name = "PurchasePrice")]
    public decimal? PurchasePrice { get; set; }

    /// <summary>Customer price per base unit; required and between 0 and 1,000,000,000.</summary>
    [Required(ErrorMessage = "ValidationRequired")]
    [Range(typeof(decimal), "0", "1000000000", ErrorMessage = "ValidationRange")]
    [Display(Name = "SalePrice")]
    public decimal? SalePrice { get; set; }

    /// <summary>Tax percentage; required and between 0 and 100.</summary>
    [Required(ErrorMessage = "ValidationRequired")]
    [Range(typeof(decimal), "0", "100", ErrorMessage = "ValidationRange")]
    [Display(Name = "TaxRate")]
    public decimal? TaxRate { get; set; }

    /// <summary>
    /// Opening stock in base units; required, between 0 and 1,000,000,000 and whole for
    /// countable units.
    /// </summary>
    [Required(ErrorMessage = "ValidationRequired")]
    [Range(typeof(decimal), "0", "1000000000", ErrorMessage = "ValidationRange")]
    [QuantityForUnit(nameof(BaseUnit), ErrorMessage = "ValidationWholeQuantity")]
    [Display(Name = "InitialStock")]
    public decimal? InitialStock { get; set; }

    /// <summary>
    /// Low-stock threshold in base units; required, between 0 and 1,000,000,000 and whole for
    /// countable units.
    /// </summary>
    [Required(ErrorMessage = "ValidationRequired")]
    [Range(typeof(decimal), "0", "1000000000", ErrorMessage = "ValidationRange")]
    [QuantityForUnit(nameof(BaseUnit), ErrorMessage = "ValidationWholeQuantity")]
    [Display(Name = "MinimumStock")]
    public decimal? MinimumStock { get; set; }
}
