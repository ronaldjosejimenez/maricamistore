using MariCamiStore.Infrastructure.Persistance;
using MariCamiStore.Model;
using Microsoft.EntityFrameworkCore;

namespace MariCamiStore.Services;

public class PaymentService(
    MariCamiStoreContext context) : IPaymentService
{
    public async Task<CustomerBalanceDto?> GetCustomerBalanceAsync(Guid customerId, Guid? organizationId)
    {
        var customer = await context.Customers.FindAsync(customerId);
        if (customer == null) return null;

        var globalBalance = await CalcBalanceAsync(customerId, ignoreOrgFilter: true);
        var orgBalance = organizationId.HasValue
            ? await CalcBalanceAsync(customerId, ignoreOrgFilter: false, explicitOrganizationId: organizationId)
            : globalBalance;

        return new CustomerBalanceDto(customerId, customer.NickName ?? customer.Name ?? "", globalBalance, orgBalance);
    }

    public async Task<(bool Success, string? Error, CustomerBalanceDto? Balance)> RegisterPaymentAsync(Guid customerId, decimal amount, Guid? organizationId)
    {
        if (organizationId == null)
            return (false, "Seleccione una organización específica para registrar el pago.", null);

        var customer = await context.Customers.FindAsync(customerId);
        if (customer == null) return (false, "Cliente no encontrado.", null);

        var config = await context.Configurations.FirstOrDefaultAsync();

        var crTimeZone = "Central America Standard Time";
        var txDate = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, crTimeZone);

        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId.Value,
            SourceId = null,
            Source = TransactionSource.Manual.Key,
            CustomerId = customerId,
            TransactionType = TransactionType.Payment.Key,
            TransactionDescription = $"Pago – {customer.NickName ?? customer.Name}",
            TransactionAmount = amount,
            TransactionDate = txDate,
            Status = TransactionStatus.Applied.Key,
            CurrencyId = config?.LocalCurrencyId ?? Guid.Empty
        };

        context.Transactions.Add(transaction);
        await context.SaveChangesAsync();

        return (true, null, await GetCustomerBalanceAsync(customerId, organizationId));
    }

    public async Task<List<SaldoReportRow>> GetSaldosReportAsync(Guid? organizationId)
    {
        var rows = await context.Transactions
            .IgnoreQueryFilters()
            .Where(t => t.CustomerId != null)
            .Where(t => organizationId == null || t.OrganizationId == organizationId.Value)
            .GroupBy(t => t.CustomerId!.Value)
            .Select(g => new
            {
                CustomerId = g.Key,
                Balance =
                    g.Where(t => t.TransactionType == "Charge").Sum(t => t.TransactionAmount) -
                    g.Where(t => t.TransactionType == "Payment").Sum(t => t.TransactionAmount) -
                    g.Where(t => t.TransactionType == "Void").Sum(t => t.TransactionAmount)
            })
            .Where(r => r.Balance != 0)
            .ToListAsync();

        var customerIds = rows.Select(r => r.CustomerId).ToList();
        var customers = await context.Customers
            .Where(c => customerIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => new {
                Name      = c.NickName != "" ? c.NickName : (c.Name ?? c.Id.ToString()),
                IsGeneric = c.IsGeneric
            });

        return rows.Select(r => {
            var cust = customers.GetValueOrDefault(r.CustomerId);
            return new SaldoReportRow(
                r.CustomerId,
                cust?.Name ?? r.CustomerId.ToString(),
                r.Balance,
                cust?.IsGeneric ?? false
            );
        }).OrderBy(r => r.CustomerName).ToList();
    }

    private async Task<decimal> CalcBalanceAsync(Guid customerId, bool ignoreOrgFilter, Guid? explicitOrganizationId = null)
    {
        var query = explicitOrganizationId.HasValue
            ? context.Transactions.IgnoreQueryFilters().Where(t => t.CustomerId == customerId && t.OrganizationId == explicitOrganizationId.Value)
            : ignoreOrgFilter
                ? context.Transactions.IgnoreQueryFilters().Where(t => t.CustomerId == customerId)
                : context.Transactions.Where(t => t.CustomerId == customerId);

        return await query.SumAsync(t =>
            t.TransactionType == "Charge" ? t.TransactionAmount :
            t.TransactionType == "Payment" || t.TransactionType == "Void" ? -t.TransactionAmount : 0m);
    }
}
