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
    Task<List<SaldoReportRow>> GetSaldosReportAsync();
}

public record SaldoReportRow(Guid CustomerId, string CustomerName, decimal Balance, bool IsGeneric);
