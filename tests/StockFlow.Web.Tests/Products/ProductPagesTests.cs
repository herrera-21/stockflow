using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using StockFlow.Domain;
using StockFlow.Domain.Entities;

namespace StockFlow.Web.Tests.Products;

/// <summary>
/// Integration tests for the product Razor Pages: CRUD flows, soft delete and role-based access.
/// </summary>
[Collection(WebCollection.Name)]
public class ProductPagesTests
{
    private readonly StockFlowWebFactory _factory;

    public ProductPagesTests(StockFlowWebFactory factory)
    {
        _factory = factory;
    }

    /// <summary>An anonymous request to the list redirects to the login page.</summary>
    [Fact]
    public async Task Index_WhenNotAuthenticated_RedirectsToLogin()
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // Act
        var response = await client.GetAsync("/Products");

        // Assert
        await AssertStatusAsync(response, HttpStatusCode.Redirect);
        Assert.Contains("/Account/Login", response.Headers.Location!.ToString());
    }

    /// <summary>Creating with valid data persists an active product.</summary>
    [Fact]
    public async Task Create_WithValidData_PersistsProduct()
    {
        // Arrange
        var client = await CreateAuthenticatedClientAsync(StockFlowWebFactory.AdminEmail, StockFlowWebFactory.AdminPassword);
        var sku = NewSku();
        var token = await GetAntiforgeryTokenAsync(client, "/Products/Create");

        // Act
        var response = await client.PostAsync("/Products/Create", Form(new Dictionary<string, string>
        {
            ["Input.Sku"] = sku,
            ["Input.Name"] = "Integration product",
            ["Input.Description"] = "Created by an integration test",
            ["Input.Category"] = "General",
            ["Input.PurchasePrice"] = "10,00",
            ["Input.SalePrice"] = "15,00",
            ["Input.TaxRate"] = "13,00",
            ["Input.InitialStock"] = "5",
            ["Input.MinimumStock"] = "2",
            ["__RequestVerificationToken"] = token
        }));

        // Assert
        await AssertStatusAsync(response, HttpStatusCode.Redirect);

        await using var db = _factory.CreateDbContext();
        var product = await db.Products.SingleAsync(p => p.Sku == sku);
        Assert.Equal("Integration product", product.Name);
        Assert.Equal(5, product.CurrentStock);
        Assert.True(product.IsActive);
    }

    /// <summary>Creating with a duplicate SKU re-renders the form and does not persist.</summary>
    [Fact]
    public async Task Create_WithDuplicateSku_ShowsErrorAndDoesNotPersist()
    {
        // Arrange
        var client = await CreateAuthenticatedClientAsync(StockFlowWebFactory.AdminEmail, StockFlowWebFactory.AdminPassword);
        var sku = NewSku();
        await using (var seedDb = _factory.CreateDbContext())
        {
            seedDb.Products.Add(Product.Create(sku, "Existing", null, "General", 10m, 15m, 13m, 5, 2));
            await seedDb.SaveChangesAsync();
        }

        var token = await GetAntiforgeryTokenAsync(client, "/Products/Create");

        // Act
        var response = await client.PostAsync("/Products/Create", Form(new Dictionary<string, string>
        {
            ["Input.Sku"] = sku,
            ["Input.Name"] = "Duplicate",
            ["Input.Category"] = "General",
            ["Input.PurchasePrice"] = "10,00",
            ["Input.SalePrice"] = "15,00",
            ["Input.TaxRate"] = "13,00",
            ["Input.InitialStock"] = "1",
            ["Input.MinimumStock"] = "0",
            ["__RequestVerificationToken"] = token
        }));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var db = _factory.CreateDbContext();
        Assert.Equal(1, await db.Products.CountAsync(p => p.Sku == sku));
    }

    /// <summary>Editing with valid data updates the editable fields but keeps current stock.</summary>
    [Fact]
    public async Task Edit_WithValidData_UpdatesProduct()
    {
        // Arrange
        var client = await CreateAuthenticatedClientAsync(StockFlowWebFactory.AdminEmail, StockFlowWebFactory.AdminPassword);
        var sku = NewSku();
        Guid id;
        await using (var seedDb = _factory.CreateDbContext())
        {
            var product = Product.Create(sku, "Before", null, "General", 10m, 15m, 13m, 5, 2);
            seedDb.Products.Add(product);
            await seedDb.SaveChangesAsync();
            id = product.Id;
        }

        var token = await GetAntiforgeryTokenAsync(client, $"/Products/Edit/{id}");

        // Act
        var response = await client.PostAsync($"/Products/Edit/{id}", Form(new Dictionary<string, string>
        {
            ["Input.Id"] = id.ToString(),
            ["Input.Sku"] = sku,
            ["Input.Name"] = "After",
            ["Input.Category"] = "Hardware",
            ["Input.PurchasePrice"] = "20,00",
            ["Input.SalePrice"] = "30,00",
            ["Input.TaxRate"] = "8,00",
            ["Input.MinimumStock"] = "4",
            ["__RequestVerificationToken"] = token
        }));

        // Assert
        await AssertStatusAsync(response, HttpStatusCode.Redirect);

        await using var db = _factory.CreateDbContext();
        var updated = await db.Products.SingleAsync(p => p.Id == id);
        Assert.Equal("After", updated.Name);
        Assert.Equal("Hardware", updated.Category);
        Assert.Equal(20m, updated.PurchasePrice);
        Assert.Equal(5, updated.CurrentStock);
    }

    /// <summary>Deactivating marks the product inactive instead of deleting it.</summary>
    [Fact]
    public async Task Deactivate_SoftDeletesProduct()
    {
        // Arrange
        var client = await CreateAuthenticatedClientAsync(StockFlowWebFactory.AdminEmail, StockFlowWebFactory.AdminPassword);
        var sku = NewSku();
        Guid id;
        await using (var seedDb = _factory.CreateDbContext())
        {
            var product = Product.Create(sku, "To deactivate", null, "General", 10m, 15m, 13m, 5, 2);
            seedDb.Products.Add(product);
            await seedDb.SaveChangesAsync();
            id = product.Id;
        }

        var token = await GetAntiforgeryTokenAsync(client, "/Products");

        // Act
        var response = await client.PostAsync($"/Products?handler=Deactivate&id={id}", Form(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token
        }));

        // Assert
        await AssertStatusAsync(response, HttpStatusCode.Redirect);

        await using var db = _factory.CreateDbContext();
        var deactivated = await db.Products.SingleAsync(p => p.Id == id);
        Assert.False(deactivated.IsActive);
    }

    /// <summary>A salesperson cannot access the create page.</summary>
    [Fact]
    public async Task Create_AsSalesperson_IsForbidden()
    {
        // Arrange
        await _factory.EnsureUserInRoleAsync("sales@stockflow.local", "Sales#2026", ApplicationRoles.Salesperson);
        var client = await CreateAuthenticatedClientAsync("sales@stockflow.local", "Sales#2026");

        // Act
        var response = await client.GetAsync("/Products/Create");

        // Assert
        // Identity's cookie auth redirects a denied request to the access-denied page.
        await AssertStatusAsync(response, HttpStatusCode.Redirect);
        Assert.Contains("/Account/AccessDenied", response.Headers.Location!.ToString());
    }

    /// <summary>A salesperson can view the product list.</summary>
    [Fact]
    public async Task Index_AsSalesperson_IsAllowed()
    {
        // Arrange
        await _factory.EnsureUserInRoleAsync("sales@stockflow.local", "Sales#2026", ApplicationRoles.Salesperson);
        var client = await CreateAuthenticatedClientAsync("sales@stockflow.local", "Sales#2026");

        // Act
        var response = await client.GetAsync("/Products");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>An anonymous request to the create page redirects to login.</summary>
    [Fact]
    public async Task Create_WhenNotAuthenticated_RedirectsToLogin()
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // Act
        var response = await client.GetAsync("/Products/Create");

        // Assert
        await AssertStatusAsync(response, HttpStatusCode.Redirect);
        Assert.Contains("/Account/Login", response.Headers.Location!.ToString());
    }

    /// <summary>A salesperson cannot access the edit page.</summary>
    [Fact]
    public async Task Edit_AsSalesperson_IsForbidden()
    {
        // Arrange
        await _factory.EnsureUserInRoleAsync("sales@stockflow.local", "Sales#2026", ApplicationRoles.Salesperson);
        var client = await CreateAuthenticatedClientAsync("sales@stockflow.local", "Sales#2026");

        // Act
        var response = await client.GetAsync($"/Products/Edit/{Guid.NewGuid()}");

        // Assert
        await AssertStatusAsync(response, HttpStatusCode.Redirect);
        Assert.Contains("/Account/AccessDenied", response.Headers.Location!.ToString());
    }

    /// <summary>A salesperson cannot deactivate a product, which stays active.</summary>
    [Fact]
    public async Task Deactivate_AsSalesperson_IsForbiddenAndKeepsProductActive()
    {
        // Arrange
        await _factory.EnsureUserInRoleAsync("sales@stockflow.local", "Sales#2026", ApplicationRoles.Salesperson);
        var client = await CreateAuthenticatedClientAsync("sales@stockflow.local", "Sales#2026");
        var sku = NewSku();
        Guid id;
        await using (var seedDb = _factory.CreateDbContext())
        {
            var product = Product.Create(sku, "Protected", null, "General", 10m, 15m, 13m, 5, 2);
            seedDb.Products.Add(product);
            await seedDb.SaveChangesAsync();
            id = product.Id;
        }

        var token = await GetAntiforgeryTokenAsync(client, "/Products");

        // Act
        var response = await client.PostAsync($"/Products?handler=Deactivate&id={id}", Form(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token
        }));

        // Assert
        await AssertStatusAsync(response, HttpStatusCode.Redirect);
        Assert.Contains("/Account/AccessDenied", response.Headers.Location!.ToString());

        await using var db = _factory.CreateDbContext();
        var unchanged = await db.Products.SingleAsync(p => p.Id == id);
        Assert.True(unchanged.IsActive);
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
