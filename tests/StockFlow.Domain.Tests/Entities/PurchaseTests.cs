using StockFlow.Domain;
using StockFlow.Domain.Entities;
using StockFlow.Domain.Exceptions;

namespace StockFlow.Domain.Tests.Entities;

/// <summary>
/// Unit tests for the <see cref="Purchase"/> aggregate: lifecycle transitions, line rules and the
/// unit snapshot.
/// </summary>
public class PurchaseTests
{
    // Fixed supplier used by every purchase in these tests.
    private static readonly Guid SupplierId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    /// <summary>Create with valid data starts the order as an editable draft.</summary>
    [Fact]
    public void Create_WithValidData_StartsAsDraft()
    {
        // Act
        var purchase = CreatePurchase();

        // Assert
        Assert.NotEqual(Guid.Empty, purchase.Id);
        Assert.Equal(SupplierId, purchase.SupplierId);
        Assert.Equal("OC-000001", purchase.Number);
        Assert.Equal(PurchaseStatus.Draft, purchase.Status);
        Assert.Empty(purchase.Lines);
        Assert.Equal(0m, purchase.Total);
    }

    /// <summary>Create without a supplier throws a <see cref="DomainException"/>.</summary>
    [Fact]
    public void Create_WithEmptySupplier_ThrowsDomainException()
    {
        // Act & Assert
        Assert.Throws<DomainException>(() => Purchase.Create(
            Guid.Empty, "OC-000001", DateTimeOffset.UnixEpoch, null, "user-1", "user@test.local"));
    }

    /// <summary>Create with a blank number throws a <see cref="DomainException"/>.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Create_WithBlankNumber_ThrowsDomainException(string number)
    {
        // Act & Assert
        Assert.Throws<DomainException>(() => Purchase.Create(
            SupplierId, number, DateTimeOffset.UnixEpoch, null, "user-1", "user@test.local"));
    }

    /// <summary>Create without a creating user throws a <see cref="DomainException"/>.</summary>
    [Theory]
    [InlineData("", "user@test.local")]
    [InlineData("user-1", "")]
    public void Create_WithoutCreatingUser_ThrowsDomainException(string userId, string userName)
    {
        // Act & Assert
        Assert.Throws<DomainException>(() => Purchase.Create(
            SupplierId, "OC-000001", DateTimeOffset.UnixEpoch, null, userId, userName));
    }

    /// <summary>Adding a line snapshots the product units and computes the line subtotal.</summary>
    [Fact]
    public void AddLine_SnapshotsUnitsAndComputesSubtotal()
    {
        // Arrange
        var purchase = CreatePurchase();
        var product = CreateProduct(purchaseUnit: UnitOfMeasure.Box, purchaseUnitFactor: 24m);

        // Act
        var line = purchase.AddLine(product, 2m, 19.50m);

        // Assert
        Assert.Equal(product.Id, line.ProductId);
        Assert.Equal(2m, line.Quantity);
        Assert.Equal(19.50m, line.PurchaseUnitCost);
        Assert.Equal(UnitOfMeasure.Box, line.PurchaseUnit);
        Assert.Equal(UnitOfMeasure.Unit, line.BaseUnit);
        Assert.Equal(24m, line.PurchaseUnitFactor);
        Assert.Equal(39.00m, line.Subtotal);
        Assert.Single(purchase.Lines);
        Assert.Equal(39.00m, purchase.Total);
    }

    /// <summary>A later product change does not rewrite the snapshotted line.</summary>
    [Fact]
    public void AddLine_Snapshot_IsNotAffectedByLaterProductChanges()
    {
        // Arrange
        var purchase = CreatePurchase();
        var product = CreateProduct(purchaseUnit: UnitOfMeasure.Box, purchaseUnitFactor: 24m);
        var line = purchase.AddLine(product, 1m, 10m);

        // Act
        product.Update(
            "SKU-001", "Renamed", null, CategoryIds.Other,
            UnitOfMeasure.Unit, UnitOfMeasure.Box, 12m, 20m, 15m, 13m, 0);

        // Assert
        Assert.Equal(24m, line.PurchaseUnitFactor);
        Assert.Equal(UnitOfMeasure.Box, line.PurchaseUnit);
    }

    /// <summary>A zero or negative quantity throws a <see cref="DomainException"/>.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddLine_WithNonPositiveQuantity_ThrowsDomainException(decimal quantity)
    {
        // Arrange
        var purchase = CreatePurchase();
        var product = CreateProduct();

        // Act & Assert
        Assert.Throws<DomainException>(() => purchase.AddLine(product, quantity, 10m));
    }

    /// <summary>A fractional quantity for a countable purchase unit throws a <see cref="DomainException"/>.</summary>
    [Fact]
    public void AddLine_WithFractionalQuantityForCountableUnit_ThrowsDomainException()
    {
        // Arrange
        var purchase = CreatePurchase();
        var product = CreateProduct(purchaseUnit: UnitOfMeasure.Box, purchaseUnitFactor: 24m);

        // Act & Assert
        Assert.Throws<DomainException>(() => purchase.AddLine(product, 1.5m, 10m));
    }

    /// <summary>A fractional quantity is accepted when the purchase unit allows fractions.</summary>
    [Fact]
    public void AddLine_WithFractionalQuantityForFractionalUnit_Succeeds()
    {
        // Arrange
        var purchase = CreatePurchase();
        var product = CreateProduct(baseUnit: UnitOfMeasure.Pound, purchaseUnit: UnitOfMeasure.Pound);

        // Act
        var line = purchase.AddLine(product, 2.5m, 10m);

        // Assert
        Assert.Equal(2.5m, line.Quantity);
        Assert.Equal(25m, line.Subtotal);
    }

    /// <summary>A negative cost throws a <see cref="DomainException"/>.</summary>
    [Fact]
    public void AddLine_WithNegativeCost_ThrowsDomainException()
    {
        // Arrange
        var purchase = CreatePurchase();
        var product = CreateProduct();

        // Act & Assert
        Assert.Throws<DomainException>(() => purchase.AddLine(product, 1m, -0.01m));
    }

    /// <summary>The same product cannot be added twice to one order.</summary>
    [Fact]
    public void AddLine_WithDuplicateProduct_ThrowsDomainException()
    {
        // Arrange
        var purchase = CreatePurchase();
        var product = CreateProduct();
        purchase.AddLine(product, 1m, 10m);

        // Act & Assert
        Assert.Throws<DomainException>(() => purchase.AddLine(product, 2m, 10m));
    }

    /// <summary>Updating a line changes quantity, cost and subtotal, keeping the snapshot.</summary>
    [Fact]
    public void UpdateLine_ChangesQuantityCostAndSubtotal()
    {
        // Arrange
        var purchase = CreatePurchase();
        var product = CreateProduct(purchaseUnit: UnitOfMeasure.Box, purchaseUnitFactor: 24m);
        var line = purchase.AddLine(product, 1m, 10m);

        // Act
        purchase.UpdateLine(line.Id, 3m, 12.50m);

        // Assert
        Assert.Equal(3m, line.Quantity);
        Assert.Equal(12.50m, line.PurchaseUnitCost);
        Assert.Equal(37.50m, line.Subtotal);
        Assert.Equal(UnitOfMeasure.Box, line.PurchaseUnit);
    }

    /// <summary>Updating an unknown line throws a <see cref="DomainException"/>.</summary>
    [Fact]
    public void UpdateLine_WithUnknownLine_ThrowsDomainException()
    {
        // Arrange
        var purchase = CreatePurchase();

        // Act & Assert
        Assert.Throws<DomainException>(() => purchase.UpdateLine(Guid.NewGuid(), 1m, 10m));
    }

    /// <summary>Updating a line with an invalid quantity throws a <see cref="DomainException"/>.</summary>
    [Fact]
    public void UpdateLine_WithInvalidQuantity_ThrowsDomainException()
    {
        // Arrange
        var purchase = CreatePurchase();
        var product = CreateProduct();
        var line = purchase.AddLine(product, 1m, 10m);

        // Act & Assert
        Assert.Throws<DomainException>(() => purchase.UpdateLine(line.Id, 0m, 10m));
    }

    /// <summary>Removing a line deletes it from the order.</summary>
    [Fact]
    public void RemoveLine_RemovesTheLine()
    {
        // Arrange
        var purchase = CreatePurchase();
        var product = CreateProduct();
        var line = purchase.AddLine(product, 1m, 10m);

        // Act
        purchase.RemoveLine(line.Id);

        // Assert
        Assert.Empty(purchase.Lines);
        Assert.Equal(0m, purchase.Total);
    }

    /// <summary>Removing an unknown line does nothing.</summary>
    [Fact]
    public void RemoveLine_WithUnknownLine_DoesNothing()
    {
        // Arrange
        var purchase = CreatePurchase();
        var product = CreateProduct();
        purchase.AddLine(product, 1m, 10m);

        // Act
        purchase.RemoveLine(Guid.NewGuid());

        // Assert
        Assert.Single(purchase.Lines);
    }

    /// <summary>The total sums the subtotals of every line.</summary>
    [Fact]
    public void Total_SumsLineSubtotals()
    {
        // Arrange
        var purchase = CreatePurchase();
        var first = CreateProduct();
        var second = CreateProduct();

        // Act
        purchase.AddLine(first, 2m, 10m);
        purchase.AddLine(second, 3m, 4.50m);

        // Assert
        Assert.Equal(33.50m, purchase.Total);
    }

    /// <summary>Confirming an order without lines throws a <see cref="DomainException"/>.</summary>
    [Fact]
    public void Confirm_WithoutLines_ThrowsDomainException()
    {
        // Arrange
        var purchase = CreatePurchase();

        // Act & Assert
        Assert.Throws<DomainException>(purchase.Confirm);
    }

    /// <summary>Confirming a draft with lines moves it to confirmed.</summary>
    [Fact]
    public void Confirm_WithLines_MovesToConfirmed()
    {
        // Arrange
        var purchase = CreatePurchaseWithLine();

        // Act
        purchase.Confirm();

        // Assert
        Assert.Equal(PurchaseStatus.Confirmed, purchase.Status);
    }

    /// <summary>Confirming an order that is not a draft throws a <see cref="DomainException"/>.</summary>
    [Fact]
    public void Confirm_WhenNotDraft_ThrowsDomainException()
    {
        // Arrange
        var purchase = CreatePurchaseWithLine();
        purchase.Confirm();

        // Act & Assert
        Assert.Throws<DomainException>(purchase.Confirm);
    }

    /// <summary>Receiving a draft order throws a <see cref="DomainException"/>.</summary>
    [Fact]
    public void Receive_WhenDraft_ThrowsDomainException()
    {
        // Arrange
        var purchase = CreatePurchaseWithLine();

        // Act & Assert
        Assert.Throws<DomainException>(purchase.Receive);
    }

    /// <summary>Receiving a confirmed order moves it to received.</summary>
    [Fact]
    public void Receive_WhenConfirmed_MovesToReceived()
    {
        // Arrange
        var purchase = CreatePurchaseWithLine();
        purchase.Confirm();

        // Act
        purchase.Receive();

        // Assert
        Assert.Equal(PurchaseStatus.Received, purchase.Status);
    }

    /// <summary>Receiving an already received order throws a <see cref="DomainException"/>.</summary>
    [Fact]
    public void Receive_WhenReceived_ThrowsDomainException()
    {
        // Arrange
        var purchase = CreateConfirmedPurchase();
        purchase.Receive();

        // Act & Assert
        Assert.Throws<DomainException>(purchase.Receive);
    }

    /// <summary>A draft order can be cancelled.</summary>
    [Fact]
    public void Cancel_WhenDraft_MovesToCancelled()
    {
        // Arrange
        var purchase = CreatePurchaseWithLine();

        // Act
        purchase.Cancel();

        // Assert
        Assert.Equal(PurchaseStatus.Cancelled, purchase.Status);
    }

    /// <summary>A confirmed order can be cancelled.</summary>
    [Fact]
    public void Cancel_WhenConfirmed_MovesToCancelled()
    {
        // Arrange
        var purchase = CreateConfirmedPurchase();

        // Act
        purchase.Cancel();

        // Assert
        Assert.Equal(PurchaseStatus.Cancelled, purchase.Status);
    }

    /// <summary>A received order cannot be cancelled.</summary>
    [Fact]
    public void Cancel_WhenReceived_ThrowsDomainException()
    {
        // Arrange
        var purchase = CreateConfirmedPurchase();
        purchase.Receive();

        // Act & Assert
        Assert.Throws<DomainException>(purchase.Cancel);
    }

    /// <summary>An already cancelled order cannot be cancelled again.</summary>
    [Fact]
    public void Cancel_WhenCancelled_ThrowsDomainException()
    {
        // Arrange
        var purchase = CreatePurchaseWithLine();
        purchase.Cancel();

        // Act & Assert
        Assert.Throws<DomainException>(purchase.Cancel);
    }

    /// <summary>Adding a line to a confirmed order throws a <see cref="DomainException"/>.</summary>
    [Fact]
    public void AddLine_AfterConfirm_ThrowsDomainException()
    {
        // Arrange
        var purchase = CreateConfirmedPurchase();
        var another = CreateProduct();

        // Act & Assert
        Assert.Throws<DomainException>(() => purchase.AddLine(another, 1m, 10m));
    }

    /// <summary>Removing a line from a received order throws a <see cref="DomainException"/>.</summary>
    [Fact]
    public void RemoveLine_WhenReceived_ThrowsDomainException()
    {
        // Arrange
        var purchase = CreateConfirmedPurchase();
        var lineId = purchase.Lines[0].Id;
        purchase.Receive();

        // Act & Assert
        Assert.Throws<DomainException>(() => purchase.RemoveLine(lineId));
    }

    /// <summary>Updating a line of a cancelled order throws a <see cref="DomainException"/>.</summary>
    [Fact]
    public void UpdateLine_WhenCancelled_ThrowsDomainException()
    {
        // Arrange
        var purchase = CreatePurchaseWithLine();
        var lineId = purchase.Lines[0].Id;
        purchase.Cancel();

        // Act & Assert
        Assert.Throws<DomainException>(() => purchase.UpdateLine(lineId, 1m, 10m));
    }

    // Creates a draft purchase order with the standard test data.
    private static Purchase CreatePurchase() =>
        Purchase.Create(SupplierId, "OC-000001", DateTimeOffset.UnixEpoch, null, "user-1", "user@test.local");

    // Creates a draft order holding a single line of the given product.
    private static Purchase CreatePurchaseWithLine()
    {
        var purchase = CreatePurchase();
        purchase.AddLine(CreateProduct(), 2m, 10m);
        return purchase;
    }

    // Creates a confirmed order ready to be received.
    private static Purchase CreateConfirmedPurchase()
    {
        var purchase = CreatePurchaseWithLine();
        purchase.Confirm();
        return purchase;
    }

    // Builds a valid product, letting each test override only the values it cares about.
    private static Product CreateProduct(
        UnitOfMeasure baseUnit = UnitOfMeasure.Unit,
        UnitOfMeasure purchaseUnit = UnitOfMeasure.Unit,
        decimal purchaseUnitFactor = 1m) =>
        Product.Create(
            "SKU-001",
            "Test product",
            null,
            CategoryIds.Other,
            baseUnit,
            purchaseUnit,
            purchaseUnitFactor,
            10m,
            15m,
            13m,
            0,
            0,
            "user-1",
            "user@test.local",
            DateTimeOffset.UnixEpoch);
}
