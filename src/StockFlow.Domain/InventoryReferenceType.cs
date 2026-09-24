namespace StockFlow.Domain;

/// <summary>
/// Document a movement refers to, used by purchases and sales in later phases. Null for movements
/// that are not tied to a document, such as the opening balance or a manual adjustment. A sale
/// cancellation references the sale itself: the movement type already says it is a reversal.
/// </summary>
public enum InventoryReferenceType
{
    /// <summary>A purchase order.</summary>
    Purchase,

    /// <summary>A sale, for both its outbound movements and the reversal when it is cancelled.</summary>
    Sale
}
