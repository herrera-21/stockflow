namespace StockFlow.Domain;

/// <summary>
/// Business reason behind a manual stock adjustment. Stored as text so the values stay readable in
/// the database.
/// </summary>
public enum InventoryAdjustmentReason
{
    /// <summary>Result of a physical count.</summary>
    PhysicalCount,

    /// <summary>Shrinkage or damage.</summary>
    Damage,

    /// <summary>Expired goods.</summary>
    Expiration,

    /// <summary>Correction of a previous mistake.</summary>
    Correction,

    /// <summary>Any other reason; a note is required when this value is used.</summary>
    Other
}
