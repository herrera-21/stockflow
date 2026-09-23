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
        int initialStock = 5,
        int minimumStock = 2) =>
        Product.Create(
            "SKU-001",
            "Test product",
            "A product used in tests",
            "General",
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
        Assert.Equal("General", product.Category);
        Assert.Equal(10m, product.PurchasePrice);
        Assert.Equal(15m, product.SalePrice);
        Assert.Equal(13m, product.TaxRate);
        Assert.Equal(5, product.CurrentStock);
        Assert.Equal(2, product.MinimumStock);
        Assert.True(product.IsActive);
    }

    /// <summary>Create with a blank SKU throws a <see cref="DomainException"/>.</summary>
    [Fact]
    public void Create_WithBlankSku_ThrowsDomainException()
    {
        // Act & Assert
        Assert.Throws<DomainException>(() => Product.Create(
            " ", "Test product", null, "General", 10m, 15m, 13m, 0, 0));
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
        product.Update("SKU-002", "Renamed", null, "Hardware", 20m, 30m, 8m, 4);

        // Assert
        Assert.Equal("SKU-002", product.Sku);
        Assert.Equal("Renamed", product.Name);
        Assert.Null(product.Description);
        Assert.Equal("Hardware", product.Category);
        Assert.Equal(20m, product.PurchasePrice);
        Assert.Equal(30m, product.SalePrice);
        Assert.Equal(8m, product.TaxRate);
        Assert.Equal(4, product.MinimumStock);
        Assert.Equal(5, product.CurrentStock);
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
}
