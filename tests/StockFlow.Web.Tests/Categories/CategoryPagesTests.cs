using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using StockFlow.Domain;
using StockFlow.Domain.Entities;

namespace StockFlow.Web.Tests.Categories;

/// <summary>
/// Integration tests for the category Razor Pages: CRUD, the in-use guard and administrator-only
/// access.
/// </summary>
[Collection(WebCollection.Name)]
public class CategoryPagesTests
{
    private readonly StockFlowWebFactory _factory;

    public CategoryPagesTests(StockFlowWebFactory factory)
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
        var response = await client.GetAsync("/Categories");

        // Assert
        await AssertStatusAsync(response, HttpStatusCode.Redirect);
        Assert.Contains("/Account/Login", response.Headers.Location!.ToString());
    }

    /// <summary>An inventory manager cannot access the category list.</summary>
    [Fact]
    public async Task Index_AsInventoryManager_IsForbidden()
    {
        // Arrange
        await _factory.EnsureUserInRoleAsync("inventory@stockflow.local", "Inventory#2026", ApplicationRoles.InventoryManager);
        var client = await CreateAuthenticatedClientAsync("inventory@stockflow.local", "Inventory#2026");

        // Act
        var response = await client.GetAsync("/Categories");

        // Assert
        await AssertStatusAsync(response, HttpStatusCode.Redirect);
        Assert.Contains("/Account/AccessDenied", response.Headers.Location!.ToString());
    }

    /// <summary>The administrator can view the category list and its seeded categories.</summary>
    [Fact]
    public async Task Index_AsAdmin_ShowsSeededCategories()
    {
        // Arrange
        var client = await AdminClientAsync();

        // Act: filter by a seeded name so the assertion is independent of other rows.
        var response = await client.GetAsync("/Categories?search=Limpieza");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Limpieza", body);
    }

    /// <summary>Creating with a valid name persists an active category.</summary>
    [Fact]
    public async Task Create_WithValidName_PersistsCategory()
    {
        // Arrange
        var client = await AdminClientAsync();
        var name = $"Cat {Guid.NewGuid():N}";
        var token = await GetAntiforgeryTokenAsync(client, "/Categories/Create");

        // Act
        var response = await client.PostAsync("/Categories/Create", Form(new Dictionary<string, string>
        {
            ["Input.Name"] = name,
            ["__RequestVerificationToken"] = token
        }));

        // Assert
        await AssertStatusAsync(response, HttpStatusCode.Redirect);

        await using var db = _factory.CreateDbContext();
        var category = await db.Categories.SingleAsync(c => c.Name == name);
        Assert.True(category.IsActive);
        Assert.Null(category.Code);
    }

    /// <summary>Creating a duplicate name re-renders the form and does not persist.</summary>
    [Fact]
    public async Task Create_WithDuplicateName_ShowsErrorAndDoesNotPersist()
    {
        // Arrange
        var client = await AdminClientAsync();
        var token = await GetAntiforgeryTokenAsync(client, "/Categories/Create");

        // Act
        var response = await client.PostAsync("/Categories/Create", Form(new Dictionary<string, string>
        {
            ["Input.Name"] = "Limpieza",
            ["__RequestVerificationToken"] = token
        }));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var db = _factory.CreateDbContext();
        Assert.Equal(1, await db.Categories.CountAsync(c => c.Name == "Limpieza"));
    }

    /// <summary>Editing with a valid name updates the category.</summary>
    [Fact]
    public async Task Edit_WithValidName_UpdatesCategory()
    {
        // Arrange
        var client = await AdminClientAsync();
        var name = $"Edit {Guid.NewGuid():N}";
        Guid id;
        await using (var seedDb = _factory.CreateDbContext())
        {
            var category = Category.Create(name);
            seedDb.Categories.Add(category);
            await seedDb.SaveChangesAsync();
            id = category.Id;
        }

        var token = await GetAntiforgeryTokenAsync(client, $"/Categories/Edit/{id}");

        // Act
        var response = await client.PostAsync($"/Categories/Edit/{id}", Form(new Dictionary<string, string>
        {
            ["Input.Id"] = id.ToString(),
            ["Input.Name"] = name + " renamed",
            ["__RequestVerificationToken"] = token
        }));

        // Assert
        await AssertStatusAsync(response, HttpStatusCode.Redirect);

        await using var db = _factory.CreateDbContext();
        var updated = await db.Categories.SingleAsync(c => c.Id == id);
        Assert.Equal(name + " renamed", updated.Name);
    }

    /// <summary>Deactivating an unused category marks it inactive.</summary>
    [Fact]
    public async Task Deactivate_WithNoProducts_DeactivatesCategory()
    {
        // Arrange
        var client = await AdminClientAsync();
        Guid id;
        await using (var seedDb = _factory.CreateDbContext())
        {
            var category = Category.Create($"Free {Guid.NewGuid():N}");
            seedDb.Categories.Add(category);
            await seedDb.SaveChangesAsync();
            id = category.Id;
        }

        var token = await GetAntiforgeryTokenAsync(client, "/Categories");

        // Act
        var response = await client.PostAsync($"/Categories?handler=Deactivate&id={id}", Form(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token
        }));

        // Assert
        await AssertStatusAsync(response, HttpStatusCode.Redirect);

        await using var db = _factory.CreateDbContext();
        var deactivated = await db.Categories.SingleAsync(c => c.Id == id);
        Assert.False(deactivated.IsActive);
    }

    /// <summary>Deactivating a category in use is refused and keeps it active.</summary>
    [Fact]
    public async Task Deactivate_WithProducts_IsRefusedAndKeepsCategoryActive()
    {
        // Arrange
        var client = await AdminClientAsync();
        Guid categoryId;
        await using (var seedDb = _factory.CreateDbContext())
        {
            var category = Category.Create($"In use {Guid.NewGuid():N}");
            seedDb.Categories.Add(category);
            await seedDb.SaveChangesAsync();
            categoryId = category.Id;

            var sku = "IT-" + Guid.NewGuid().ToString("N")[..8];
            seedDb.Products.Add(Product.Create(sku, "Uses category", null, categoryId, UnitOfMeasure.Unit, UnitOfMeasure.Unit, 1m, 10m, 15m, 13m, 1, 0));
            await seedDb.SaveChangesAsync();
        }

        var token = await GetAntiforgeryTokenAsync(client, "/Categories");

        // Act
        var response = await client.PostAsync($"/Categories?handler=Deactivate&id={categoryId}", Form(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token
        }));

        // Assert
        await AssertStatusAsync(response, HttpStatusCode.Redirect);

        await using var db = _factory.CreateDbContext();
        var persisted = await db.Categories.SingleAsync(c => c.Id == categoryId);
        Assert.True(persisted.IsActive);
    }

    /// <summary>Activating a deactivated category marks it active again.</summary>
    [Fact]
    public async Task Activate_WithDeactivatedCategory_ActivatesCategory()
    {
        // Arrange
        var client = await AdminClientAsync();
        Guid id;
        await using (var seedDb = _factory.CreateDbContext())
        {
            var category = Category.Create($"Inactive {Guid.NewGuid():N}");
            category.Deactivate();
            seedDb.Categories.Add(category);
            await seedDb.SaveChangesAsync();
            id = category.Id;
        }

        var token = await GetAntiforgeryTokenAsync(client, "/Categories");

        // Act
        var response = await client.PostAsync($"/Categories?handler=Activate&id={id}", Form(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token
        }));

        // Assert
        await AssertStatusAsync(response, HttpStatusCode.Redirect);

        await using var db = _factory.CreateDbContext();
        var activated = await db.Categories.SingleAsync(c => c.Id == id);
        Assert.True(activated.IsActive);
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
