using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using StockFlow.Application;
using StockFlow.Application.Common.Interfaces;
using StockFlow.Infrastructure.Identity;
using StockFlow.Infrastructure.Persistence;
using StockFlow.Web;
using StockFlow.Web.Authorization;
using StockFlow.Web.Services;
using StockFlow.Web.Validation;

// Application entry point: wires up Razor Pages, localization, Identity and the layered services,
// then seeds Identity data on startup. Top-level statements cannot carry XML documentation, so the
// intent comments below use the plain "//" style.
var builder = WebApplication.CreateBuilder(args);

// Spanish first: it is the default culture for this app (see SetLanguage.cshtml.cs for the switcher).
// The numeric format is overridden to use the dot as decimal separator and the comma as group
// separator, matching the HTML number inputs and the UI/UX convention. The culture is cloned so the
// shared, read-only CultureInfo from the cache is never mutated.
var spanishCulture = (CultureInfo)CultureInfo.GetCultureInfo("es").Clone();
spanishCulture.NumberFormat.NumberDecimalSeparator = ".";
spanishCulture.NumberFormat.NumberGroupSeparator = ",";
spanishCulture.NumberFormat.CurrencyDecimalSeparator = ".";
spanishCulture.NumberFormat.CurrencyGroupSeparator = ",";
spanishCulture.NumberFormat.PercentDecimalSeparator = ".";
spanishCulture.NumberFormat.PercentGroupSeparator = ",";

var englishCulture = CultureInfo.GetCultureInfo("en");

var supportedCultures = new[] { spanishCulture, englishCulture };

// Add services to the container.
builder.Services.AddRazorPages()
    .AddRazorPagesOptions(options =>
    {
        // Defense in depth: authorization is enforced by convention as well as by attributes, so
        // pages stay protected even if an attribute is forgotten. Everything under /Products and
        // /Customers requires authentication; the management pages additionally require a policy.
        options.Conventions.AuthorizeFolder("/Products");
        options.Conventions.AuthorizePage("/Products/Create", Policies.CanManageProducts);
        options.Conventions.AuthorizePage("/Products/Edit", Policies.CanManageProducts);

        // Adjusting stock moves inventory, so it requires the inventory policy; viewing the movement
        // history is allowed for anyone who can see products.
        options.Conventions.AuthorizePage("/Products/Adjust", Policies.CanAdjustInventory);

        options.Conventions.AuthorizeFolder("/Customers");
        options.Conventions.AuthorizePage("/Customers/Create", Policies.CanManageCustomers);
        options.Conventions.AuthorizePage("/Customers/Edit", Policies.CanManageCustomers);

        // Suppliers are managed by the same roles as products, so the whole folder requires the
        // manage-suppliers policy; salespeople have no access at all.
        options.Conventions.AuthorizeFolder("/Suppliers", Policies.CanManageSuppliers);

        // Categories are part of the shared catalog and only administrators may change them.
        options.Conventions.AuthorizeFolder("/Categories", Policies.CanManageCategories);
    })
    .AddViewLocalization()
    .AddDataAnnotationsLocalization(options =>
    {
        // DataAnnotations error messages use keys resolved against the shared resources.
        options.DataAnnotationLocalizerProvider = (_, factory) => factory.Create(typeof(SharedResource));
    });
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

// The built-in validation attributes are localized out of the box, but the custom ones need an
// adapter that resolves their ErrorMessage key against the shared resources; without it the raw
// key would be shown to the user.
builder.Services.Replace(
    ServiceDescriptor.Singleton<IValidationAttributeAdapterProvider, LocalizedValidationAttributeAdapterProvider>());
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    // The modified culture instances must be the ones registered, otherwise the middleware would
    // resolve the cached "es" culture (comma decimal separator) by name and the override is lost.
    options.DefaultRequestCulture = new RequestCulture(spanishCulture, spanishCulture);
    options.SupportedCultures = supportedCultures.ToList();
    options.SupportedUICultures = supportedCultures.ToList();
});

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

// Exposes the authenticated user to the application handlers (movements, audit).
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

// AppDbContext is an IdentityDbContext, so this also wires up the EF Core Identity stores.
builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthorization(Policies.Configure);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

// Must run before UseRouting so the resolved culture (cookie/query string) applies to the whole request.
app.UseRequestLocalization(app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value);

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

// Keeps the schema current on every startup (creates the database when missing) and then seeds
// the fixed roles and the seed accounts, both idempotently.
using (var scope = app.Services.CreateScope())
{
    await DatabaseInitializer.MigrateAsync(scope.ServiceProvider);
    await IdentitySeeder.SeedAsync(scope.ServiceProvider);
}

app.Run();

// Exposed so integration tests can boot the app with WebApplicationFactory<Program>.
public partial class Program;
