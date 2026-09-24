using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using StockFlow.Domain;
using StockFlow.Domain.Entities;

namespace StockFlow.Web.Tests.Products;

/// <summary>
/// Integration coverage for the htmx flow of the movement history: paging returns the partial rows
/// without reloading the page.
/// </summary>
[Collection(WebCollection.Name)]
public class InventoryHtmxTests
{
    private readonly StockFlowWebFactory _factory;

    public InventoryHtmxTests(StockFlowWebFactory factory)
    {
        _factory = factory;
    }

    /// <summary>Paging the movement rows returns only the partial, honororing the requested page.</summary>
    [Fact]
    public async Task Movements_Rows_Paged_ReturnsPartial()
    {
        // Arrange
        var client = await CreateAuthenticatedClientAsync(StockFlowWebFactory.AdminEmail, StockFlowWebFactory.AdminPassword);
        var id = await SeedMovementsAsync(count: 12);

        // Act
        var response = await GetHtmxAsync(client, $"/Products/Movements/{id}?handler=Rows&pageNumber=2");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = System.Net.WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Contains("Página 2 de 2", body);
        Assert.Contains($"pageNumber=1", body);
        Assert.DoesNotContain("<!DOCTYPE", body);
    }

    // Seeds a product with the given number of movements and returns its id.
    private async Task<Guid> SeedMovementsAsync(int count)
    {
        await using var db = _factory.CreateDbContext();
        var product = Product.Create(
            $"IT-{Guid.NewGuid().ToString("N")[..8]}", $"Movements {Guid.NewGuid():N}", null,
            CategoryIds.Other, UnitOfMeasure.Unit, UnitOfMeasure.Unit, 1m, 10m, 15m, 13m, 0, 0,
            "seed", "seed@test.local", DateTimeOffset.UtcNow);

        for (var i = 0; i < count; i++)
        {
            var movement = product.ApplyMovement(
                InventoryMovementType.AdjustmentIncrease, 1m, InventoryAdjustmentReason.PhysicalCount, null,
                "seed", "seed@test.local", DateTimeOffset.UtcNow.AddMinutes(i));
            db.InventoryMovements.Add(movement);
        }

        db.Products.Add(product);
        await db.SaveChangesAsync();
        return product.Id;
    }

    // Sends a GET marked as an htmx request.
    private static async Task<HttpResponseMessage> GetHtmxAsync(HttpClient client, string url)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
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

        Assert.True(response.StatusCode == HttpStatusCode.Redirect, $"Login failed with {response.StatusCode}.");
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
