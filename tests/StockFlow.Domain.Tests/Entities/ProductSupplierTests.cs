using StockFlow.Domain;
using StockFlow.Domain.Entities;
using StockFlow.Domain.Exceptions;

namespace StockFlow.Domain.Tests.Entities;

/// <summary>
/// Unit tests for <see cref="ProductSupplier"/> and for the product-side rules that keep at most one
/// preferred supplier per product.
/// </summary>
public class ProductSupplierTests
{
    // Builds a valid product for the association scenarios.
    private static Product CreateProduct() =>
        Product.Create("SKU-001", "Test product", null, CategoryIds.Other, UnitOfMeasure.Unit, UnitOfMeasure.Unit, 1m, 10m, 15m, 13m, 0, 0);

    /// <summary>Create with valid data sets the properties and is not preferred by default.</summary>
    [Fact]
    public void Create_WithValidData_SetsPropertiesAndIsNotPreferred()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var supplierId = Guid.NewGuid();

        // Act
        var association = ProductSupplier.Create(productId, supplierId, "  SUP-1  ", 8.5m);

        // Assert
        Assert.Equal(productId, association.ProductId);
        Assert.Equal(supplierId, association.SupplierId);
        Assert.Equal("SUP-1", association.SupplierSku);
        Assert.Equal(8.5m, association.PurchasePrice);
        Assert.False(association.IsPreferred);
    }

    /// <summary>Create with an empty product or supplier throws a <see cref="DomainException"/>.</summary>
    [Fact]
    public void Create_WithEmptyIds_ThrowsDomainException()
    {
        // Act & Assert
        Assert.Throws<DomainException>(() => ProductSupplier.Create(Guid.Empty, Guid.NewGuid(), null, null));
        Assert.Throws<DomainException>(() => ProductSupplier.Create(Guid.NewGuid(), Guid.Empty, null, null));
    }

    /// <summary>Create with a negative purchase price throws a <see cref="DomainException"/>.</summary>
    [Fact]
    public void Create_WithNegativePurchasePrice_ThrowsDomainException()
    {
        // Act & Assert
        Assert.Throws<DomainException>(() => ProductSupplier.Create(Guid.NewGuid(), Guid.NewGuid(), null, -1m));
    }

    /// <summary>Update changes the supplier-specific data and normalizes blank values to null.</summary>
    [Fact]
    public void Update_ChangesSupplierData()
    {
        // Arrange
        var association = ProductSupplier.Create(Guid.NewGuid(), Guid.NewGuid(), "SUP-1", 8.5m);

        // Act
        association.Update("   ", null);

        // Assert
        Assert.Null(association.SupplierSku);
        Assert.Null(association.PurchasePrice);
    }

    /// <summary>Update with a negative purchase price throws a <see cref="DomainException"/>.</summary>
    [Fact]
    public void Update_WithNegativePurchasePrice_ThrowsDomainException()
    {
        // Arrange
        var association = ProductSupplier.Create(Guid.NewGuid(), Guid.NewGuid(), null, null);

        // Act & Assert
        Assert.Throws<DomainException>(() => association.Update(null, -1m));
    }

    /// <summary>Assigning a supplier adds an association with its data.</summary>
    [Fact]
    public void AssignSupplier_AddsAssociation()
    {
        // Arrange
        var product = CreateProduct();
        var supplierId = Guid.NewGuid();

        // Act
        product.AssignSupplier(supplierId, "SUP-1", 8.5m, isPreferred: false);

        // Assert
        var association = Assert.Single(product.Suppliers);
        Assert.Equal(supplierId, association.SupplierId);
        Assert.Equal("SUP-1", association.SupplierSku);
        Assert.Equal(8.5m, association.PurchasePrice);
        Assert.False(association.IsPreferred);
    }

    /// <summary>Assigning the same supplier twice updates it instead of duplicating it.</summary>
    [Fact]
    public void AssignSupplier_WhenAlreadyAssigned_UpdatesInsteadOfDuplicating()
    {
        // Arrange
        var product = CreateProduct();
        var supplierId = Guid.NewGuid();
        product.AssignSupplier(supplierId, "SUP-1", 8.5m, isPreferred: false);

        // Act
        product.AssignSupplier(supplierId, "SUP-2", 9.0m, isPreferred: false);

        // Assert
        var association = Assert.Single(product.Suppliers);
        Assert.Equal("SUP-2", association.SupplierSku);
        Assert.Equal(9.0m, association.PurchasePrice);
    }

    /// <summary>Marking a supplier as preferred clears the flag from the previously preferred one.</summary>
    [Fact]
    public void AssignSupplier_AsPreferred_KeepsOnlyOnePreferred()
    {
        // Arrange
        var product = CreateProduct();
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        product.AssignSupplier(first, null, null, isPreferred: true);

        // Act
        product.AssignSupplier(second, null, null, isPreferred: true);

        // Assert
        Assert.False(product.Suppliers.Single(s => s.SupplierId == first).IsPreferred);
        Assert.True(product.Suppliers.Single(s => s.SupplierId == second).IsPreferred);
    }

    /// <summary>Assigning a preferred supplier as not preferred clears the flag.</summary>
    [Fact]
    public void AssignSupplier_WithoutPreferred_ClearsPreferredFlag()
    {
        // Arrange
        var product = CreateProduct();
        var supplierId = Guid.NewGuid();
        product.AssignSupplier(supplierId, null, null, isPreferred: true);

        // Act
        product.AssignSupplier(supplierId, null, null, isPreferred: false);

        // Assert
        Assert.False(product.Suppliers.Single().IsPreferred);
    }

    /// <summary>Marking an unassociated supplier as preferred throws a <see cref="DomainException"/>.</summary>
    [Fact]
    public void SetPreferredSupplier_WithUnassociatedSupplier_ThrowsDomainException()
    {
        // Arrange
        var product = CreateProduct();

        // Act & Assert
        Assert.Throws<DomainException>(() => product.SetPreferredSupplier(Guid.NewGuid()));
    }

    /// <summary>Removing an associated supplier drops it from the product.</summary>
    [Fact]
    public void RemoveSupplier_WhenAssociated_RemovesIt()
    {
        // Arrange
        var product = CreateProduct();
        var supplierId = Guid.NewGuid();
        product.AssignSupplier(supplierId, null, null, isPreferred: false);

        // Act
        product.RemoveSupplier(supplierId);

        // Assert
        Assert.Empty(product.Suppliers);
    }

    /// <summary>Removing an unassociated supplier is a no-op.</summary>
    [Fact]
    public void RemoveSupplier_WhenNotAssociated_DoesNothing()
    {
        // Arrange
        var product = CreateProduct();

        // Act
        product.RemoveSupplier(Guid.NewGuid());

        // Assert
        Assert.Empty(product.Suppliers);
    }
}
