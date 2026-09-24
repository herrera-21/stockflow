namespace StockFlow.Domain;

/// <summary>
/// Business rules attached to <see cref="UnitOfMeasure"/> values.
/// </summary>
public static class UnitOfMeasureExtensions
{
    /// <summary>
    /// Whether quantities in the given unit may have decimals. Weight, volume and length units can be
    /// split (2.5 lb); countable units (unit, dozen, box, pack) only take whole quantities.
    /// </summary>
    /// <param name="unit">Unit to check.</param>
    /// <returns>True when fractional quantities are allowed.</returns>
    public static bool AllowsFractions(this UnitOfMeasure unit) =>
        unit is not (UnitOfMeasure.Unit or UnitOfMeasure.Dozen or UnitOfMeasure.Box or UnitOfMeasure.Pack);

    /// <summary>
    /// Whether the quantity is valid for the given unit: any value for fractional units, whole
    /// numbers only for countable ones.
    /// </summary>
    /// <param name="unit">Unit the quantity is expressed in.</param>
    /// <param name="quantity">Quantity to check.</param>
    /// <returns>True when the quantity fits the unit.</returns>
    public static bool IsValidQuantity(this UnitOfMeasure unit, decimal quantity) =>
        unit.AllowsFractions() || decimal.Truncate(quantity) == quantity;
}
