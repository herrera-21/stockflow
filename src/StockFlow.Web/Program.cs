using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using StockFlow.Application;
using StockFlow.Infrastructure.Identity;
using StockFlow.Infrastructure.Persistence;
using StockFlow.Web;
using StockFlow.Web.Authorization;

// Application entry point: wires up Razor Pages, localization, Identity and the layered services,
// then seeds Identity data on startup. Top-level statements cannot carry XML documentation, so the
// intent comments below use the plain "//" style.
var builder = WebApplication.CreateBuilder(args);

// Spanish first: it is the default culture for this app (see SetLanguage.cshtml.cs for the switcher).
var supportedCultures = new[] { "es", "en" };

// Add services to the container.
builder.Services.AddRazorPages()
    .AddRazorPagesOptions(options =>
    {
        // Defense in depth: authorization is enforced by convention as well as by attributes, so
        // pages stay protected even if an attribute is forgotten. Everything under /Products
        // requires authentication; the management pages additionally require the policy.
        options.Conventions.AuthorizeFolder("/Products");
        options.Conventions.AuthorizePage("/Products/Create", Policies.CanManageProducts);
        options.Conventions.AuthorizePage("/Products/Edit", Policies.CanManageProducts);
    })
    .AddViewLocalization()
    .AddDataAnnotationsLocalization(options =>
    {
        // DataAnnotations error messages use keys resolved against the shared resources.
        options.DataAnnotationLocalizerProvider = (_, factory) => factory.Create(typeof(SharedResource));
    });
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.SetDefaultCulture(supportedCultures[0]);
    options.AddSupportedCultures(supportedCultures);
    options.AddSupportedUICultures(supportedCultures);
});

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

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

// Idempotent: creates the fixed roles and the seed admin account only if they do not exist yet.
using (var scope = app.Services.CreateScope())
{
    await IdentitySeeder.SeedAsync(scope.ServiceProvider);
}

app.Run();

// Exposed so integration tests can boot the app with WebApplicationFactory<Program>.
public partial class Program;
