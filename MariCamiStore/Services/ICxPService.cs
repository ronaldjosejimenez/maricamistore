using MariCamiStore.Model;

namespace MariCamiStore.Services;

public record CxPCurrencyBalance(string CurrencyName, string Sign, decimal Amount);

public record CxPPeriodIndicatorsDto(
    Guid PeriodId,
    int TransactionMonth,
    int TransactionYear,
    decimal ExchangeRate,
    Dictionary<string, CxPCurrencyBalance> PorPagarPorMoneda,
    decimal PorPagarEnColones,
    decimal SaldosPorCobrar,
    decimal PagosRealizados,
    decimal DeudaAPagar,
    decimal EnCuenta,
    decimal PendienteDeRecoger,
    decimal ShippingCRPendientesDeAplicar,
    decimal Posicion,
    bool IsClosed,
    bool ExchangeRateWarning);

public record CxPEntryDto(
    Guid Id,
    Guid CurrencyId,
    string CurrencyName,
    string Sign,
    decimal Amount,
    string Reference,
    string Type,
    Guid? OrderId,
    DateTime CreatedAt);

public record CreateManualCxPEntryRequest(Guid CurrencyId, decimal Amount, string Reference);

public record UpdatePeriodFieldsRequest(decimal ExchangeRate, decimal PagosRealizados, decimal EnCuenta);

public record InitPeriodRequest(int Month, int Year, decimal ExchangeRate);

public record DeleteEntryRequest(Guid EntryId);

public interface ICxPService
{
    Task<PeriodControl?> GetOpenPeriodAsync();
    Task<PeriodControl> InitializePeriodAsync(int month, int year, decimal exchangeRate);
    Task<CxPPeriodIndicatorsDto> GetPeriodIndicatorsAsync(Guid periodId);
    Task<List<CxPEntryDto>> GetEntriesByPeriodAsync(Guid periodId);
    Task<CxPEntry> CreateManualEntryAsync(Guid periodId, CreateManualCxPEntryRequest req);
    Task DeleteEntryAsync(Guid entryId);
    Task<CxPEntry> CreateAutoEntryAsync(Guid periodId, Guid orderId, Guid currencyId, decimal amount, string reference, string type);
    Task<PeriodControl> UpdatePeriodFieldsAsync(Guid periodId, UpdatePeriodFieldsRequest req);
    Task ClosePeriodAsync(Guid periodId);
}
