using MariCamiStore.Infrastructure.Persistance;
using MariCamiStore.Model;
using Microsoft.EntityFrameworkCore;

namespace MariCamiStore.Services;

public class CatalogService(
    MariCamiStoreContext context,
    ICurrentOrganizationService currentOrg) : ICatalogService
{
    // ── Currencies ──────────────────────────────────────────────────────────

    public Task<List<Currency>> GetCurrenciesAsync() =>
        context.Currencies.OrderBy(c => c.Name).ToListAsync();

    public async Task<Currency?> GetCurrencyByIdAsync(Guid id) =>
        await context.Currencies.FindAsync(id);

    public async Task<Currency> CreateCurrencyAsync(Currency currency)
    {
        currency.Id = Guid.NewGuid();
        context.Currencies.Add(currency);
        await context.SaveChangesAsync();
        return currency;
    }

    public async Task<Currency> UpdateCurrencyAsync(Currency currency)
    {
        context.Currencies.Update(currency);
        await context.SaveChangesAsync();
        return currency;
    }

    public async Task DeleteCurrencyAsync(Guid id)
    {
        var entity = await context.Currencies.FindAsync(id);
        if (entity != null)
        {
            context.Currencies.Remove(entity);
            await context.SaveChangesAsync();
        }
    }

    // ── Configuration ────────────────────────────────────────────────────────

    public Task<Configuration?> GetConfigurationAsync() =>
        context.Configurations.FirstOrDefaultAsync();

    public async Task<Configuration> UpsertConfigurationAsync(Configuration input)
    {
        var existing = await context.Configurations.FirstOrDefaultAsync();
        if (existing == null)
        {
            input.Id = Guid.NewGuid();
            input.OrganizationId = currentOrg.OrganizationId;
            context.Configurations.Add(input);
        }
        else
        {
            existing.ExchangeRate = input.ExchangeRate;
            existing.ExchangeRateMargin = input.ExchangeRateMargin;
            existing.TaxPercentage = input.TaxPercentage;
            existing.LocalCurrencyId = input.LocalCurrencyId;
            existing.OrderCurrencyIdDefault = input.OrderCurrencyIdDefault;
            existing.ProductTypeIdDefault = input.ProductTypeIdDefault;
        }
        await context.SaveChangesAsync();
        return existing ?? input;
    }

    // ── ProductTypes ─────────────────────────────────────────────────────────

    public Task<List<ProductType>> GetProductTypesAsync() =>
        context.ProductTypes.OrderBy(p => p.Name).ToListAsync();

    public Task<List<ProductType>> GetProductTypesByCurrencyAsync(Guid currencyId) =>
        context.ProductTypes
            .Where(p => p.CurrencyId == currencyId)
            .OrderBy(p => p.Name)
            .ToListAsync();

    public async Task<ProductType> CreateProductTypeAsync(ProductType productType)
    {
        productType.Id = Guid.NewGuid();
        context.ProductTypes.Add(productType);
        await context.SaveChangesAsync();
        return productType;
    }

    public async Task<ProductType> UpdateProductTypeAsync(ProductType productType)
    {
        context.ProductTypes.Update(productType);
        await context.SaveChangesAsync();
        return productType;
    }

    public async Task DeleteProductTypeAsync(Guid id)
    {
        var entity = await context.ProductTypes.FindAsync(id);
        if (entity != null)
        {
            context.ProductTypes.Remove(entity);
            await context.SaveChangesAsync();
        }
    }

    // ── Suppliers ─────────────────────────────────────────────────────────────

    public Task<List<Supplier>> GetSuppliersAsync() =>
        context.Suppliers.OrderBy(s => s.Name).ToListAsync();

    public async Task<Supplier> CreateSupplierAsync(Supplier supplier)
    {
        supplier.Id = Guid.NewGuid();
        context.Suppliers.Add(supplier);
        await context.SaveChangesAsync();
        return supplier;
    }

    public async Task<Supplier> UpdateSupplierAsync(Supplier supplier)
    {
        context.Suppliers.Update(supplier);
        await context.SaveChangesAsync();
        return supplier;
    }

    public async Task DeleteSupplierAsync(Guid id)
    {
        var entity = await context.Suppliers.FindAsync(id);
        if (entity != null)
        {
            context.Suppliers.Remove(entity);
            await context.SaveChangesAsync();
        }
    }

    // ── Customers (global) ────────────────────────────────────────────────────

    public Task<List<Customer>> GetCustomersAsync() =>
        context.Customers.OrderBy(c => c.NickName).ToListAsync();

    public Task<List<Customer>> GetPayableCustomersAsync() =>
        context.Customers.Where(c => !c.IsGeneric).OrderBy(c => c.NickName).ToListAsync();

    public async Task<Customer> CreateCustomerAsync(Customer customer)
    {
        customer.Id = Guid.NewGuid();
        context.Customers.Add(customer);
        await context.SaveChangesAsync();
        return customer;
    }

    public async Task<Customer> UpdateCustomerAsync(Customer customer)
    {
        context.Customers.Update(customer);
        await context.SaveChangesAsync();
        return customer;
    }

    public async Task DeleteCustomerAsync(Guid id)
    {
        var entity = await context.Customers.FindAsync(id);
        if (entity != null)
        {
            context.Customers.Remove(entity);
            await context.SaveChangesAsync();
        }
    }
}
