using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using StockFlow.Domain;
using StockFlow.Domain.Entities;

namespace StockFlow.Web.Tests.Suppliers;

/// <summary>
/// Integration tests for the supplier-products association page: assigning, updating, removing and
/// role-based access.
/// </summary>
[Collection(WebCollection.Name)]
public class SupplierProductsPageTests
{
    private readonly StockFlowWebFactory _factory;

    public SupplierProductsPageTests(StockFlowWebFactory factory)
    {
        _factory = factory;
    }

    /// <summary>An anonymous request to the page redirects to the login page.</summary>
    [Fact]
    public async Task Products_WhenNotAuthenticated_RedirectsToLogin()
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var supplierId = await SeedSupplierAsync();

        // Act
        var response = await client.GetAsync($"/Suppliers/Products/{supplierId}");

        // Assert
        await AssertStatusAsync(response, HttpStatusCode.Redirect);
        Assert.Contains("/Account/Login", response.Headers.Location!.ToString());
    }

    /// <summary>A salesperson cannot access the association page.</summary>
    [Fact]
    public async Task Products_AsSalesperson_IsForbidden()
    {
        // Arrange
        await _factory.EnsureUserInRoleAsync("sales@stockflow.local", "Sales#2026", ApplicationRoles.Salesperson);
        var client = await CreateAuthenticatedClientAsync("sales@stockflow.local", "Sales#2026");
        var supplierId = await SeedSupplierAsync();

        // Act
        var response = await client.GetAsync($"/Suppliers/Products/{supplierId}");

        // Assert
        await AssertStatusAsync(response, HttpStatusCode.Redirect);
        Assert.Contains("/Account/AccessDenied", response.Headers.Location!.ToString());
    }

    /// <summary>The page lists the products already associated with the supplier.</summary>
    [Fact]
    public async Task Products_ShowsAssociatedProducts()
    {
        // Arrange
        var client = await AdminClientAsync();
        var supplierId = await SeedSupplierAsync();
        var productId = await SeedProductAsync("Associated product");
        await using (var db = _factory.CreateDbContext())
        {
            db.ProductSuppliers.Add(ProductSupplier.Create(productId, supplierId, "SUP-1", 5m));
            await db.SaveChangesAsync();
        }

        // Act
        var response = await client.GetAsync($"/Suppliers/Products/{supplierId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Associated product", body);
        Assert.Contains("SUP-1", body);
        Assert.Contains("$5.00", body);
    }

    /// <summary>
    /// The form explains its fields: product options carry their purchase unit and price for the
    /// dynamic price help, and the list shows the agreed price per purchase unit.
    /// </summary>
    [Fact]
    public async Task Products_ShowsPriceHelpAndPricePerPurchaseUnit()
    {
        // Arrange
        var client = await AdminClientAsync();
        var supplierId = await SeedSupplierAsync();
        var productId = await SeedProductAsync("Priced product");
        await using (var db = _factory.CreateDbContext())
        {
            db.ProductSuppliers.Add(ProductSupplier.Create(productId, supplierId, null, 5m));
            await db.SaveChangesAsync();
        }

        // Act
        var body = await client.GetStringAsync($"/Suppliers/Products/{supplierId}");

        // Assert: the seeded products are bought by the unit (symbol "u" in the default culture).
        Assert.Contains("id=\"supplier-price-help\"", body);
        Assert.Contains("data-unit=\"", body);
        Assert.Contains("data-price=\"$", body);
        Assert.Contains("$5.00 <span class=\"text-body-secondary small\">/ u</span>", body);
    }

    /// <summary>Assigning a product persists the association with its supplier-specific data.</summary>
    [Fact]
    public async Task Assign_WithValidData_PersistsAssociation()
    {
        // Arrange
        var client = await AdminClientAsync();
        var supplierId = await SeedSupplierAsync();
        var productId = await SeedProductAsync("Assignable product");
        var token = await GetAntiforgeryTokenAsync(client, $"/Suppliers/Products/{supplierId}");

        // Act
        var response = await client.PostAsync($"/Suppliers/Products/{supplierId}?handler=Assign", Form(new Dictionary<string, string>
        {
            ["Input.ProductId"] = productId.ToString(),
            ["Input.SupplierSku"] = "SUP-42",
            ["Input.PurchasePrice"] = "7.50",
            ["Input.IsPreferred"] = "true",
            ["__RequestVerificationToken"] = token
        }));

        // Assert
        await AssertStatusAsync(response, HttpStatusCode.Redirect);

        await using var db = _factory.CreateDbContext();
        var association = await db.ProductSuppliers.SingleAsync(ps => ps.SupplierId == supplierId && ps.ProductId == productId);
        Assert.Equal("SUP-42", association.SupplierSku);
        Assert.Equal(7.50m, association.PurchasePrice);
        Assert.True(association.IsPreferred);
    }

    /// <summary>Assigning the same product again updates the existing association.</summary>
    [Fact]
    public async Task Assign_WhenAssociationExists_UpdatesIt()
    {
        // Arrange
        var client = await AdminClientAsync();
        var supplierId = await SeedSupplierAsync();
        var productId = await SeedProductAsync("Updatable product");
        await using (var db = _factory.CreateDbContext())
        {
            db.ProductSuppliers.Add(ProductSupplier.Create(productId, supplierId, "SUP-OLD", 1m));
            await db.SaveChangesAsync();
        }

        var token = await GetAntiforgeryTokenAsync(client, $"/Suppliers/Products/{supplierId}");

        // Act
        var response = await client.PostAsync($"/Suppliers/Products/{supplierId}?handler=Assign", Form(new Dictionary<string, string>
        {
            ["Input.ProductId"] = productId.ToString(),
            ["Input.SupplierSku"] = "SUP-NEW",
            ["Input.PurchasePrice"] = "9.99",
            ["__RequestVerificationToken"] = token
        }));

        // Assert
        await AssertStatusAsync(response, HttpStatusCode.Redirect);

        await using var db2 = _factory.CreateDbContext();
        Assert.Equal(1, await db2.ProductSuppliers.CountAsync(ps => ps.SupplierId == supplierId && ps.ProductId == productId));
        var association = await db2.ProductSuppliers.SingleAsync(ps => ps.SupplierId == supplierId && ps.ProductId == productId);
        Assert.Equal("SUP-NEW", association.SupplierSku);
        Assert.Equal(9.99m, association.PurchasePrice);
    }

    /// <summary>Removing an association deletes it while keeping the product.</summary>
    [Fact]
    public async Task Remove_DeletesAssociation()
    {
        // Arrange
        var client = await AdminClientAsync();
        var supplierId = await SeedSupplierAsync();
        var productId = await SeedProductAsync("Removable product");
        await using (var db = _factory.CreateDbContext())
        {
            db.ProductSuppliers.Add(ProductSupplier.Create(productId, supplierId, null, null));
            await db.SaveChangesAsync();
        }

        var token = await GetAntiforgeryTokenAsync(client, $"/Suppliers/Products/{supplierId}");

        // Act
        var response = await client.PostAsync($"/Suppliers/Products/{supplierId}?handler=Remove&productId={productId}", Form(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token
        }));

        // Assert
        await AssertStatusAsync(response, HttpStatusCode.Redirect);

        await using var db2 = _factory.CreateDbContext();
        Assert.False(await db2.ProductSuppliers.AnyAsync(ps => ps.SupplierId == supplierId && ps.ProductId == productId));
        Assert.True(await db2.Products.AnyAsync(p => p.Id == productId));
    }

    // Persists a supplier with a unique tax id and returns its id.
    private async Task<Guid> SeedSupplierAsync()
    {
        await using var db = _factory.CreateDbContext();
        var number = BitConverter.ToUInt32(Guid.NewGuid().ToByteArray(), 0) % 100_000_000;
        var supplier = Supplier.Create("Association supplier", DocumentType.Dui, $"{number:D8}-{number % 10}", null, null, null, null);
        db.Suppliers.Add(supplier);
        await db.SaveChangesAsync();
        return supplier.Id;
    }

    // Persists an active product with a unique SKU and returns its id.
    private async Task<Guid> SeedProductAsync(string name)
    {
        await using var db = _factory.CreateDbContext();
        var product = Product.Create($"SKU-{Guid.NewGuid().ToString("N")[..8]}", name, null, CategoryIds.Other, UnitOfMeasure.Unit, UnitOfMeasure.Unit, 1m, 10m, 15m, 13m, 0, 0, "user-1", "user@test.local", DateTimeOffset.UnixEpoch);
        db.Products.Add(product);
        await db.SaveChangesAsync();
        return product.Id;
    }

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

    // Returns an authenticated client for the seeded administrator.
    private Task<HttpClient> AdminClientAsync() =>
        CreateAuthenticatedClientAsync(StockFlowWebFactory.AdminEmail, StockFlowWebFactory.AdminPassword);

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
