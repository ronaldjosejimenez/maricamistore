using MariCamiStore.Infrastructure.Persistance;
using MariCamiStore.Model;
using Microsoft.EntityFrameworkCore;

namespace MariCamiStore.Services;

public class PaymentService(
    MariCamiStoreContext context,
    ICurrentOrganizationService currentOrg) : IPaymentService
{
    /// <summary>Order statuses whose items feed the "Saldos de clientes" labels. Listed explicitly: a new status is not considered until added here.</summary>
    private static readonly string[] OrdersInProgress = { OrderStatus.Active.Key, OrderStatus.Delivering.Key };

    private sealed record CustomerItemStats(Guid CustomerId, int InTransit, int Received, decimal AgreedSum);

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

        // Labels (spec 021): evaluated AFTER the balance, reusing it, and only for the customers that owe money.
        var stats = await GetItemStatsAsync(rows.Where(r => r.Balance > 0).Select(r => r.CustomerId).ToList(), organizationId);

        return rows.Select(r => {
            var cust = customers.GetValueOrDefault(r.CustomerId);
            stats.TryGetValue(r.CustomerId, out var s);
            var inTransit = r.Balance > 0 ? s?.InTransit ?? 0 : 0;
            var received = r.Balance > 0 ? s?.Received ?? 0 : 0;
            var deliveredToCollect = received > 0 && inTransit == 0;
            // "Entregado - por cobrar" wins over "Sugerir adelanto": once everything was received it is no longer an advance.
            var suggestAdvance = inTransit > 0 && (s?.AgreedSum ?? 0) >= r.Balance;
            return new SaldoReportRow(
                r.CustomerId,
                cust?.Name ?? r.CustomerId.ToString(),
                r.Balance,
                cust?.IsGeneric ?? false,
                inTransit,
                suggestAdvance,
                deliveredToCollect
            );
        }).OrderBy(r => r.CustomerName).ToList();
    }

    /// <summary>Counts, per customer, the items in Active/Delivering orders that are pending (Active) or received, plus their agreed price.</summary>
    /// <remarks>Same organization scope as the balance: all organizations when <paramref name="organizationId"/> is null.</remarks>
    private async Task<Dictionary<Guid, CustomerItemStats>> GetItemStatsAsync(List<Guid> customerIds, Guid? organizationId)
    {
        if (customerIds.Count == 0) return [];

        var list = await context.OrderItems
            .IgnoreQueryFilters()
            .Where(i => customerIds.Contains(i.CustomerId)
                     && OrderItemStatus.CountedKeys.Contains(i.Status)
                     && OrdersInProgress.Contains(i.Order.Status)
                     && (organizationId == null || i.Order.OrganizationId == organizationId.Value))
            .GroupBy(i => i.CustomerId)
            .Select(g => new CustomerItemStats(
                g.Key,
                g.Count(i => i.Status == OrderItemStatus.Active.Key),
                g.Count(i => i.Status == OrderItemStatus.Received.Key),
                g.Sum(i => i.AgreedPriceInLocal)))
            .ToListAsync();

        return list.ToDictionary(x => x.CustomerId);
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
