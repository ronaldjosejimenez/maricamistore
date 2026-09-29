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

public record ClosePeriodRequest(Guid PeriodId, decimal ExchangeRate, decimal EnCuenta);

public record ClosePreviewNewPeriodDto(
    int Month,
    int Year,
    decimal ExchangeRate,
    decimal ProposedExchangeRate,
    decimal PagosRealizados,
    decimal EnCuenta,
    decimal ProposedEnCuenta,
    decimal? SaldoAnterior,
    CxPPeriodIndicatorsDto Indicators);

public record ClosePreviewDto(
    CxPPeriodIndicatorsDto Closing,
    ClosePreviewNewPeriodDto NewPeriod,
    List<string> Errors);

/// <summary>User-facing messages shared by the CxP service and page.</summary>
public static class CxPMessages
{
    public const string ExchangeRateMustBePositive = "El tipo de cambio debe ser mayor a cero.";
    public const string ValueCannotBeNegative = "El valor no puede ser negativo.";
    public const string PeriodAlreadyClosed = "Este mes ya fue cerrado.";
}

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
    Task ClosePeriodAsync(Guid periodId, decimal newExchangeRate, decimal newEnCuenta);
    Task<ClosePreviewDto> GetClosePreviewAsync(Guid periodId, decimal? newExchangeRate, decimal? newEnCuenta);
}
