using Microsoft.Extensions.DependencyInjection;
using StockFlow.Application.Products.Commands;
using StockFlow.Application.Products.Queries;

namespace StockFlow.Application;

/// <summary>
/// Registers the Application layer use cases in the dependency injection container.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds the product command and query handlers to the service collection.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <returns>The same service collection, to allow chaining.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<CreateProductHandler>();
        services.AddScoped<UpdateProductHandler>();
        services.AddScoped<DeactivateProductHandler>();
        services.AddScoped<GetProductsPagedHandler>();
        services.AddScoped<GetProductByIdHandler>();

        return services;
    }
}
