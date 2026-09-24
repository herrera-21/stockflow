using System.ComponentModel.DataAnnotations;
using StockFlow.Domain;

namespace StockFlow.Web.Pages.Products;

/// <summary>
/// Form model for adjusting a product's stock. Validation error messages are localization resource
/// keys. The product is loaded server-side so the adjustment can be checked against its base unit.
/// </summary>
public class AdjustStockInputModel
{
    /// <summary>Identifier of the product being adjusted; required.</summary>
    [Required(ErrorMessage = "ValidationRequired")]
    public Guid? ProductId { get; set; }

    /// <summary>Adjustment direction: positive or negative; required.</summary>
    [Required(ErrorMessage = "ValidationRequired")]
    [Display(Name = "MovementType")]
    public InventoryMovementType? Type { get; set; }

    /// <summary>Positive moved quantity in base units; required and greater than zero.</summary>
    [Required(ErrorMessage = "ValidationRequired")]
    [Range(typeof(decimal), "0.001", "1000000000", ErrorMessage = "ValidationRange")]
    [Display(Name = "Quantity")]
    public decimal? Quantity { get; set; }

    /// <summary>Reason for the adjustment; required.</summary>
    [Required(ErrorMessage = "ValidationRequired")]
    [Display(Name = "AdjustmentReason")]
    public InventoryAdjustmentReason? Reason { get; set; }

    /// <summary>Optional note; required when the reason is "other".</summary>
    [StringLength(1000, ErrorMessage = "ValidationStringLength")]
    [Display(Name = "Note")]
    public string? Note { get; set; }
}
