using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using StockFlow.Domain;
using StockFlow.Domain.Entities;

namespace StockFlow.Web.Tests.Suppliers;

/// <summary>
/// Integration tests for the supplier Razor Pages: CRUD flows, soft delete and role-based access.
/// </summary>
[Collection(WebCollection.Name)]
public class SupplierPagesTests
{
    private readonly StockFlowWebFactory _factory;

    public SupplierPagesTests(StockFlowWebFactory factory)
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
        var response = await client.GetAsync("/Suppliers");

        // Assert
        await AssertStatusAsync(response, HttpStatusCode.Redirect);
        Assert.Contains("/Account/Login", response.Headers.Location!.ToString());
    }

    /// <summary>An inventory manager can view the supplier list.</summary>
    [Fact]
    public async Task Index_AsInventoryManager_IsAllowed()
    {
        // Arrange
        await _factory.EnsureUserInRoleAsync("inventory@stockflow.local", "Inventory#2026", ApplicationRoles.InventoryManager);
        var client = await CreateAuthenticatedClientAsync("inventory@stockflow.local", "Inventory#2026");

        // Act
        var response = await client.GetAsync("/Suppliers");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>A salesperson cannot access the supplier list.</summary>
    [Fact]
    public async Task Index_AsSalesperson_IsForbidden()
    {
        // Arrange
        await _factory.EnsureUserInRoleAsync("sales@stockflow.local", "Sales#2026", ApplicationRoles.Salesperson);
        var client = await CreateAuthenticatedClientAsync("sales@stockflow.local", "Sales#2026");

        // Act
        var response = await client.GetAsync("/Suppliers");

        // Assert
        await AssertStatusAsync(response, HttpStatusCode.Redirect);
        Assert.Contains("/Account/AccessDenied", response.Headers.Location!.ToString());
    }

    /// <summary>Creating with valid data persists an active supplier.</summary>
    [Fact]
    public async Task Create_WithValidData_PersistsSupplier()
    {
        // Arrange
        var client = await AdminClientAsync();
        var taxId = NewTaxId();
        var token = await GetAntiforgeryTokenAsync(client, "/Suppliers/Create");

        // Act
        var response = await client.PostAsync("/Suppliers/Create", Form(new Dictionary<string, string>
        {
            ["Input.Name"] = "Integration supplier",
            ["Input.DocumentType"] = "Dui",
            ["Input.TaxId"] = taxId,
            ["Input.ContactName"] = "Jane Doe",
            ["Input.Phone"] = "8888-8888",
            ["Input.Email"] = "integration@acme.test",
            ["Input.Address"] = "Main street 1",
            ["__RequestVerificationToken"] = token
        }));

        // Assert
        await AssertStatusAsync(response, HttpStatusCode.Redirect);

        await using var db = _factory.CreateDbContext();
        var supplier = await db.Suppliers.SingleAsync(s => s.TaxId == taxId);
        Assert.Equal("Integration supplier", supplier.Name);
        Assert.Equal("Jane Doe", supplier.ContactName);
        Assert.Equal("integration@acme.test", supplier.Email);
        Assert.True(supplier.IsActive);
    }

    /// <summary>An invalid contact name shows a localized message instead of the resource key.</summary>
    [Fact]
    public async Task Create_WithInvalidContactName_ShowsLocalizedMessage()
    {
        // Arrange
        var client = await AdminClientAsync();
        var token = await GetAntiforgeryTokenAsync(client, "/Suppliers/Create");

        // Act
        var response = await client.PostAsync("/Suppliers/Create", Form(new Dictionary<string, string>
        {
            ["Input.Name"] = "Acme Supplies",
            ["Input.DocumentType"] = "Dui",
            ["Input.TaxId"] = NewTaxId(),
            ["Input.ContactName"] = "Jane 123",
            ["__RequestVerificationToken"] = token
        }));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("solo admite letras", html);
        Assert.DoesNotContain("ValidationPersonName", html);
    }

    /// <summary>Creating with a duplicate tax id re-renders the form and does not persist.</summary>
    [Fact]
    public async Task Create_WithDuplicateTaxId_ShowsErrorAndDoesNotPersist()
    {
        // Arrange
        var client = await AdminClientAsync();
        var taxId = NewTaxId();
        await using (var seedDb = _factory.CreateDbContext())
        {
            seedDb.Suppliers.Add(Supplier.Create("Existing", DocumentType.Dui, taxId, null, null, null, null));
            await seedDb.SaveChangesAsync();
        }

        var token = await GetAntiforgeryTokenAsync(client, "/Suppliers/Create");

        // Act
        var response = await client.PostAsync("/Suppliers/Create", Form(new Dictionary<string, string>
        {
            ["Input.Name"] = "Duplicate",
            ["Input.DocumentType"] = "Dui",
            ["Input.TaxId"] = taxId,
            ["__RequestVerificationToken"] = token
        }));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var db = _factory.CreateDbContext();
        Assert.Equal(1, await db.Suppliers.CountAsync(s => s.TaxId == taxId));
    }

    /// <summary>Editing with valid data updates the supplier.</summary>
    [Fact]
    public async Task Edit_WithValidData_UpdatesSupplier()
    {
        // Arrange
        var client = await AdminClientAsync();
        var taxId = NewTaxId();
        Guid id;
        await using (var seedDb = _factory.CreateDbContext())
        {
            var supplier = Supplier.Create("Before", DocumentType.Dui, taxId, null, null, null, null);
            seedDb.Suppliers.Add(supplier);
            await seedDb.SaveChangesAsync();
            id = supplier.Id;
        }

        var token = await GetAntiforgeryTokenAsync(client, $"/Suppliers/Edit/{id}");

        // Act
        var response = await client.PostAsync($"/Suppliers/Edit/{id}", Form(new Dictionary<string, string>
        {
            ["Input.Id"] = id.ToString(),
            ["Input.Name"] = "After",
            ["Input.DocumentType"] = "Dui",
            ["Input.TaxId"] = taxId,
            ["Input.ContactName"] = "John Roe",
            ["Input.Phone"] = "7777-7777",
            ["Input.Email"] = "after@acme.test",
            ["Input.Address"] = "New address 5",
            ["__RequestVerificationToken"] = token
        }));

        // Assert
        await AssertStatusAsync(response, HttpStatusCode.Redirect);

        await using var db = _factory.CreateDbContext();
        var updated = await db.Suppliers.SingleAsync(s => s.Id == id);
        Assert.Equal("After", updated.Name);
        Assert.Equal("John Roe", updated.ContactName);
        Assert.Equal("7777-7777", updated.Phone);
        Assert.Equal("after@acme.test", updated.Email);
        Assert.Equal("New address 5", updated.Address);
    }

    /// <summary>Deactivating marks the supplier inactive instead of deleting it.</summary>
    [Fact]
    public async Task Deactivate_SoftDeletesSupplier()
    {
        // Arrange
        var client = await AdminClientAsync();
        Guid id;
        await using (var seedDb = _factory.CreateDbContext())
        {
            var supplier = Supplier.Create("To deactivate", DocumentType.Dui, NewTaxId(), null, null, null, null);
            seedDb.Suppliers.Add(supplier);
            await seedDb.SaveChangesAsync();
            id = supplier.Id;
        }

        var token = await GetAntiforgeryTokenAsync(client, "/Suppliers");

        // Act
        var response = await client.PostAsync($"/Suppliers?handler=Deactivate&id={id}", Form(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token
        }));

        // Assert
        await AssertStatusAsync(response, HttpStatusCode.Redirect);

        await using var db = _factory.CreateDbContext();
        var deactivated = await db.Suppliers.SingleAsync(s => s.Id == id);
        Assert.False(deactivated.IsActive);
    }

    /// <summary>A salesperson cannot deactivate a supplier, which stays active.</summary>
    [Fact]
    public async Task Deactivate_AsSalesperson_IsForbiddenAndKeepsSupplierActive()
    {
        // Arrange
        await _factory.EnsureUserInRoleAsync("sales@stockflow.local", "Sales#2026", ApplicationRoles.Salesperson);
        var client = await CreateAuthenticatedClientAsync("sales@stockflow.local", "Sales#2026");
        Guid id;
        await using (var seedDb = _factory.CreateDbContext())
        {
            var supplier = Supplier.Create("Protected", DocumentType.Dui, NewTaxId(), null, null, null, null);
            seedDb.Suppliers.Add(supplier);
            await seedDb.SaveChangesAsync();
            id = supplier.Id;
        }

        // Act
        var response = await client.PostAsync($"/Suppliers?handler=Deactivate&id={id}", Form(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = "irrelevant"
        }));

        // Assert
        await AssertStatusAsync(response, HttpStatusCode.Redirect);
        Assert.Contains("/Account/AccessDenied", response.Headers.Location!.ToString());

        await using var db = _factory.CreateDbContext();
        var unchanged = await db.Suppliers.SingleAsync(s => s.Id == id);
        Assert.True(unchanged.IsActive);
    }

    // Generates a unique tax id so tests do not collide on the unique index.
    private static string NewTaxId()
    {
        var number = BitConverter.ToUInt32(Guid.NewGuid().ToByteArray(), 0) % 100_000_000;
        return $"{number:D8}-{number % 10}";
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
