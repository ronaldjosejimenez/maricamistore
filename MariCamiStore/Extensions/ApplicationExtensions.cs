using MariCamiStore.Services;

namespace MariCamiStore.Extensions
{
    public static class ApplicationExtensions
    {
        public static IServiceCollection AddServices(this IServiceCollection services)
        {
            services.AddScoped<ICurrentOrganizationService, CurrentOrganizationService>();
            services.AddScoped<IOrganizationService, OrganizationService>();
            services.AddScoped<ICatalogService, CatalogService>();
            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<IPaymentService, PaymentService>();
            services.AddTransient<IConfigurationManagmentService, ConfigurationManagmentService>();
            services.AddTransient<ISalesService, SalesService>();
            services.AddScoped<ITransactionService, TransactionService>();
            services.AddScoped<ICxPService, CxPService>();

            return services;
        }
    }
}
