using MariCamiStore.Infrastructure.Persistance;
using MariCamiStore.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MariCamiStore.Services;

public class CxPService(
    MariCamiStoreContext context,
    ICurrentOrganizationService currentOrg,
    IPaymentService paymentService,
    ILogger<CxPService> logger) : ICxPService
{
    // ── Period ────────────────────────────────────────────────────────────────

    public Task<PeriodControl?> GetOpenPeriodAsync() =>
        context.PeriodControls.FirstOrDefaultAsync(p => !p.IsClosed);

    public async Task<PeriodControl> InitializePeriodAsync(int month, int year, decimal exchangeRate)
    {
        var existing = await GetOpenPeriodAsync();
        if (existing != null)
            throw new InvalidOperationException("Ya existe un período abierto.");

        var period = new PeriodControl
        {
            Id = Guid.NewGuid(),
            OrganizationId = currentOrg.OrganizationId,
            TransactionMonth = month,
            TransactionYear = year,
            ExchangeRate = exchangeRate,
            PagosRealizados = 0,
            EnCuenta = 0,
            IsClosed = false,
            CreatedAt = DateTime.UtcNow
        };
        context.PeriodControls.Add(period);
        await context.SaveChangesAsync();
        return period;
    }

    public async Task<PeriodControl> UpdatePeriodFieldsAsync(Guid periodId, UpdatePeriodFieldsRequest req)
    {
        var period = await context.PeriodControls.FindAsync(periodId)
            ?? throw new InvalidOperationException("Período no encontrado.");
        if (period.IsClosed)
            throw new InvalidOperationException("El período está cerrado.");

        period.ExchangeRate = req.ExchangeRate;
        period.PagosRealizados = req.PagosRealizados;
        period.EnCuenta = req.EnCuenta;
        await context.SaveChangesAsync();
        return period;
    }

    public async Task ClosePeriodAsync(Guid periodId)
    {
        var period = await context.PeriodControls.FindAsync(periodId)
            ?? throw new InvalidOperationException("Período no encontrado.");
        if (period.IsClosed)
            throw new InvalidOperationException("El período ya está cerrado.");

        var indicators = await GetPeriodIndicatorsAsync(periodId);
        var deudaAPagar = indicators.DeudaAPagar;

        period.IsClosed = true;

        var nextMonth = period.TransactionMonth == 12 ? 1 : period.TransactionMonth + 1;
        var nextYear = period.TransactionMonth == 12 ? period.TransactionYear + 1 : period.TransactionYear;

        var config = await context.Configurations.FirstOrDefaultAsync();
        var newExchangeRate = config?.ExchangeRate ?? period.ExchangeRate;
        var localCurrencyId = config?.LocalCurrencyId ?? Guid.Empty;

        var newPeriod = new PeriodControl
        {
            Id = Guid.NewGuid(),
            OrganizationId = currentOrg.OrganizationId,
            TransactionMonth = nextMonth,
            TransactionYear = nextYear,
            ExchangeRate = newExchangeRate,
            PagosRealizados = 0,
            EnCuenta = 0,
            IsClosed = false,
            CreatedAt = DateTime.UtcNow
        };
        context.PeriodControls.Add(newPeriod);

        var saldoEntry = new CxPEntry
        {
            Id = Guid.NewGuid(),
            PeriodControlId = newPeriod.Id,
            CurrencyId = localCurrencyId,
            Amount = deudaAPagar,
            Reference = "Saldo anterior",
            Type = "SaldoAnterior",
            OrderId = null,
            CreatedAt = DateTime.UtcNow
        };
        context.CxPEntries.Add(saldoEntry);

        await context.SaveChangesAsync();
    }

    // ── Indicators ────────────────────────────────────────────────────────────

    public async Task<CxPPeriodIndicatorsDto> GetPeriodIndicatorsAsync(Guid periodId)
    {
        var period = await context.PeriodControls.FindAsync(periodId)
            ?? throw new InvalidOperationException("Período no encontrado.");

        var entries = await context.CxPEntries
            .Where(e => e.PeriodControlId == periodId)
            .ToListAsync();

        var currencyIds = entries.Select(e => e.CurrencyId).Distinct().ToList();
        var currencies = await context.Currencies
            .Where(c => currencyIds.Contains(c.Id))
            .ToListAsync();
        var currencyMap = currencies.ToDictionary(c => c.Id);

        var config = await context.Configurations.FirstOrDefaultAsync();
        var localCurrencyId = config?.LocalCurrencyId ?? Guid.Empty;

        bool exchangeRateWarning = period.ExchangeRate == 0;

        var porPagarPorMoneda = new Dictionary<string, CxPCurrencyBalance>();
        foreach (var group in entries.GroupBy(e => e.CurrencyId))
        {
            currencyMap.TryGetValue(group.Key, out var currency);
            var total = group.Sum(e => e.Amount);
            porPagarPorMoneda[group.Key.ToString()] = new CxPCurrencyBalance(
                currency?.Name ?? group.Key.ToString(),
                currency?.Sign ?? string.Empty,
                total);
        }

        decimal porPagarEnColones;
        decimal shippingCRPendientesDeAplicar;

        if (exchangeRateWarning)
        {
            porPagarEnColones = 0;
            shippingCRPendientesDeAplicar = 0;
        }
        else
        {
            porPagarEnColones = 0;
            foreach (var kvp in porPagarPorMoneda)
            {
                if (Guid.TryParse(kvp.Key, out var cid) && cid == localCurrencyId)
                    porPagarEnColones += kvp.Value.Amount;
                else
                    porPagarEnColones += kvp.Value.Amount * period.ExchangeRate;
            }

            var activeOrders = await context.Orders
                .Where(o => o.Status == OrderStatus.Active.Key)
                .Select(o => new { o.ShippingAmountToCR, o.CurrencyId })
                .ToListAsync();
            shippingCRPendientesDeAplicar = activeOrders.Sum(o =>
                o.CurrencyId == localCurrencyId
                    ? o.ShippingAmountToCR
                    : o.ShippingAmountToCR * period.ExchangeRate);
        }

        var saldosRows = await paymentService.GetSaldosReportAsync();
        var saldosPorCobrar = saldosRows.Sum(r => r.Balance);

        var deudaAPagar = porPagarEnColones - period.PagosRealizados;
        var pendienteDeRecoger = deudaAPagar - period.EnCuenta;
        var posicion = saldosPorCobrar + period.EnCuenta - deudaAPagar - shippingCRPendientesDeAplicar;

        return new CxPPeriodIndicatorsDto(
            PeriodId: period.Id,
            TransactionMonth: period.TransactionMonth,
            TransactionYear: period.TransactionYear,
            ExchangeRate: period.ExchangeRate,
            PorPagarPorMoneda: porPagarPorMoneda,
            PorPagarEnColones: porPagarEnColones,
            SaldosPorCobrar: saldosPorCobrar,
            PagosRealizados: period.PagosRealizados,
            DeudaAPagar: deudaAPagar,
            EnCuenta: period.EnCuenta,
            PendienteDeRecoger: pendienteDeRecoger,
            ShippingCRPendientesDeAplicar: shippingCRPendientesDeAplicar,
            Posicion: posicion,
            IsClosed: period.IsClosed,
            ExchangeRateWarning: exchangeRateWarning);
    }

    // ── Entries ───────────────────────────────────────────────────────────────

    public async Task<List<CxPEntryDto>> GetEntriesByPeriodAsync(Guid periodId)
    {
        var entries = await context.CxPEntries
            .Where(e => e.PeriodControlId == periodId)
            .OrderBy(e => e.CreatedAt)
            .ToListAsync();

        var currencyIds = entries.Select(e => e.CurrencyId).Distinct().ToList();
        var currencies = await context.Currencies
            .Where(c => currencyIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id);

        return entries.Select(e =>
        {
            currencies.TryGetValue(e.CurrencyId, out var currency);
            return new CxPEntryDto(
                Id: e.Id,
                CurrencyId: e.CurrencyId,
                CurrencyName: currency?.Name ?? e.CurrencyId.ToString(),
                Sign: currency?.Sign ?? string.Empty,
                Amount: e.Amount,
                Reference: e.Reference,
                Type: e.Type,
                OrderId: e.OrderId,
                CreatedAt: e.CreatedAt);
        }).ToList();
    }

    public async Task<CxPEntry> CreateManualEntryAsync(Guid periodId, CreateManualCxPEntryRequest req)
    {
        var period = await context.PeriodControls.FindAsync(periodId)
            ?? throw new InvalidOperationException("Período no encontrado.");
        if (period.IsClosed)
            throw new InvalidOperationException("El período está cerrado.");

        var currency = await context.Currencies.FindAsync(req.CurrencyId)
            ?? throw new InvalidOperationException("Moneda no encontrada.");

        if (req.Amount <= 0)
            throw new ArgumentException("El monto debe ser mayor a cero.");
        if (string.IsNullOrWhiteSpace(req.Reference))
            throw new ArgumentException("La referencia es requerida.");

        var entry = new CxPEntry
        {
            Id = Guid.NewGuid(),
            PeriodControlId = periodId,
            CurrencyId = req.CurrencyId,
            Amount = req.Amount,
            Reference = req.Reference.Trim(),
            Type = "Manual",
            OrderId = null,
            CreatedAt = DateTime.UtcNow
        };
        context.CxPEntries.Add(entry);
        await context.SaveChangesAsync();
        return entry;
    }

    public async Task DeleteEntryAsync(Guid entryId)
    {
        var entry = await context.CxPEntries
            .Include(e => e.Period)
            .FirstOrDefaultAsync(e => e.Id == entryId)
            ?? throw new InvalidOperationException("Entrada no encontrada.");

        if (entry.Period!.IsClosed)
            throw new InvalidOperationException("No se puede eliminar una entrada de un período cerrado.");

        context.CxPEntries.Remove(entry);
        await context.SaveChangesAsync();
    }

    public async Task<CxPEntry> CreateAutoEntryAsync(Guid periodId, Guid orderId, Guid currencyId, decimal amount, string reference, string type)
    {
        var entry = new CxPEntry
        {
            Id = Guid.NewGuid(),
            PeriodControlId = periodId,
            CurrencyId = currencyId,
            Amount = amount,
            Reference = reference,
            Type = type,
            OrderId = orderId,
            CreatedAt = DateTime.UtcNow
        };
        context.CxPEntries.Add(entry);
        await context.SaveChangesAsync();
        return entry;
    }
}
