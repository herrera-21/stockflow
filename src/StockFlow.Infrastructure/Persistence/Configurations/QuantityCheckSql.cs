using StockFlow.Domain;

namespace StockFlow.Infrastructure.Persistence.Configurations;

/// <summary>
/// Builds the SQL fragments shared by the stock check constraints. The countable-unit list is
/// derived from the domain rule so the database constraints and the domain invariants stay in sync.
/// </summary>
internal static class QuantityCheckSql
{
    /// <summary>
    /// Quoted SQL list of the units that only take whole quantities, derived from the domain rule.
    /// </summary>
    /// <returns>A comma-separated list of quoted unit names, for example <c>'Unit', 'Dozen'</c>.</returns>
    public static string CountableUnits() =>
        string.Join(", ", Enum.GetValues<UnitOfMeasure>()
            .Where(unit => !unit.AllowsFractions())
            .Select(unit => $"'{unit}'"));

    /// <summary>
    /// SQL check for a stock column: never negative and whole for countable base units.
    /// </summary>
    /// <param name="column">Name of the column to check.</param>
    /// <returns>The check expression.</returns>
    public static string NonNegativeWhole(string column) =>
        $"[{column}] >= 0 AND ([BaseUnit] NOT IN ({CountableUnits()}) OR [{column}] = FLOOR([{column}]))";

    /// <summary>
    /// SQL check for a quantity column: positive and whole for countable base units.
    /// </summary>
    /// <param name="column">Name of the column to check.</param>
    /// <returns>The check expression.</returns>
    public static string PositiveWhole(string column) =>
        $"[{column}] > 0 AND ([BaseUnit] NOT IN ({CountableUnits()}) OR [{column}] = FLOOR([{column}]))";
}
