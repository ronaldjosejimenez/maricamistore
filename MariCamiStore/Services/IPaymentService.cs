namespace MariCamiStore.Services;

public record CustomerBalanceDto(
    Guid CustomerId,
    string CustomerName,
    decimal GlobalBalance,
    decimal OrgBalance);

public interface IPaymentService
{
    Task<CustomerBalanceDto?> GetCustomerBalanceAsync(Guid customerId, Guid? organizationId);
    Task<(bool Success, string? Error, CustomerBalanceDto? Balance)> RegisterPaymentAsync(Guid customerId, decimal amount, Guid? organizationId);
    Task<List<SaldoReportRow>> GetSaldosReportAsync(Guid? organizationId);
}

public record SaldoReportRow(Guid CustomerId, string CustomerName, decimal Balance, bool IsGeneric);
