using System.ComponentModel.DataAnnotations;

namespace StockFlow.Web.Pages.Suppliers;

/// <summary>
/// Form model for associating a product with a supplier. Validation error messages are localization
/// resource keys.
/// </summary>
public class AssignSupplierProductInputModel
{
    /// <summary>Identifier of the product to associate; required.</summary>
    [Required(ErrorMessage = "ValidationRequired")]
    [Display(Name = "Product")]
    public Guid ProductId { get; set; }

    /// <summary>Optional reference the supplier uses for the product; at most 100 characters.</summary>
    [StringLength(100, ErrorMessage = "ValidationStringLength")]
    [Display(Name = "SupplierSku")]
    public string? SupplierSku { get; set; }

    /// <summary>Optional purchase price agreed with the supplier; between 0 and 1,000,000,000.</summary>
    [Range(typeof(decimal), "0", "1000000000", ErrorMessage = "ValidationRange")]
    [Display(Name = "PurchasePrice")]
    public decimal? PurchasePrice { get; set; }

    /// <summary>Whether this supplier becomes the preferred one for the product.</summary>
    [Display(Name = "PreferredSupplier")]
    public bool IsPreferred { get; set; }
}
