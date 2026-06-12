using MariCamiStore.Model;

namespace MariCamiStore.Services;

public interface ICatalogService
{
    // Currencies
    Task<List<Currency>> GetCurrenciesAsync();
    Task<Currency?> GetCurrencyByIdAsync(Guid id);
    Task<Currency> CreateCurrencyAsync(Currency currency);
    Task<Currency> UpdateCurrencyAsync(Currency currency);
    Task DeleteCurrencyAsync(Guid id);

    // Configuration (upsert — one per org)
    Task<Configuration?> GetConfigurationAsync();
    Task<Configuration> UpsertConfigurationAsync(Configuration config);

    // ProductTypes
    Task<List<ProductType>> GetProductTypesAsync();
    Task<List<ProductType>> GetProductTypesByCurrencyAsync(Guid currencyId);
    Task<ProductType> CreateProductTypeAsync(ProductType productType);
    Task<ProductType> UpdateProductTypeAsync(ProductType productType);
    Task DeleteProductTypeAsync(Guid id);

    // Suppliers
    Task<List<Supplier>> GetSuppliersAsync();
    Task<Supplier> CreateSupplierAsync(Supplier supplier);
    Task<Supplier> UpdateSupplierAsync(Supplier supplier);
    Task DeleteSupplierAsync(Guid id);

    // Customers (global — no org filter)
    Task<List<Customer>> GetCustomersAsync();
    Task<List<Customer>> GetPayableCustomersAsync();
    Task<Customer> CreateCustomerAsync(Customer customer);
    Task<Customer> UpdateCustomerAsync(Customer customer);
    Task DeleteCustomerAsync(Guid id);
}
