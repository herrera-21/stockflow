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

    // Logs in with the seeded administrator and returns a client that does not follow redirects.
    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var token = await GetAntiforgeryTokenAsync(client, "/Account/Login");

        var response = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.Email"] = StockFlowWebFactory.AdminEmail,
            ["Input.Password"] = StockFlowWebFactory.AdminPassword,
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
