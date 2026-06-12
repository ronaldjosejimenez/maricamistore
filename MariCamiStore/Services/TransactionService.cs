using MariCamiStore.Infrastructure.Persistance;
using MariCamiStore.Model;
using Microsoft.EntityFrameworkCore;

namespace MariCamiStore.Services;

public class TransactionService(
    MariCamiStoreContext context,
    ICurrentOrganizationService currentOrg) : ITransactionService
{
    public async Task<List<TransactionDto>> GetTransactionsAsync(TransactionFilterDto filter)
    {
        var query = from t in context.Transactions
                    join c in context.Customers on t.CustomerId equals c.Id into cg
                    from c in cg.DefaultIfEmpty()
                    join oi in context.OrderItems.IgnoreQueryFilters() on t.SourceId equals (Guid?)oi.Id into oig
                    from oi in oig.DefaultIfEmpty()
                    join o in context.Orders.IgnoreQueryFilters() on (Guid?)oi.OrderId equals (Guid?)o.Id into og
                    from o in og.DefaultIfEmpty()
                    select new { t, c, o };

        if (filter.DateFrom.HasValue)
            query = query.Where(x => x.t.TransactionDate >= filter.DateFrom.Value);
        if (filter.DateTo.HasValue)
            query = query.Where(x => x.t.TransactionDate <= filter.DateTo.Value);
        if (filter.CustomerId.HasValue)
            query = query.Where(x => x.t.CustomerId == filter.CustomerId.Value);
        if (!string.IsNullOrEmpty(filter.TransactionType))
            query = query.Where(x => x.t.TransactionType == filter.TransactionType);

        var rows = await query.OrderByDescending(x => x.t.TransactionDate).ToListAsync();

        return rows.Select(x => new TransactionDto(
            x.t.Id,
            x.o != null ? x.o.NameOfOrder : null,
            x.c != null ? (string.IsNullOrWhiteSpace(x.c.NickName) ? x.c.Name : x.c.NickName) : null,
            x.t.TransactionType,
            x.t.TransactionDescription,
            x.t.TransactionAmount,
            x.t.TransactionDate
        )).ToList();
    }

    public async Task CreateManualTransactionAsync(ManualTransactionRequest request)
    {
        var customer = await context.Customers.FindAsync(request.CustomerId)
            ?? throw new InvalidOperationException("Cliente no encontrado.");

        var config = await context.Configurations.FirstOrDefaultAsync();
        var crTimeZone = "Central America Standard Time";
        var txDate = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, crTimeZone);
        var description = string.IsNullOrWhiteSpace(request.Description)
            ? $"{request.TransactionType} manual – {(string.IsNullOrWhiteSpace(customer.NickName) ? customer.Name : customer.NickName)}"
            : request.Description;

        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            OrganizationId = currentOrg.OrganizationId,
            SourceId = null,
            Source = TransactionSource.Manual.Key,
            CustomerId = request.CustomerId,
            TransactionType = request.TransactionType,
            TransactionDescription = description,
            TransactionAmount = request.Amount,
            TransactionDate = txDate,
            Status = TransactionStatus.Applied.Key,
            CurrencyId = config?.LocalCurrencyId ?? Guid.Empty
        };

        context.Transactions.Add(transaction);
        await context.SaveChangesAsync();
    }
}
