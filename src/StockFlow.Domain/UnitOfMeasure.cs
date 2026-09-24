namespace StockFlow.Domain;

/// <summary>
/// Unit in which a product is stocked, sold or purchased. Stored as text so the values stay readable
/// in the database.
/// </summary>
public enum UnitOfMeasure
{
    /// <summary>A single piece.</summary>
    Unit,

    /// <summary>A dozen pieces.</summary>
    Dozen,

    /// <summary>A box; its content is defined by the conversion factor.</summary>
    Box,

    /// <summary>A pack; its content is defined by the conversion factor.</summary>
    Pack,

    /// <summary>Pound (weight).</summary>
    Pound,

    /// <summary>Ounce (weight).</summary>
    Ounce,

    /// <summary>Kilogram (weight).</summary>
    Kilogram,

    /// <summary>Gram (weight).</summary>
    Gram,

    /// <summary>Liter (volume).</summary>
    Liter,

    /// <summary>Milliliter (volume).</summary>
    Milliliter,

    /// <summary>Gallon (volume).</summary>
    Gallon,

    /// <summary>Meter (length).</summary>
    Meter
}
