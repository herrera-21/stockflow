using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using StockFlow.Domain;
using StockFlow.Domain.Entities;

namespace StockFlow.Web.Tests.Customers;

/// <summary>
/// Integration tests for the customer Razor Pages: CRUD flows, soft delete, the purchase-history
/// placeholder and role-based access.
/// </summary>
[Collection(WebCollection.Name)]
public class CustomerPagesTests
{
    private readonly StockFlowWebFactory _factory;

    public CustomerPagesTests(StockFlowWebFactory factory)
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
        var response = await client.GetAsync("/Customers");

        // Assert
        await AssertStatusAsync(response, HttpStatusCode.Redirect);
        Assert.Contains("/Account/Login", response.Headers.Location!.ToString());
    }

    /// <summary>A salesperson can view the customer list.</summary>
    [Fact]
    public async Task Index_AsSalesperson_IsAllowed()
    {
        // Arrange
        await _factory.EnsureUserInRoleAsync("sales@stockflow.local", "Sales#2026", ApplicationRoles.Salesperson);
        var client = await CreateAuthenticatedClientAsync("sales@stockflow.local", "Sales#2026");

        // Act
        var response = await client.GetAsync("/Customers");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>An inventory manager cannot access the customer list.</summary>
    [Fact]
    public async Task Index_AsInventoryManager_IsForbidden()
    {
        // Arrange
        await _factory.EnsureUserInRoleAsync("inventory@stockflow.local", "Inventory#2026", ApplicationRoles.InventoryManager);
        var client = await CreateAuthenticatedClientAsync("inventory@stockflow.local", "Inventory#2026");

        // Act
        var response = await client.GetAsync("/Customers");

        // Assert
        await AssertStatusAsync(response, HttpStatusCode.Redirect);
        Assert.Contains("/Account/AccessDenied", response.Headers.Location!.ToString());
    }

    /// <summary>Creating with valid data persists an active customer.</summary>
    [Fact]
    public async Task Create_WithValidData_PersistsCustomer()
    {
        // Arrange
        var client = await AdminClientAsync();
        var taxId = NewTaxId();
        var token = await GetAntiforgeryTokenAsync(client, "/Customers/Create");

        // Act
        var response = await client.PostAsync("/Customers/Create", Form(new Dictionary<string, string>
        {
            ["Input.Name"] = "Integration customer",
            ["Input.DocumentType"] = "Dui",
            ["Input.TaxId"] = taxId,
            ["Input.Phone"] = "8888-8888",
            ["Input.Email"] = "integration@acme.test",
            ["Input.Address"] = "Main street 1",
            ["__RequestVerificationToken"] = token
        }));

        // Assert
        await AssertStatusAsync(response, HttpStatusCode.Redirect);

        await using var db = _factory.CreateDbContext();
        var customer = await db.Customers.SingleAsync(c => c.TaxId == taxId);
        Assert.Equal("Integration customer", customer.Name);
        Assert.Equal("integration@acme.test", customer.Email);
        Assert.True(customer.IsActive);
    }

    /// <summary>Creating a company customer whose name contains digits and symbols persists it.</summary>
    [Fact]
    public async Task Create_WithCompanyNameContainingDigitsAndSymbols_PersistsCustomer()
    {
        // Arrange
        var client = await AdminClientAsync();
        var nit = NewCompanyNit();
        var token = await GetAntiforgeryTokenAsync(client, "/Customers/Create");

        // Act
        var response = await client.PostAsync("/Customers/Create", Form(new Dictionary<string, string>
        {
            ["Input.Name"] = "Distribuidora 24/7 S.A. de C.V.",
            ["Input.DocumentType"] = "Nit",
            ["Input.TaxId"] = nit,
            ["__RequestVerificationToken"] = token
        }));

        // Assert
        await AssertStatusAsync(response, HttpStatusCode.Redirect);

        await using var db = _factory.CreateDbContext();
        var normalized = DocumentValidation.Normalize(DocumentType.Nit, nit);
        var customer = await db.Customers.SingleAsync(c => c.TaxId == normalized);
        Assert.Equal("Distribuidora 24/7 S.A. de C.V.", customer.Name);
    }

    /// <summary>An invalid phone shows a localized message instead of the resource key.</summary>
    [Fact]
    public async Task Create_WithInvalidPhone_ShowsLocalizedMessage()
    {
        // Arrange
        var client = await AdminClientAsync();
        var token = await GetAntiforgeryTokenAsync(client, "/Customers/Create");

        // Act
        var response = await client.PostAsync("/Customers/Create", Form(new Dictionary<string, string>
        {
            ["Input.Name"] = "Acme S.A.",
            ["Input.DocumentType"] = "Dui",
            ["Input.TaxId"] = NewTaxId(),
            ["Input.Phone"] = "call-me",
            ["__RequestVerificationToken"] = token
        }));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Contains("no es un número de teléfono válido", html);
        Assert.DoesNotContain("ValidationPhone", html);
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
            seedDb.Customers.Add(Customer.Create("Existing", DocumentType.Dui, taxId, null, null, null));
            await seedDb.SaveChangesAsync();
        }

        var token = await GetAntiforgeryTokenAsync(client, "/Customers/Create");

        // Act
        var response = await client.PostAsync("/Customers/Create", Form(new Dictionary<string, string>
        {
            ["Input.Name"] = "Duplicate",
            ["Input.DocumentType"] = "Dui",
            ["Input.TaxId"] = taxId,
            ["__RequestVerificationToken"] = token
        }));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var db = _factory.CreateDbContext();
        Assert.Equal(1, await db.Customers.CountAsync(c => c.TaxId == taxId));
    }

    /// <summary>Editing with valid data updates the customer.</summary>
    [Fact]
    public async Task Edit_WithValidData_UpdatesCustomer()
    {
        // Arrange
        var client = await AdminClientAsync();
        var taxId = NewTaxId();
        Guid id;
        await using (var seedDb = _factory.CreateDbContext())
        {
            var customer = Customer.Create("Before", DocumentType.Dui, taxId, null, null, null);
            seedDb.Customers.Add(customer);
            await seedDb.SaveChangesAsync();
            id = customer.Id;
        }

        var token = await GetAntiforgeryTokenAsync(client, $"/Customers/Edit/{id}");

        // Act
        var response = await client.PostAsync($"/Customers/Edit/{id}", Form(new Dictionary<string, string>
        {
            ["Input.Id"] = id.ToString(),
            ["Input.Name"] = "After",
            ["Input.DocumentType"] = "Dui",
            ["Input.TaxId"] = taxId,
            ["Input.Phone"] = "7777-7777",
            ["Input.Email"] = "after@acme.test",
            ["Input.Address"] = "New address 5",
            ["__RequestVerificationToken"] = token
        }));

        // Assert
        await AssertStatusAsync(response, HttpStatusCode.Redirect);

        await using var db = _factory.CreateDbContext();
        var updated = await db.Customers.SingleAsync(c => c.Id == id);
        Assert.Equal("After", updated.Name);
        Assert.Equal("7777-7777", updated.Phone);
        Assert.Equal("after@acme.test", updated.Email);
        Assert.Equal("New address 5", updated.Address);
    }

    /// <summary>The purchase-history page shows the customer and the coming-soon placeholder.</summary>
    [Fact]
    public async Task History_WithExistingCustomer_ShowsPlaceholder()
    {
        // Arrange
        var client = await AdminClientAsync();
        Guid id;
        await using (var seedDb = _factory.CreateDbContext())
        {
            var customer = Customer.Create("History customer", DocumentType.Dui, NewTaxId(), null, null, null);
            seedDb.Customers.Add(customer);
            await seedDb.SaveChangesAsync();
            id = customer.Id;
        }

        // Act
        var response = await client.GetAsync($"/Customers/History/{id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("History customer", body);
        Assert.Contains("Fase 5", body);
    }

    /// <summary>Deactivating marks the customer inactive instead of deleting it.</summary>
    [Fact]
    public async Task Deactivate_SoftDeletesCustomer()
    {
        // Arrange
        var client = await AdminClientAsync();
        Guid id;
        await using (var seedDb = _factory.CreateDbContext())
        {
            var customer = Customer.Create("To deactivate", DocumentType.Dui, NewTaxId(), null, null, null);
            seedDb.Customers.Add(customer);
            await seedDb.SaveChangesAsync();
            id = customer.Id;
        }

        var token = await GetAntiforgeryTokenAsync(client, "/Customers");

        // Act
        var response = await client.PostAsync($"/Customers?handler=Deactivate&id={id}", Form(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token
        }));

        // Assert
        await AssertStatusAsync(response, HttpStatusCode.Redirect);

        await using var db = _factory.CreateDbContext();
        var deactivated = await db.Customers.SingleAsync(c => c.Id == id);
        Assert.False(deactivated.IsActive);
    }

    /// <summary>An inventory manager cannot deactivate a customer, which stays active.</summary>
    [Fact]
    public async Task Deactivate_AsInventoryManager_IsForbiddenAndKeepsCustomerActive()
    {
        // Arrange
        await _factory.EnsureUserInRoleAsync("inventory@stockflow.local", "Inventory#2026", ApplicationRoles.InventoryManager);
        var client = await CreateAuthenticatedClientAsync("inventory@stockflow.local", "Inventory#2026");
        Guid id;
        await using (var seedDb = _factory.CreateDbContext())
        {
            var customer = Customer.Create("Protected", DocumentType.Dui, NewTaxId(), null, null, null);
            seedDb.Customers.Add(customer);
            await seedDb.SaveChangesAsync();
            id = customer.Id;
        }

        // Act
        var response = await client.PostAsync($"/Customers?handler=Deactivate&id={id}", Form(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = "irrelevant"
        }));

        // Assert
        await AssertStatusAsync(response, HttpStatusCode.Redirect);
        Assert.Contains("/Account/AccessDenied", response.Headers.Location!.ToString());

        await using var db = _factory.CreateDbContext();
        var unchanged = await db.Customers.SingleAsync(c => c.Id == id);
        Assert.True(unchanged.IsActive);
    }

    // Generates a unique, valid DUI so tests do not collide on the unique index.
    private static string NewTaxId()
    {
        var number = BitConverter.ToUInt32(Guid.NewGuid().ToByteArray(), 0) % 100_000_000;
        return $"{number:D8}-{number % 10}";
    }

    // Generates a unique, valid 14-digit NIT so tests do not collide on the unique index.
    private static string NewCompanyNit()
    {
        var digits = Guid.NewGuid().ToByteArray().Select(b => (char)('0' + (b % 10))).ToArray();
        return new string(digits, 0, 14);
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
