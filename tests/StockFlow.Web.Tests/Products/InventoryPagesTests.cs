using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using StockFlow.Domain;
using StockFlow.Domain.Entities;

namespace StockFlow.Web.Tests.Products;

/// <summary>
/// Integration tests for the inventory pages: the stock adjustment flow, the movement history and
/// role-based access, all against the real SQL Server database.
/// </summary>
[Collection(WebCollection.Name)]
public class InventoryPagesTests
{
    private readonly StockFlowWebFactory _factory;

    public InventoryPagesTests(StockFlowWebFactory factory)
    {
        _factory = factory;
    }

    /// <summary>An anonymous request to the adjustment page redirects to the login page.</summary>
    [Fact]
    public async Task Adjust_WhenNotAuthenticated_RedirectsToLogin()
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var id = await SeedProductAsync(stock: 5);

        // Act
        var response = await client.GetAsync($"/Products/Adjust/{id}");

        // Assert
        await AssertStatusAsync(response, HttpStatusCode.Redirect);
        Assert.Contains("/Account/Login", response.Headers.Location!.ToString());
    }

    /// <summary>A salesperson cannot open the adjustment page.</summary>
    [Fact]
    public async Task Adjust_AsSalesperson_IsForbidden()
    {
        // Arrange
        await _factory.EnsureUserInRoleAsync("sales@stockflow.local", "Sales#2026", ApplicationRoles.Salesperson);
        var client = await CreateAuthenticatedClientAsync("sales@stockflow.local", "Sales#2026");
        var id = await SeedProductAsync(stock: 5);

        // Act
        var response = await client.GetAsync($"/Products/Adjust/{id}");

        // Assert
        await AssertStatusAsync(response, HttpStatusCode.Redirect);
        Assert.Contains("/Account/AccessDenied", response.Headers.Location!.ToString());
    }

    /// <summary>The adjustment page shows the current stock and the resulting-stock preview hook.</summary>
    [Fact]
    public async Task Adjust_Get_ShowsCurrentStockAndPreview()
    {
        // Arrange
        var client = await CreateAuthenticatedClientAsync(StockFlowWebFactory.AdminEmail, StockFlowWebFactory.AdminPassword);
        var id = await SeedProductAsync(stock: 12.5m, unit: UnitOfMeasure.Pound);

        // Act
        var html = await client.GetStringAsync($"/Products/Adjust/{id}");

        // Assert
        Assert.Contains("id=\"stock-preview\"", html);
        Assert.Contains("data-current=\"12.5", html);
        Assert.Contains("Movimiento", html);
    }

    /// <summary>A positive adjustment updates the stock and records the movement.</summary>
    [Fact]
    public async Task Adjust_Post_WithValidIncrease_UpdatesStockAndRecordsMovement()
    {
        // Arrange
        var client = await CreateAuthenticatedClientAsync(StockFlowWebFactory.AdminEmail, StockFlowWebFactory.AdminPassword);
        var id = await SeedProductAsync(stock: 10);
        var token = await GetAntiforgeryTokenAsync(client, $"/Products/Adjust/{id}");

        // Act
        var response = await client.PostAsync($"/Products/Adjust/{id}", Form(new Dictionary<string, string>
        {
            ["Input.ProductId"] = id.ToString(),
            ["Input.Type"] = "AdjustmentIncrease",
            ["Input.Quantity"] = "5",
            ["Input.Reason"] = "PhysicalCount",
            ["Input.Note"] = string.Empty,
            ["__RequestVerificationToken"] = token
        }));

        // Assert
        await AssertStatusAsync(response, HttpStatusCode.Redirect);
        Assert.Contains($"/Products/Movements/{id}", response.Headers.Location!.ToString());

        await using var db = _factory.CreateDbContext();
        Assert.Equal(15m, (await db.Products.SingleAsync(p => p.Id == id)).CurrentStock);
        var movement = await db.InventoryMovements
            .SingleAsync(m => m.ProductId == id && m.Type == InventoryMovementType.AdjustmentIncrease);
        Assert.Equal(5m, movement.Quantity);
        Assert.Equal(10m, movement.StockBefore);
        Assert.Equal(15m, movement.StockAfter);
        Assert.Equal(StockFlowWebFactory.AdminEmail, movement.UserName);
    }

    /// <summary>A negative adjustment that would go below zero is rejected and keeps the stock.</summary>
    [Fact]
    public async Task Adjust_Post_WithInsufficientStock_ShowsErrorAndKeepsStock()
    {
        // Arrange
        var client = await CreateAuthenticatedClientAsync(StockFlowWebFactory.AdminEmail, StockFlowWebFactory.AdminPassword);
        var id = await SeedProductAsync(stock: 3);
        var token = await GetAntiforgeryTokenAsync(client, $"/Products/Adjust/{id}");

        // Act
        var response = await client.PostAsync($"/Products/Adjust/{id}", Form(new Dictionary<string, string>
        {
            ["Input.ProductId"] = id.ToString(),
            ["Input.Type"] = "AdjustmentDecrease",
            ["Input.Quantity"] = "5",
            ["Input.Reason"] = "Damage",
            ["__RequestVerificationToken"] = token
        }));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AssertFieldError(await response.Content.ReadAsStringAsync(), "Input.Quantity");

        await using var db = _factory.CreateDbContext();
        Assert.Equal(3m, (await db.Products.SingleAsync(p => p.Id == id)).CurrentStock);
        Assert.False(await db.InventoryMovements.AnyAsync(
            m => m.ProductId == id && m.Type == InventoryMovementType.AdjustmentDecrease));
    }

    /// <summary>The movement history lists the movements of a product, newest first.</summary>
    [Fact]
    public async Task Movements_Get_ListsProductMovements()
    {
        // Arrange
        var client = await CreateAuthenticatedClientAsync(StockFlowWebFactory.AdminEmail, StockFlowWebFactory.AdminPassword);
        var id = await SeedProductAsync(stock: 10);

        // Record a manual adjustment through the page so the history has more than the opening
        // balance; the opening balance is created by the seed.
        var token = await GetAntiforgeryTokenAsync(client, $"/Products/Adjust/{id}");
        await client.PostAsync($"/Products/Adjust/{id}", Form(new Dictionary<string, string>
        {
            ["Input.ProductId"] = id.ToString(),
            ["Input.Type"] = "AdjustmentIncrease",
            ["Input.Quantity"] = "5",
            ["Input.Reason"] = "PhysicalCount",
            ["__RequestVerificationToken"] = token
        }));

        // Act
        var html = await client.GetStringAsync($"/Products/Movements/{id}");

        // Assert: the opening balance and the manual adjustment appear.
        Assert.Contains("Saldo inicial", html);
        Assert.Contains("Ajuste positivo", html);
        Assert.Contains("movements-container", html);
    }

    /// <summary>A product with no movements shows the empty state.</summary>
    [Fact]
    public async Task Movements_WithoutMovements_ShowsEmptyState()
    {
        // Arrange
        var client = await CreateAuthenticatedClientAsync(StockFlowWebFactory.AdminEmail, StockFlowWebFactory.AdminPassword);
        var id = await SeedProductAsync(stock: 0);

        // Act
        var html = await client.GetStringAsync($"/Products/Movements/{id}");

        // Assert
        Assert.Contains("no tiene movimientos", html);
    }

    /// <summary>The movement history is visible to a salesperson, who just cannot adjust.</summary>
    [Fact]
    public async Task Movements_AsSalesperson_IsAllowed()
    {
        // Arrange
        await _factory.EnsureUserInRoleAsync("sales@stockflow.local", "Sales#2026", ApplicationRoles.Salesperson);
        var client = await CreateAuthenticatedClientAsync("sales@stockflow.local", "Sales#2026");
        var id = await SeedProductAsync(stock: 5);

        // Act
        var response = await client.GetAsync($"/Products/Movements/{id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// Two simultaneous adjustments of the same product: the first commits, the second sees a stale
    /// rowversion and its save is rejected instead of silently overwriting the first.
    /// </summary>
    [Fact]
    public async Task ConcurrentAdjustments_SecondSave_DetectsConflict()
    {
        // Arrange
        var id = await SeedProductAsync(stock: 10);

        await using var first = _factory.CreateDbContext();
        await using var second = _factory.CreateDbContext();

        // Both contexts track the same product at the same version.
        var firstProduct = await first.Products.SingleAsync(p => p.Id == id);
        var secondProduct = await second.Products.SingleAsync(p => p.Id == id);

        // The first operation commits, bumping the rowversion.
        firstProduct.ApplyMovement(
            InventoryMovementType.AdjustmentIncrease, 5m, InventoryAdjustmentReason.PhysicalCount, null,
            "user-1", "user1@test.local", DateTimeOffset.UtcNow);
        first.InventoryMovements.Add(firstProduct.Movements[^1]);
        await first.SaveChangesAsync();

        // The second operation still holds the old version; adding the movement explicitly mirrors
        // what the handler does.
        secondProduct.ApplyMovement(
            InventoryMovementType.AdjustmentIncrease, 3m, InventoryAdjustmentReason.PhysicalCount, null,
            "user-2", "user2@test.local", DateTimeOffset.UtcNow);
        second.InventoryMovements.Add(secondProduct.Movements[^1]);

        // Act & Assert: the stale context is rejected by the concurrency token.
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());

        // Only the first adjustment stuck: the opening balance plus one adjustment.
        await using var db = _factory.CreateDbContext();
        Assert.Equal(15m, (await db.Products.SingleAsync(p => p.Id == id)).CurrentStock);
        Assert.Equal(2, await db.InventoryMovements.CountAsync(m => m.ProductId == id));
    }

    /// <summary>
    /// The product list offers the adjust action for active products only; an inactive product keeps
    /// its movement history link.
    /// </summary>
    [Fact]
    public async Task Index_InactiveProduct_HidesAdjustAction()
    {
        // Arrange
        var client = await CreateAuthenticatedClientAsync(StockFlowWebFactory.AdminEmail, StockFlowWebFactory.AdminPassword);
        var activeId = await SeedProductAsync(stock: 5);
        var inactiveId = await SeedProductAsync(stock: 5);
        string activeSku;
        string inactiveSku;
        await using (var db = _factory.CreateDbContext())
        {
            var inactive = await db.Products.SingleAsync(p => p.Id == inactiveId);
            inactive.Deactivate();
            await db.SaveChangesAsync();
            inactiveSku = inactive.Sku;
            activeSku = (await db.Products.SingleAsync(p => p.Id == activeId)).Sku;
        }

        // Act
        var activeHtml = await client.GetStringAsync($"/Products?search={activeSku}");
        var inactiveHtml = await client.GetStringAsync($"/Products?search={inactiveSku}");

        // Assert
        Assert.Contains($"/Products/Adjust/{activeId}", activeHtml);
        Assert.DoesNotContain($"/Products/Adjust/{inactiveId}", inactiveHtml);
        Assert.Contains($"/Products/Movements/{inactiveId}", inactiveHtml);
    }

    // Seeds an active product with the given stock and unit and returns its id.
    private async Task<Guid> SeedProductAsync(decimal stock, UnitOfMeasure unit = UnitOfMeasure.Unit)
    {
        await using var db = _factory.CreateDbContext();
        var product = Product.Create(
            NewSku(), $"Inventory {Guid.NewGuid():N}", null, CategoryIds.Other,
            unit, unit, 1m, 10m, 15m, 13m, stock, 0,
            "seed", "seed@test.local", DateTimeOffset.UtcNow);
        db.Products.Add(product);
        await db.SaveChangesAsync();
        return product.Id;
    }

    // Generates a unique SKU so tests do not collide on the unique index.
    private static string NewSku() => $"IT-{Guid.NewGuid().ToString("N")[..8]}";

    // Wraps the form fields as URL-encoded content.
    private static FormUrlEncodedContent Form(Dictionary<string, string> fields) => new(fields);

    // Asserts the status code, including the response body in the failure message.
    private static async Task AssertStatusAsync(HttpResponseMessage response, HttpStatusCode expected)
    {
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(
            response.StatusCode == expected,
            $"Expected {expected}, got {response.StatusCode}. Location: {response.Headers.Location}. Body: {body}");
    }

    // Asserts that the rendered form shows a validation error for the given field.
    private static void AssertFieldError(string html, string field)
    {
        var span = Regex.Match(html, $"<span[^>]*data-valmsg-for=\"{Regex.Escape(field)}\"[^>]*>");
        Assert.True(span.Success, $"No validation span for {field}.");
        Assert.Contains("field-validation-error", span.Value);
    }

    // Logs in with the given credentials and returns a client that does not follow redirects.
    private async Task<HttpClient> CreateAuthenticatedClientAsync(string email, string password)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var token = await GetAntiforgeryTokenAsync(client, "/Account/Login");

        var response = await client.PostAsync("/Account/Login", Form(new Dictionary<string, string>
        {
            ["Input.Email"] = email,
            ["Input.Password"] = password,
            ["__RequestVerificationToken"] = token
        }));

        await AssertStatusAsync(response, HttpStatusCode.Redirect);
        return client;
    }

    // Scrapes the antiforgery token from the page served at the given URL.
    private static async Task<string> GetAntiforgeryTokenAsync(HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        var html = await response.Content.ReadAsStringAsync();
        var match = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");

        Assert.True(match.Success, $"Antiforgery token not found in {url}.");
        return match.Groups[1].Value;
    }
}
