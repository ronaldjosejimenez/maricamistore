namespace MariCamiStore.Services;

public record TransactionDto(
    Guid Id,
    string? OrderName,
    string? CustomerName,
    string TransactionType,
    string TransactionDescription,
    decimal TransactionAmount,
    DateTime TransactionDate);

public record TransactionFilterDto(
    DateTime? DateFrom,
    DateTime? DateTo,
    Guid? CustomerId,
    string? TransactionType);

public record ManualTransactionRequest(
    Guid CustomerId,
    string TransactionType,
    decimal Amount,
    string? Description);

public interface ITransactionService
{
    Task<List<TransactionDto>> GetTransactionsAsync(TransactionFilterDto filter);
    Task CreateManualTransactionAsync(ManualTransactionRequest request);
}
