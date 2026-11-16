namespace MariCamiStore.Services;

public record CustomerBalanceDto(
    Guid CustomerId,
    string CustomerName,
    decimal GlobalBalance,
    decimal OrgBalance);

public interface IPaymentService
{
    Task<CustomerBalanceDto?> GetCustomerBalanceAsync(Guid customerId);
    Task<CustomerBalanceDto?> RegisterPaymentAsync(Guid customerId, decimal amount);
    Task<List<SaldoReportRow>> GetSaldosReportAsync(Guid? organizationId);
}

/// <summary>A row of "Saldos de clientes".</summary>
/// <param name="ItemsInTransit">Items still pending reception (Active) in the customer's Active/Delivering orders.</param>
/// <param name="SuggestAdvance">The customer's balance is at least the sum of the pending items, i.e. nothing has been contributed yet, so an advance is worth asking for.</param>
/// <param name="DeliveredToCollect">A balance remains and nothing is left to deliver in the customer's Active/Delivering orders (also when it has none).</param>
/// <remarks>The three label fields are only evaluated when <c>Balance &gt; 0</c>; otherwise they are 0 / false.</remarks>
public record SaldoReportRow(
    Guid CustomerId, string CustomerName, decimal Balance, bool IsGeneric,
    int ItemsInTransit, bool SuggestAdvance, bool DeliveredToCollect);
