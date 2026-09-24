namespace StockFlow.Domain.Tests;

/// <summary>
/// Unit tests for the <see cref="UnitOfMeasureExtensions"/> quantity rules.
/// </summary>
public class UnitOfMeasureExtensionsTests
{
    /// <summary>Countable units reject fractions; weight, volume and length units accept them.</summary>
    [Theory]
    [InlineData(UnitOfMeasure.Unit, false)]
    [InlineData(UnitOfMeasure.Dozen, false)]
    [InlineData(UnitOfMeasure.Box, false)]
    [InlineData(UnitOfMeasure.Pack, false)]
    [InlineData(UnitOfMeasure.Pound, true)]
    [InlineData(UnitOfMeasure.Kilogram, true)]
    [InlineData(UnitOfMeasure.Liter, true)]
    [InlineData(UnitOfMeasure.Meter, true)]
    public void AllowsFractions_ReturnsExpectedValue(UnitOfMeasure unit, bool expected)
    {
        // Act & Assert
        Assert.Equal(expected, unit.AllowsFractions());
    }

    /// <summary>IsValidQuantity accepts whole numbers for countable units and any value otherwise.</summary>
    [Theory]
    [InlineData(UnitOfMeasure.Unit, 3, true)]
    [InlineData(UnitOfMeasure.Unit, 3.5, false)]
    [InlineData(UnitOfMeasure.Box, 2.000, true)]
    [InlineData(UnitOfMeasure.Pound, 2.5, true)]
    public void IsValidQuantity_ReturnsExpectedValue(UnitOfMeasure unit, decimal quantity, bool expected)
    {
        // Act & Assert
        Assert.Equal(expected, unit.IsValidQuantity(quantity));
    }
}
