using MariCamiStore.Infrastructure.Persistance;
using MariCamiStore.Model;
using Microsoft.EntityFrameworkCore;

namespace MariCamiStore.Services;

public class PaymentService(
    MariCamiStoreContext context,
    ICurrentOrganizationService currentOrg) : IPaymentService
{
    public async Task<CustomerBalanceDto?> GetCustomerBalanceAsync(Guid customerId)
    {
        var customer = await context.Customers.FindAsync(customerId);
        if (customer == null) return null;

        var globalBalance = await CalcBalanceAsync(customerId, ignoreOrgFilter: true);
        var orgBalance = await CalcBalanceAsync(customerId, ignoreOrgFilter: false);

        return new CustomerBalanceDto(customerId, customer.NickName ?? customer.Name ?? "", globalBalance, orgBalance);
    }

    public async Task<CustomerBalanceDto?> RegisterPaymentAsync(Guid customerId, decimal amount)
    {
        var customer = await context.Customers.FindAsync(customerId);
        if (customer == null) return null;

        var config = await context.Configurations.FirstOrDefaultAsync();

        var crTimeZone = "Central America Standard Time";
        var txDate = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, crTimeZone);

        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            OrganizationId = currentOrg.OrganizationId,
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

        return await GetCustomerBalanceAsync(customerId);
    }

    public async Task<List<SaldoReportRow>> GetSaldosReportAsync()
    {
        var rows = await context.Transactions
            .IgnoreQueryFilters()
            .Where(t => t.CustomerId != null)
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

    private async Task<decimal> CalcBalanceAsync(Guid customerId, bool ignoreOrgFilter)
    {
        var query = ignoreOrgFilter
            ? context.Transactions.IgnoreQueryFilters().Where(t => t.CustomerId == customerId)
            : context.Transactions.Where(t => t.CustomerId == customerId);

        return await query.SumAsync(t =>
            t.TransactionType == "Charge" ? t.TransactionAmount :
            t.TransactionType == "Payment" || t.TransactionType == "Void" ? -t.TransactionAmount : 0m);
    }
}
