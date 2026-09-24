using Microsoft.Extensions.DependencyInjection;
using StockFlow.Application.Categories.Commands;
using StockFlow.Application.Categories.Queries;
using StockFlow.Application.Customers.Commands;
using StockFlow.Application.Customers.Queries;
using StockFlow.Application.Products.Commands;
using StockFlow.Application.Products.Queries;
using StockFlow.Application.Suppliers.Commands;
using StockFlow.Application.Suppliers.Queries;

namespace StockFlow.Application;

/// <summary>
/// Registers the Application layer use cases in the dependency injection container.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds the product, customer and supplier command and query handlers to the service collection.
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

        services.AddScoped<CreateCustomerHandler>();
        services.AddScoped<UpdateCustomerHandler>();
        services.AddScoped<DeactivateCustomerHandler>();
        services.AddScoped<GetCustomersPagedHandler>();
        services.AddScoped<GetCustomerByIdHandler>();

        services.AddScoped<CreateSupplierHandler>();
        services.AddScoped<UpdateSupplierHandler>();
        services.AddScoped<DeactivateSupplierHandler>();
        services.AddScoped<GetSuppliersPagedHandler>();
        services.AddScoped<GetSupplierByIdHandler>();
        services.AddScoped<AssignSupplierProductHandler>();
        services.AddScoped<RemoveSupplierProductHandler>();
        services.AddScoped<GetSupplierProductsHandler>();

        services.AddScoped<CreateCategoryHandler>();
        services.AddScoped<UpdateCategoryHandler>();
        services.AddScoped<DeactivateCategoryHandler>();
        services.AddScoped<ActivateCategoryHandler>();
        services.AddScoped<GetCategoriesHandler>();
        services.AddScoped<GetCategoriesPagedHandler>();
        services.AddScoped<GetCategoryByIdHandler>();

        return services;
    }
}
