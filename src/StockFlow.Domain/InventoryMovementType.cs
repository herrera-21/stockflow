namespace StockFlow.Domain;

/// <summary>
/// Reason why a stock quantity changed. Stored as text so the values stay readable in the database.
/// The type also determines the direction of the movement (inbound increases stock, outbound
/// decreases it).
/// </summary>
public enum InventoryMovementType
{
    /// <summary>Opening balance recorded when a product is created with stock on hand.</summary>
    InitialBalance,

    /// <summary>Stock received from a supplier (purchases).</summary>
    PurchaseIn,

    /// <summary>Stock shipped to a customer (sales).</summary>
    SaleOut,

    /// <summary>Reversal of a previously confirmed sale.</summary>
    SaleCancellation,

    /// <summary>Manual positive adjustment (physical count surplus, correction).</summary>
    AdjustmentIncrease,

    /// <summary>Manual negative adjustment (shrinkage, damage, expiration, correction).</summary>
    AdjustmentDecrease,

    /// <summary>Stock returned by a customer.</summary>
    CustomerReturn,

    /// <summary>Stock returned to a supplier.</summary>
    SupplierReturn
}
