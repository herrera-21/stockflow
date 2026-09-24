using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using StockFlow.Domain;
using StockFlow.Domain.Entities;

namespace StockFlow.Web.Tests.Suppliers;

/// <summary>
/// Integration coverage for the supplier htmx flows: live search returns the partial rows, and
/// deactivating a supplier swaps a single row without reloading the page.
/// </summary>
[Collection(WebCollection.Name)]
public class SupplierHtmxTests
{
    private readonly StockFlowWebFactory _factory;

    public SupplierHtmxTests(StockFlowWebFactory factory)
    {
        _factory = factory;
    }

    /// <summary>The index page renders the htmx attributes and antiforgery tokens used by the flows.</summary>
    [Fact]
    public async Task Index_RendersLiveSearchControls()
    {
        // Arrange
        var client = await AdminClientAsync();
        var name = $"Live search {Guid.NewGuid():N}";
        var id = await SeedAsync(name);

        // Act: filter down to the seeded supplier so it is guaranteed to be on the rendered page.
        var response = await client.GetAsync($"/Suppliers?search={Uri.EscapeDataString(name)}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("hx-get=\"/Suppliers?handler=Rows\"", body);
        Assert.Contains("hx-target=\"#suppliers-container\"", body);
        Assert.Contains("hx-swap=\"innerHTML\"", body);
        Assert.Contains("hx-trigger=\"keyup changed delay:300ms", body);
        Assert.Contains("id=\"suppliers-container\"", body);
        Assert.Contains("/lib/htmx/htmx.min.js", body);

        // The deactivate form posts over htmx and carries the antiforgery token, so htmx POSTs are
        // not rejected with 400.
        Assert.Contains($"hx-post=\"/Suppliers?id={id}", body);
        Assert.Contains("handler=Deactivate", body);
    }

    /// <summary>The rows endpoint filters by name and returns only matching rows without a layout.</summary>
    [Fact]
    public async Task Rows_FiltersByName_ReturnsOnlyMatchingRowsWithoutLayout()
    {
        // Arrange
        var client = await AdminClientAsync();
        var matchName = $"Match {Guid.NewGuid():N}";
        var otherName = $"Other {Guid.NewGuid():N}";
        await SeedAsync(matchName);
        await SeedAsync(otherName);

        // Act
        var response = await GetHtmxAsync(client, $"/Suppliers?handler=Rows&search={Uri.EscapeDataString(matchName)}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains(matchName, body);
        Assert.DoesNotContain(otherName, body);
        Assert.DoesNotContain("<!DOCTYPE", body);
    }

    /// <summary>Search also matches the tax identification.</summary>
    [Fact]
    public async Task Rows_FiltersByTaxId_ReturnsOnlyMatchingRows()
    {
        // Arrange
        var client = await AdminClientAsync();
        var matchTaxId = NewTaxId();
        var otherTaxId = NewTaxId();
        await SeedAsync("Match supplier", matchTaxId);
        await SeedAsync("Other supplier", otherTaxId);

        // Act
        var response = await GetHtmxAsync(client, $"/Suppliers?handler=Rows&search={Uri.EscapeDataString(matchTaxId)}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Match supplier", body);
        Assert.DoesNotContain("Other supplier", body);
    }

    /// <summary>No matches render the localized empty state.</summary>
    [Fact]
    public async Task Rows_WithNoMatches_ShowsEmptyState()
    {
        // Arrange
        var client = await AdminClientAsync();

        // Act
        var response = await GetHtmxAsync(client, $"/Suppliers?handler=Rows&search={Guid.NewGuid():N}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("No hay proveedores para mostrar.", body);
    }

    /// <summary>Paging the rows keeps the search filter applied.</summary>
    [Fact]
    public async Task Rows_Pagination_PreservesSearchFilter()
    {
        // Arrange
        var client = await AdminClientAsync();
        var prefix = $"Paged{Guid.NewGuid():N}";
        await using (var db = _factory.CreateDbContext())
        {
            for (var i = 1; i <= 12; i++)
            {
                db.Suppliers.Add(Supplier.Create($"{prefix} {i:D2}", DocumentType.Dui, NewTaxId(), null, null, null, null));
            }

            await db.SaveChangesAsync();
        }

        // Act
        var firstPage = await GetHtmxAsync(client, $"/Suppliers?handler=Rows&search={prefix}");
        var secondPage = await GetHtmxAsync(client, $"/Suppliers?handler=Rows&search={prefix}&pageNumber=2");

        // Assert
        var firstBody = await firstPage.Content.ReadAsStringAsync();
        var secondBody = System.Net.WebUtility.HtmlDecode(await secondPage.Content.ReadAsStringAsync());
        Assert.Contains("pageNumber=2", firstBody);
        Assert.Contains($"search={prefix}", firstBody);

        // The second page still honors the search filter and shows the remaining rows.
        Assert.Contains("Página 2 de 2", secondBody);
        Assert.Contains($"{prefix} 11", secondBody);
        Assert.Contains($"{prefix} 12", secondBody);
    }

    /// <summary>Deactivating over htmx returns the updated row and persists the change.</summary>
    [Fact]
    public async Task Deactivate_ViaHtmx_DeactivatesAndReturnsUpdatedRow()
    {
        // Arrange
        var client = await AdminClientAsync();
        var id = await SeedAsync("Htmx deactivate");
        var token = await GetAntiforgeryTokenAsync(client, "/Suppliers");

        // Act
        var response = await PostHtmxAsync(client, $"/Suppliers?handler=Deactivate&id={id}", token);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains($"supplier-row-{id}", body);
        Assert.Contains("Inactivo", body);
        Assert.DoesNotContain("Desactivar", body);
        Assert.DoesNotContain("<!DOCTYPE", body);

        await using var db = _factory.CreateDbContext();
        var deactivated = await db.Suppliers.SingleAsync(s => s.Id == id);
        Assert.False(deactivated.IsActive);
    }

    /// <summary>A salesperson's htmx deactivate is forbidden and the supplier stays active.</summary>
    [Fact]
    public async Task Deactivate_ViaHtmx_AsSalesperson_IsForbiddenAndKeepsSupplierActive()
    {
        // Arrange
        await _factory.EnsureUserInRoleAsync("sales@stockflow.local", "Sales#2026", ApplicationRoles.Salesperson);
        var client = await CreateAuthenticatedClientAsync("sales@stockflow.local", "Sales#2026");
        var id = await SeedAsync("Htmx protected");
        var token = await GetAntiforgeryTokenAsync(client, "/Account/Login");

        // Act
        var response = await PostHtmxAsync(client, $"/Suppliers?handler=Deactivate&id={id}", token);

        // Assert
        // Cookie authentication turns Forbid() into a redirect to the access-denied page.
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
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

    // Persists a supplier with the given name and returns its id.
    private async Task<Guid> SeedAsync(string name, string? taxId = null)
    {
        await using var db = _factory.CreateDbContext();
        var supplier = Supplier.Create(name, DocumentType.Dui, taxId ?? NewTaxId(), null, null, null, null);
        db.Suppliers.Add(supplier);
        await db.SaveChangesAsync();
        return supplier.Id;
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

    // Returns an authenticated client for the seeded administrator.
    private Task<HttpClient> AdminClientAsync() =>
        CreateAuthenticatedClientAsync(StockFlowWebFactory.AdminEmail, StockFlowWebFactory.AdminPassword);

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
