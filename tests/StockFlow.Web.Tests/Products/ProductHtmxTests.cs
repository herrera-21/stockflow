using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using StockFlow.Domain;
using StockFlow.Domain.Entities;

namespace StockFlow.Web.Tests.Products;

/// <summary>
/// Integration coverage for the htmx flows: live search/filter returns the partial rows, and
/// deactivating a product swaps a single row without reloading the page.
/// </summary>
[Collection(WebCollection.Name)]
public class ProductHtmxTests
{
    private readonly StockFlowWebFactory _factory;

    public ProductHtmxTests(StockFlowWebFactory factory)
    {
        _factory = factory;
    }

    /// <summary>The htmx script is served from wwwroot.</summary>
    [Fact]
    public async Task HtmxScript_IsServedFromWwwroot()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/lib/htmx/htmx.min.js");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>The index page renders the htmx attributes and antiforgery tokens used by the flows.</summary>
    [Fact]
    public async Task Index_RendersLiveSearchControls()
    {
        // Arrange
        var client = await CreateAuthenticatedClientAsync(StockFlowWebFactory.AdminEmail, StockFlowWebFactory.AdminPassword);
        var name = $"Live search {Guid.NewGuid():N}";
        var id = await SeedAsync(name, CategoryIds.Other);

        // Act: filter down to the seeded product so it is guaranteed to be on the rendered page.
        var response = await client.GetAsync($"/Products?search={Uri.EscapeDataString(name)}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("hx-get=\"/Products?handler=Rows\"", body);
        Assert.Contains("hx-target=\"#products-container\"", body);
        Assert.Contains("hx-swap=\"innerHTML\"", body);
        Assert.Contains("hx-trigger=\"keyup changed delay:300ms", body);
        Assert.Contains("id=\"products-container\"", body);
        Assert.Contains("/lib/htmx/htmx.min.js", body);

        // The deactivate form posts over htmx and carries the antiforgery token (the logout form
        // contributes another one), so htmx POSTs are not rejected with 400.
        Assert.Contains($"hx-post=\"/Products?id={id}", body);
        Assert.Contains("handler=Deactivate", body);
        Assert.Contains("pageNumber=", body);
        Assert.True(
            Regex.Matches(body, "name=\"__RequestVerificationToken\"").Count >= 2,
            "Expected an antiforgery token in both the logout and the deactivate forms.");
    }

    /// <summary>The rows endpoint filters by name and returns only matching rows without a layout.</summary>
    [Fact]
    public async Task Rows_FiltersByName_ReturnsOnlyMatchingRowsWithoutLayout()
    {
        // Arrange
        var client = await CreateAuthenticatedClientAsync(StockFlowWebFactory.AdminEmail, StockFlowWebFactory.AdminPassword);
        var matchName = $"Match {Guid.NewGuid():N}";
        var otherName = $"Other {Guid.NewGuid():N}";
        await SeedAsync(matchName, CategoryIds.Other);
        await SeedAsync(otherName, CategoryIds.Other);

        // Act
        var response = await GetHtmxAsync(client, $"/Products?handler=Rows&search={Uri.EscapeDataString(matchName)}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains(matchName, body);
        Assert.DoesNotContain(otherName, body);
        Assert.DoesNotContain("<!DOCTYPE", body);
    }

    /// <summary>The rows endpoint filters by category and returns only matching rows.</summary>
    [Fact]
    public async Task Rows_FiltersByCategory_ReturnsOnlyMatchingRows()
    {
        // Arrange
        var client = await CreateAuthenticatedClientAsync(StockFlowWebFactory.AdminEmail, StockFlowWebFactory.AdminPassword);
        var matchName = $"In {Guid.NewGuid():N}";
        var otherName = $"Out {Guid.NewGuid():N}";
        var matchCategoryId = await SeedCategoryAsync();
        var otherCategoryId = await SeedCategoryAsync();
        await SeedAsync(matchName, matchCategoryId);
        await SeedAsync(otherName, otherCategoryId);

        // Act
        var response = await GetHtmxAsync(client, $"/Products?handler=Rows&categoryId={matchCategoryId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains(matchName, body);
        Assert.DoesNotContain(otherName, body);
    }

    /// <summary>No matches render the localized empty state.</summary>
    [Fact]
    public async Task Rows_WithNoMatches_ShowsEmptyState()
    {
        // Arrange
        var client = await CreateAuthenticatedClientAsync(StockFlowWebFactory.AdminEmail, StockFlowWebFactory.AdminPassword);

        // Act
        var response = await GetHtmxAsync(client, $"/Products?handler=Rows&search={Guid.NewGuid():N}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("No hay productos para mostrar.", body);
    }

    /// <summary>Paging the rows keeps the category filter applied.</summary>
    [Fact]
    public async Task Rows_Pagination_PreservesCategoryFilter()
    {
        // Arrange
        var client = await CreateAuthenticatedClientAsync(StockFlowWebFactory.AdminEmail, StockFlowWebFactory.AdminPassword);
        var categoryId = await SeedCategoryAsync();
        await using (var db = _factory.CreateDbContext())
        {
            for (var i = 1; i <= 12; i++)
            {
                db.Products.Add(Product.Create(NewSku(), $"Paged {i:D2}", null, categoryId, UnitOfMeasure.Unit, UnitOfMeasure.Unit, 1m, 10m, 15m, 13m, 5, 2, "user-1", "user@test.local", DateTimeOffset.UnixEpoch));
            }

            await db.SaveChangesAsync();
        }

        // Act
        var firstPage = await GetHtmxAsync(client, $"/Products?handler=Rows&categoryId={categoryId}");
        var secondPage = await GetHtmxAsync(client, $"/Products?handler=Rows&categoryId={categoryId}&pageNumber=2");

        // Assert
        var firstBody = await firstPage.Content.ReadAsStringAsync();
        var secondBody = System.Net.WebUtility.HtmlDecode(await secondPage.Content.ReadAsStringAsync());
        Assert.Contains("pageNumber=2", firstBody);
        Assert.Contains($"categoryId={categoryId}", firstBody);

        // The second page still honors the category filter and shows the remaining rows.
        Assert.Contains("Página 2 de 2", secondBody);
        Assert.Contains("Paged 11", secondBody);
        Assert.Contains("Paged 12", secondBody);
    }

    /// <summary>Deactivating over htmx returns the updated row and persists the change.</summary>
    [Fact]
    public async Task Deactivate_ViaHtmx_DeactivatesAndReturnsUpdatedRow()
    {
        // Arrange
        var client = await CreateAuthenticatedClientAsync(StockFlowWebFactory.AdminEmail, StockFlowWebFactory.AdminPassword);
        var id = await SeedAsync("Htmx deactivate", CategoryIds.Other);
        var token = await GetAntiforgeryTokenAsync(client, "/Products");

        // Act
        var response = await PostHtmxAsync(client, $"/Products?handler=Deactivate&id={id}", token);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains($"product-row-{id}", body);
        Assert.Contains("Inactivo", body);
        Assert.DoesNotContain("Desactivar", body);
        Assert.DoesNotContain("<!DOCTYPE", body);

        await using var db = _factory.CreateDbContext();
        var deactivated = await db.Products.SingleAsync(p => p.Id == id);
        Assert.False(deactivated.IsActive);
    }

    /// <summary>A salesperson's htmx deactivate is forbidden and the product stays active.</summary>
    [Fact]
    public async Task Deactivate_ViaHtmx_AsSalesperson_IsForbiddenAndKeepsProductActive()
    {
        // Arrange
        await _factory.EnsureUserInRoleAsync("sales@stockflow.local", "Sales#2026", ApplicationRoles.Salesperson);
        var client = await CreateAuthenticatedClientAsync("sales@stockflow.local", "Sales#2026");
        var id = await SeedAsync("Htmx protected", CategoryIds.Other);
        var token = await GetAntiforgeryTokenAsync(client, "/Products");

        // Act
        var response = await PostHtmxAsync(client, $"/Products?handler=Deactivate&id={id}", token);

        // Assert
        // Cookie authentication turns Forbid() into a redirect to the access-denied page.
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/AccessDenied", response.Headers.Location!.ToString());

        await using var db = _factory.CreateDbContext();
        var unchanged = await db.Products.SingleAsync(p => p.Id == id);
        Assert.True(unchanged.IsActive);
    }

    // Generates a unique SKU so tests do not collide on the unique index.
    private static string NewSku() => $"IT-{Guid.NewGuid().ToString("N")[..8]}";

    // Persists a product with the given name and category and returns its id.
    private async Task<Guid> SeedAsync(string name, Guid categoryId)
    {
        await using var db = _factory.CreateDbContext();
        var product = Product.Create(NewSku(), name, null, categoryId, UnitOfMeasure.Unit, UnitOfMeasure.Unit, 1m, 10m, 15m, 13m, 5, 2, "user-1", "user@test.local", DateTimeOffset.UnixEpoch);
        db.Products.Add(product);
        await db.SaveChangesAsync();
        return product.Id;
    }

    // Persists a fresh category with a unique name and returns its id.
    private async Task<Guid> SeedCategoryAsync()
    {
        await using var db = _factory.CreateDbContext();
        var category = Category.Create($"Cat {Guid.NewGuid():N}");
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        return category.Id;
    }

    // Sends a GET marked as an htmx request.
    private static async Task<HttpResponseMessage> GetHtmxAsync(HttpClient client, string url)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("HX-Request", "true");
        return await client.SendAsync(request);
    }

    // Sends a POST marked as an htmx request, carrying the antiforgery token.
    private static async Task<HttpResponseMessage> PostHtmxAsync(HttpClient client, string url, string antiforgeryToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = antiforgeryToken
            })
        };
        request.Headers.Add("HX-Request", "true");
        return await client.SendAsync(request);
    }

    // Logs in with the given credentials and returns a client that does not follow redirects.
    private async Task<HttpClient> CreateAuthenticatedClientAsync(string email, string password)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var token = await GetAntiforgeryTokenAsync(client, "/Account/Login");

        var response = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.Email"] = email,
            ["Input.Password"] = password,
            ["__RequestVerificationToken"] = token
        }));

        Assert.True(
            response.StatusCode == HttpStatusCode.Redirect,
            $"Login failed with {response.StatusCode}.");
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
