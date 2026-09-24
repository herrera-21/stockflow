using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace StockFlow.Web.Tests;

/// <summary>
/// Integration tests for the application shell: sidebar navigation, header and the removal of the
/// privacy page.
/// </summary>
[Collection(WebCollection.Name)]
public class LayoutTests
{
    private readonly StockFlowWebFactory _factory;

    public LayoutTests(StockFlowWebFactory factory)
    {
        _factory = factory;
    }

    /// <summary>The root URL redirects to the product list.</summary>
    [Fact]
    public async Task Home_RedirectsToProducts()
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // Act
        var response = await client.GetAsync("/");

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Products", response.Headers.Location!.ToString());
    }

    /// <summary>The shell renders the sidebar navigation and no longer exposes the privacy page.</summary>
    [Fact]
    public async Task Shell_RendersSidebarWithoutPrivacy()
    {
        // Arrange
        var client = await CreateAuthenticatedClientAsync();

        // Act
        var response = await client.GetAsync("/Products");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        // Decoded because Razor encodes non-ASCII characters (e.g. "Categorías") as HTML entities.
        var body = System.Net.WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Contains("class=\"app-sidebar\"", body);
        Assert.Contains("class=\"app-header\"", body);
        Assert.Contains("data-bs-theme=\"dark\"", body);
        Assert.Contains("app-nav-link", body);
        Assert.Contains("app-nav-label", body);
        Assert.Contains(">Productos<", body);
        Assert.Contains(">Categorías<", body);
        Assert.DoesNotContain("Privacidad", body);
    }

    /// <summary>An administrator sees both navigation sections with their titles.</summary>
    [Fact]
    public async Task Shell_AsAdministrator_RendersBothNavigationSections()
    {
        // Arrange
        var client = await CreateAuthenticatedClientAsync();

        // Act
        var body = System.Net.WebUtility.HtmlDecode(await client.GetStringAsync("/Products"));

        // Assert
        Assert.Contains("id=\"nav-section-inventory\">Inventario<", body);
        Assert.Contains("id=\"nav-section-commercial\">Comercial<", body);
        Assert.Contains(">Proveedores<", body);
    }

    /// <summary>
    /// The language selector is a dropdown whose options submit the culture to SetLanguage, with the
    /// current language marked.
    /// </summary>
    [Fact]
    public async Task Shell_RendersLanguageDropdown()
    {
        // Arrange
        var client = await CreateAuthenticatedClientAsync();

        // Act
        var body = System.Net.WebUtility.HtmlDecode(await client.GetStringAsync("/Products"));

        // Assert
        Assert.Contains("app-language-menu", body);
        Assert.Contains("name=\"culture\" value=\"es\"", body);
        Assert.Contains("name=\"culture\" value=\"en\"", body);
        Assert.Matches("value=\"es\"[^>]*class=\"[^\"]*active", body);
        Assert.DoesNotContain("<select name=\"culture\"", body);
    }

    /// <summary>A salesperson only sees the links of their role inside each section.</summary>
    [Fact]
    public async Task Shell_AsSalesperson_RendersOnlyAllowedLinks()
    {
        // Arrange
        var client = await CreateAuthenticatedClientAsync(StockFlowWebFactory.SalesEmail, StockFlowWebFactory.SalesPassword);

        // Act
        var body = System.Net.WebUtility.HtmlDecode(await client.GetStringAsync("/Products"));

        // Assert
        Assert.Contains(">Productos<", body);
        Assert.Contains(">Clientes<", body);
        Assert.DoesNotContain(">Categorías<", body);
        Assert.DoesNotContain(">Proveedores<", body);
    }

    /// <summary>The shell renders the sidebar toggle, its state labels and the off-canvas backdrop.</summary>
    [Fact]
    public async Task Shell_RendersSidebarToggleAndBackdrop()
    {
        // Arrange
        var client = await CreateAuthenticatedClientAsync();

        // Act
        var response = await client.GetAsync("/Products");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = System.Net.WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Contains("data-sidebar-toggle", body);
        Assert.Contains("data-label-collapse", body);
        Assert.Contains("data-label-expand", body);
        Assert.Contains("Contraer navegación", body);
        Assert.Contains("data-sidebar-backdrop", body);
    }

    // Logs in with the given seeded account (the administrator by default) and returns a client
    // that does not follow redirects.
    private async Task<HttpClient> CreateAuthenticatedClientAsync(
        string email = StockFlowWebFactory.AdminEmail,
        string password = StockFlowWebFactory.AdminPassword)
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
