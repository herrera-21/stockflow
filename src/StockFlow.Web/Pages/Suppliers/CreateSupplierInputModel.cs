using System.ComponentModel.DataAnnotations;

namespace StockFlow.Web.Pages.Suppliers;

/// <summary>
/// Form model for creating a supplier. Validation error messages are localization resource keys.
/// </summary>
public class CreateSupplierInputModel
{
    /// <summary>Company name; required and at most 200 characters.</summary>
    [Required(ErrorMessage = "ValidationRequired")]
    [StringLength(200, ErrorMessage = "ValidationStringLength")]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Tax identification; required and at most 50 characters.</summary>
    [Required(ErrorMessage = "ValidationRequired")]
    [StringLength(50, ErrorMessage = "ValidationStringLength")]
    [Display(Name = "TaxId")]
    public string TaxId { get; set; } = string.Empty;

    /// <summary>Optional contact person name; at most 200 characters.</summary>
    [StringLength(200, ErrorMessage = "ValidationStringLength")]
    [Display(Name = "ContactName")]
    public string? ContactName { get; set; }

    /// <summary>Optional phone number; at most 50 characters.</summary>
    [StringLength(50, ErrorMessage = "ValidationStringLength")]
    [Display(Name = "Phone")]
    public string? Phone { get; set; }

    /// <summary>Optional email address; at most 200 characters.</summary>
    [StringLength(200, ErrorMessage = "ValidationStringLength")]
    [EmailAddress(ErrorMessage = "ValidationEmail")]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    /// <summary>Optional postal address; at most 300 characters.</summary>
    [StringLength(300, ErrorMessage = "ValidationStringLength")]
    [Display(Name = "Address")]
    public string? Address { get; set; }
}
