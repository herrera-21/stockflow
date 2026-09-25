namespace StockFlow.Domain;

/// <summary>
/// Lifecycle of a purchase order. Stored as text so the values stay readable in the database.
/// A purchase is drafted, confirmed, and then received into stock; it can be cancelled while it has
/// not been received.
/// </summary>
public enum PurchaseStatus
{
    /// <summary>Editable draft. Lines can be added, updated or removed.</summary>
    Draft,

    /// <summary>Confirmed order. It can no longer be edited and is waiting to be received.</summary>
    Confirmed,

    /// <summary>Received order. Stock has been booked and the order is final.</summary>
    Received,

    /// <summary>Cancelled order. Allowed while it has not been received.</summary>
    Cancelled
}
