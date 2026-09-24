using System.ComponentModel.DataAnnotations;
using StockFlow.Domain;
using StockFlow.Web.Validation;

namespace StockFlow.Web.Pages.Customers;

/// <summary>
/// Form model for creating a customer. Validation error messages are localization resource keys.
/// </summary>
public class CreateCustomerInputModel
{
    /// <summary>Display name of the person or company; required and at most 200 characters.</summary>
    [Required(ErrorMessage = "ValidationRequired")]
    [StringLength(200, ErrorMessage = "ValidationStringLength")]
    [Display(Name = "FullName")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Type of the identity document; required.</summary>
    [Required(ErrorMessage = "ValidationRequired")]
    [Display(Name = "DocumentType")]
    public DocumentType? DocumentType { get; set; }

    /// <summary>Identity document number; required and must match the selected type.</summary>
    [Required(ErrorMessage = "ValidationRequired")]
    [StringLength(50, ErrorMessage = "ValidationStringLength")]
    [DocumentNumber(nameof(DocumentType), ErrorMessage = "ValidationDocument")]
    [Display(Name = "TaxId")]
    public string TaxId { get; set; } = string.Empty;

    /// <summary>Optional phone number; at most 50 characters.</summary>
    [StringLength(50, ErrorMessage = "ValidationStringLength")]
    [PhoneNumber(ErrorMessage = "ValidationPhone")]
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
