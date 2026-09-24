using StockFlow.Domain;
using StockFlow.Domain.Entities;
using StockFlow.Domain.Exceptions;

namespace StockFlow.Domain.Tests.Entities;

/// <summary>
/// Unit tests for the <see cref="Product"/> aggregate invariants.
/// </summary>
public class ProductTests
{
    // Builds a valid product, letting each test override only the values it cares about.
    private static Product CreateProduct(
        decimal purchasePrice = 10m,
        decimal salePrice = 15m,
        decimal taxRate = 13m,
        decimal initialStock = 5,
        decimal minimumStock = 2,
        UnitOfMeasure baseUnit = UnitOfMeasure.Unit,
        UnitOfMeasure purchaseUnit = UnitOfMeasure.Unit,
        decimal purchaseUnitFactor = 1m) =>
        Product.Create(
            "SKU-001",
            "Test product",
            "A product used in tests",
            CategoryIds.Other,
            baseUnit,
            purchaseUnit,
            purchaseUnitFactor,
            purchasePrice,
            salePrice,
            taxRate,
            initialStock,
            minimumStock);

    /// <summary>Create with valid data sets the properties and marks the product active.</summary>
    [Fact]
    public void Create_WithValidData_SetsPropertiesAndActiveState()
    {
        // Act
        var product = CreateProduct();

        // Assert
        Assert.NotEqual(Guid.Empty, product.Id);
        Assert.Equal("SKU-001", product.Sku);
        Assert.Equal("Test product", product.Name);
        Assert.Equal(CategoryIds.Other, product.CategoryId);
        Assert.Equal(UnitOfMeasure.Unit, product.BaseUnit);
        Assert.Equal(UnitOfMeasure.Unit, product.PurchaseUnit);
        Assert.Equal(1m, product.PurchaseUnitFactor);
        Assert.Equal(10m, product.PurchasePrice);
        Assert.Equal(15m, product.SalePrice);
        Assert.Equal(13m, product.TaxRate);
        Assert.Equal(5m, product.CurrentStock);
        Assert.Equal(2m, product.MinimumStock);
        Assert.True(product.IsActive);
    }

    /// <summary>Create with a blank SKU throws a <see cref="DomainException"/>.</summary>
    [Fact]
    public void Create_WithBlankSku_ThrowsDomainException()
    {
        // Act & Assert
        Assert.Throws<DomainException>(() => Product.Create(
            " ", "Test product", null, CategoryIds.Other, UnitOfMeasure.Unit, UnitOfMeasure.Unit, 1m, 10m, 15m, 13m, 0, 0));
    }

    /// <summary>Create with out-of-range prices or tax rate throws a <see cref="DomainException"/>.</summary>
    [Theory]
    [InlineData(-1, 15, 13)]
    [InlineData(10, -1, 13)]
    [InlineData(10, 15, -1)]
    [InlineData(10, 15, 101)]
    public void Create_WithInvalidNumbers_ThrowsDomainException(
        decimal purchasePrice, decimal salePrice, decimal taxRate)
    {
        // Act & Assert
        Assert.Throws<DomainException>(() => CreateProduct(purchasePrice, salePrice, taxRate));
    }

    /// <summary>Create with a negative initial stock throws a <see cref="DomainException"/>.</summary>
    [Fact]
    public void Create_WithNegativeInitialStock_ThrowsDomainException()
    {
        // Act & Assert
        Assert.Throws<DomainException>(() => CreateProduct(initialStock: -1));
    }

    /// <summary>HasLowStock is true when current stock is at or below the minimum.</summary>
    [Fact]
    public void HasLowStock_WhenCurrentAtOrBelowMinimum_IsTrue()
    {
        // Arrange
        var atMinimum = CreateProduct(initialStock: 2, minimumStock: 2);
        var belowMinimum = CreateProduct(initialStock: 1, minimumStock: 2);

        // Assert
        Assert.True(atMinimum.HasLowStock);
        Assert.True(belowMinimum.HasLowStock);
    }

    /// <summary>HasLowStock is false when current stock is above the minimum.</summary>
    [Fact]
    public void HasLowStock_WhenCurrentAboveMinimum_IsFalse()
    {
        // Arrange
        var product = CreateProduct(initialStock: 10, minimumStock: 2);

        // Assert
        Assert.False(product.HasLowStock);
    }

    /// <summary>Update changes editable data but leaves current stock untouched.</summary>
    [Fact]
    public void Update_ChangesEditableDataButNotCurrentStock()
    {
        // Arrange
        var product = CreateProduct(initialStock: 5, minimumStock: 2);

        // Act
        product.Update(
            "SKU-002", "Renamed", null, CategoryIds.Food,
            UnitOfMeasure.Unit, UnitOfMeasure.Box, 12m, 20m, 30m, 8m, 4);

        // Assert
        Assert.Equal("SKU-002", product.Sku);
        Assert.Equal("Renamed", product.Name);
        Assert.Null(product.Description);
        Assert.Equal(CategoryIds.Food, product.CategoryId);
        Assert.Equal(UnitOfMeasure.Box, product.PurchaseUnit);
        Assert.Equal(12m, product.PurchaseUnitFactor);
        Assert.Equal(20m, product.PurchasePrice);
        Assert.Equal(30m, product.SalePrice);
        Assert.Equal(8m, product.TaxRate);
        Assert.Equal(4m, product.MinimumStock);
        Assert.Equal(5m, product.CurrentStock);
    }

    /// <summary>Deactivate then Activate toggles the active state.</summary>
    [Fact]
    public void Deactivate_ThenActivate_TogglesActiveState()
    {
        // Arrange
        var product = CreateProduct();

        // Act
        product.Deactivate();
        var afterDeactivate = product.IsActive;
        product.Activate();

        // Assert
        Assert.False(afterDeactivate);
        Assert.True(product.IsActive);
    }

    /// <summary>Create with a box of 24 units sets the units, the factor and the derived unit cost.</summary>
    [Fact]
    public void Create_WithPurchaseUnitAndFactor_SetsUnitsAndUnitCost()
    {
        // Act
        var product = CreateProduct(
            purchasePrice: 12m,
            purchaseUnit: UnitOfMeasure.Box,
            purchaseUnitFactor: 24m);

        // Assert
        Assert.Equal(UnitOfMeasure.Unit, product.BaseUnit);
        Assert.Equal(UnitOfMeasure.Box, product.PurchaseUnit);
        Assert.Equal(24m, product.PurchaseUnitFactor);
        Assert.Equal(0.5m, product.UnitCost);
    }

    /// <summary>Create with a zero or negative factor throws a <see cref="DomainException"/>.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithNonPositiveFactor_ThrowsDomainException(decimal factor)
    {
        // Act & Assert
        Assert.Throws<DomainException>(() =>
            CreateProduct(purchaseUnit: UnitOfMeasure.Box, purchaseUnitFactor: factor));
    }

    /// <summary>Create with the same base and purchase unit and a factor other than 1 throws.</summary>
    [Fact]
    public void Create_WithSameUnitsAndFactorOtherThanOne_ThrowsDomainException()
    {
        // Act & Assert
        Assert.Throws<DomainException>(() => CreateProduct(purchaseUnitFactor: 2m));
    }

    /// <summary>Create with a fractional factor for a countable base unit throws.</summary>
    [Fact]
    public void Create_WithFractionalFactorForCountableUnit_ThrowsDomainException()
    {
        // Act & Assert
        Assert.Throws<DomainException>(() =>
            CreateProduct(purchaseUnit: UnitOfMeasure.Box, purchaseUnitFactor: 2.5m));
    }

    /// <summary>Create with fractional stock for a countable base unit throws.</summary>
    [Theory]
    [InlineData(2.5, 0)]
    [InlineData(2, 0.5)]
    public void Create_WithFractionalStockForCountableUnit_ThrowsDomainException(
        decimal initialStock, decimal minimumStock)
    {
        // Act & Assert
        Assert.Throws<DomainException>(() =>
            CreateProduct(initialStock: initialStock, minimumStock: minimumStock));
    }

    /// <summary>Create with fractional stock and factor for a weight base unit succeeds.</summary>
    [Fact]
    public void Create_WithFractionalStockForWeightUnit_Succeeds()
    {
        // Act
        var product = CreateProduct(
            initialStock: 2.5m,
            minimumStock: 0.75m,
            baseUnit: UnitOfMeasure.Pound,
            purchaseUnit: UnitOfMeasure.Kilogram,
            purchaseUnitFactor: 2.2046m);

        // Assert
        Assert.Equal(UnitOfMeasure.Pound, product.BaseUnit);
        Assert.Equal(2.5m, product.CurrentStock);
        Assert.Equal(0.75m, product.MinimumStock);
    }

    /// <summary>Create with an undefined unit value throws a <see cref="DomainException"/>.</summary>
    [Fact]
    public void Create_WithUndefinedUnit_ThrowsDomainException()
    {
        // Act & Assert
        Assert.Throws<DomainException>(() => CreateProduct(baseUnit: (UnitOfMeasure)999));
    }

    /// <summary>Update rejects a base unit change while the product has stock.</summary>
    [Fact]
    public void Update_ChangingBaseUnitWithStock_ThrowsDomainException()
    {
        // Arrange
        var product = CreateProduct(initialStock: 5);

        // Act & Assert
        Assert.False(product.CanChangeBaseUnitTo(UnitOfMeasure.Pound));
        Assert.Throws<DomainException>(() => product.Update(
            "SKU-001", "Test product", null, CategoryIds.Other,
            UnitOfMeasure.Pound, UnitOfMeasure.Pound, 1m, 10m, 15m, 13m, 2));
    }

    /// <summary>Update allows a base unit change when the product has no stock.</summary>
    [Fact]
    public void Update_ChangingBaseUnitWithoutStock_ChangesUnit()
    {
        // Arrange
        var product = CreateProduct(initialStock: 0);

        // Act
        product.Update(
            "SKU-001", "Test product", null, CategoryIds.Other,
            UnitOfMeasure.Pound, UnitOfMeasure.Pound, 1m, 10m, 15m, 13m, 1.5m);

        // Assert
        Assert.Equal(UnitOfMeasure.Pound, product.BaseUnit);
        Assert.Equal(1.5m, product.MinimumStock);
    }
}
