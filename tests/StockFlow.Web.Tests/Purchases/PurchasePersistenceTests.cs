using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StockFlow.Application.Common.Interfaces;
using StockFlow.Domain;
using StockFlow.Domain.Entities;

namespace StockFlow.Web.Tests.Purchases;

/// <summary>
/// Integration tests for purchase persistence against the real SQL Server database: an order with
/// lines is saved and read back with its unit snapshot, and the numbering sequence advances one by
/// one.
/// </summary>
[Collection(WebCollection.Name)]
public class PurchasePersistenceTests
{
    private readonly StockFlowWebFactory _factory;

    /// <summary>Initializes the tests with the shared web factory.</summary>
    /// <param name="factory">Factory that boots the app against the test database.</param>
    public PurchasePersistenceTests(StockFlowWebFactory factory)
    {
        _factory = factory;
    }

    /// <summary>A draft order with lines round-trips with its snapshot, subtotal and total.</summary>
    [Fact]
    public async Task PersistDraft_WithLines_SavesSnapshotAndSubtotal()
    {
        // Arrange
        Guid purchaseId;
        Guid supplierId;
        Guid productId;
        string number;

        await using (var db = _factory.CreateDbContext())
        {
            var supplier = Supplier.Create(
                $"Proveedor {Guid.NewGuid():N}", DocumentType.Passport, NewTaxId(), null, null, null, null);
            var product = Product.Create(
                NewSku(), "Producto de compra", null, CategoryIds.Other,
                UnitOfMeasure.Unit, UnitOfMeasure.Box, 24m, 19.50m, 25m, 13m, 0m, 0m,
                "seed", "seed@test.local", DateTimeOffset.UtcNow);

            db.Suppliers.Add(supplier);
            db.Products.Add(product);
            await db.SaveChangesAsync();

            using var scope = _factory.Services.CreateScope();
            var generator = scope.ServiceProvider.GetRequiredService<IPurchaseNumberGenerator>();
            number = await generator.NextNumberAsync();

            var purchase = Purchase.Create(
                supplier.Id, number, DateTimeOffset.UtcNow, "Integration", "user-1", "user1@test.local");
            purchase.AddLine(product, 2m, 19.50m);

            db.Purchases.Add(purchase);
            await db.SaveChangesAsync();

            purchaseId = purchase.Id;
            supplierId = supplier.Id;
            productId = product.Id;
        }

        // Act
        await using var verify = _factory.CreateDbContext();
        var saved = await verify.Purchases
            .Include(p => p.Lines)
            .SingleAsync(p => p.Id == purchaseId);

        // Assert
        Assert.Equal(number, saved.Number);
        Assert.Equal(supplierId, saved.SupplierId);
        Assert.Equal(PurchaseStatus.Draft, saved.Status);
        Assert.Equal("Integration", saved.Note);
        Assert.Equal("user-1", saved.CreatedByUserId);

        var line = Assert.Single(saved.Lines);
        Assert.Equal(productId, line.ProductId);
        Assert.Equal(2m, line.Quantity);
        Assert.Equal(19.50m, line.PurchaseUnitCost);
        Assert.Equal(UnitOfMeasure.Box, line.PurchaseUnit);
        Assert.Equal(UnitOfMeasure.Unit, line.BaseUnit);
        Assert.Equal(24m, line.PurchaseUnitFactor);
        Assert.Equal(39.00m, line.Subtotal);
        Assert.Equal(39.00m, saved.Total);
    }

    /// <summary>Two calls to the generator return consecutive sequence numbers.</summary>
    [Fact]
    public async Task NextNumberAsync_ReturnsConsecutiveNumbers()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var generator = scope.ServiceProvider.GetRequiredService<IPurchaseNumberGenerator>();

        // Act
        var first = await generator.NextNumberAsync();
        var second = await generator.NextNumberAsync();

        // Assert
        Assert.StartsWith($"{PurchaseNumber.Prefix}-", first);
        Assert.Equal(SequenceValue(first) + 1, SequenceValue(second));
    }

    // Extracts the numeric part of a formatted order number.
    private static long SequenceValue(string number) =>
        long.Parse(number[(PurchaseNumber.Prefix.Length + 1)..], CultureInfo.InvariantCulture);

    // Generates a unique SKU so tests do not collide on the unique index.
    private static string NewSku() => $"PT-{Guid.NewGuid().ToString("N")[..8]}";

    // Generates a passport-like document number so suppliers do not collide on the unique index.
    private static string NewTaxId() => $"P{Guid.NewGuid().ToString("N")[..10]}";
}
