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
        if (exchangeRate <= 0)
            throw new InvalidOperationException(CxPMessages.ExchangeRateMustBePositive);

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
        if (req.ExchangeRate <= 0)
            throw new InvalidOperationException(CxPMessages.ExchangeRateMustBePositive);
        if (req.PagosRealizados < 0 || req.EnCuenta < 0)
            throw new InvalidOperationException(CxPMessages.ValueCannotBeNegative);

        period.ExchangeRate = req.ExchangeRate;
        period.PagosRealizados = req.PagosRealizados;
        period.EnCuenta = req.EnCuenta;
        await context.SaveChangesAsync();
        return period;
    }

    public async Task ClosePeriodAsync(Guid periodId, decimal newExchangeRate, decimal newEnCuenta)
    {
        if (newExchangeRate <= 0)
            throw new InvalidOperationException(CxPMessages.ExchangeRateMustBePositive);
        if (newEnCuenta < 0)
            throw new InvalidOperationException(CxPMessages.ValueCannotBeNegative);

        // Close exactly the period shown in the preview; never fall back to "the open period".
        // Query (not FindAsync) so the organization query filter applies to the client-supplied id.
        var period = await context.PeriodControls.FirstOrDefaultAsync(p => p.Id == periodId);
        if (period == null || period.IsClosed)
            throw new InvalidOperationException(CxPMessages.PeriodAlreadyClosed);

        // Recompute with current data (never trust values computed in the browser).
        var indicators = await GetPeriodIndicatorsAsync(periodId);
        var (saldoAnterior, _) = CarryOver(indicators.DeudaAPagar, period.EnCuenta);

        period.IsClosed = true;

        var (nextMonth, nextYear) = NextMonth(period);
        var localCurrencyId = (await context.Configurations.FirstOrDefaultAsync())?.LocalCurrencyId ?? Guid.Empty;
        var now = DateTime.UtcNow;

        var newPeriod = new PeriodControl
        {
            Id = Guid.NewGuid(),
            OrganizationId = currentOrg.OrganizationId,
            TransactionMonth = nextMonth,
            TransactionYear = nextYear,
            ExchangeRate = newExchangeRate,
            PagosRealizados = 0,
            EnCuenta = newEnCuenta,
            IsClosed = false,
            CreatedAt = now
        };
        context.PeriodControls.Add(newPeriod);

        if (saldoAnterior > 0)
        {
            context.CxPEntries.Add(new CxPEntry
            {
                Id = Guid.NewGuid(),
                PeriodControlId = newPeriod.Id,
                CurrencyId = localCurrencyId,
                Amount = saldoAnterior,
                Reference = "Saldo anterior",
                Type = CxPEntryType.SaldoAnterior,
                OrderId = null,
                CreatedAt = now
            });
        }

        // Single SaveChanges = atomic close. A concurrent close violates the "one open period per
        // organization" / (org, month, year) unique indexes and is reported as already closed.
        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            logger.LogWarning(ex, "Concurrent close detected for period {PeriodId}.", periodId);
            throw new InvalidOperationException(CxPMessages.PeriodAlreadyClosed);
        }
    }

    public async Task<ClosePreviewDto> GetClosePreviewAsync(Guid periodId, decimal? newExchangeRate, decimal? newEnCuenta)
    {
        var period = await context.PeriodControls.AsNoTracking().FirstOrDefaultAsync(p => p.Id == periodId);
        if (period == null || period.IsClosed)
            throw new InvalidOperationException(CxPMessages.PeriodAlreadyClosed);

        var closing = await GetPeriodIndicatorsAsync(periodId);
        var (saldoAnterior, proposedEnCuenta) = CarryOver(closing.DeudaAPagar, period.EnCuenta);

        var config = await context.Configurations.FirstOrDefaultAsync();
        var localCurrencyId = config?.LocalCurrencyId ?? Guid.Empty;
        var proposedExchangeRate = config != null && config.ExchangeRate > 0 ? config.ExchangeRate : period.ExchangeRate;

        var exchangeRate = newExchangeRate ?? proposedExchangeRate;
        var enCuenta = newEnCuenta ?? proposedEnCuenta;

        var errors = new List<string>();
        if (exchangeRate <= 0) errors.Add(CxPMessages.ExchangeRateMustBePositive);
        if (enCuenta < 0) errors.Add(CxPMessages.ValueCannotBeNegative);

        // The new month starts with the Saldo anterior (local currency) as its only entry.
        var porPagarPorMoneda = new Dictionary<string, CxPCurrencyBalance>();
        if (saldoAnterior > 0)
        {
            var local = await context.Currencies.FirstOrDefaultAsync(c => c.Id == localCurrencyId);
            porPagarPorMoneda[localCurrencyId.ToString()] = new CxPCurrencyBalance(
                local?.Name ?? localCurrencyId.ToString(), local?.Sign ?? string.Empty, saldoAnterior);
        }

        var effectiveRate = exchangeRate > 0 ? exchangeRate : 0m;
        var shipping = await ComputeShippingPendingAsync(effectiveRate, localCurrencyId);
        var (deuda, pendiente, posicion) = ComputeDerived(saldoAnterior, 0m, enCuenta, closing.SaldosPorCobrar, shipping);
        var (nextMonth, nextYear) = NextMonth(period);

        var newIndicators = new CxPPeriodIndicatorsDto(
            PeriodId: Guid.Empty,
            TransactionMonth: nextMonth,
            TransactionYear: nextYear,
            ExchangeRate: exchangeRate,
            PorPagarPorMoneda: porPagarPorMoneda,
            PorPagarEnColones: saldoAnterior,
            SaldosPorCobrar: closing.SaldosPorCobrar,
            PagosRealizados: 0m,
            DeudaAPagar: deuda,
            EnCuenta: enCuenta,
            PendienteDeRecoger: pendiente,
            ShippingCRPendientesDeAplicar: shipping,
            Posicion: posicion,
            IsClosed: false,
            ExchangeRateWarning: effectiveRate == 0);

        return new ClosePreviewDto(
            Closing: closing,
            NewPeriod: new ClosePreviewNewPeriodDto(
                Month: nextMonth,
                Year: nextYear,
                ExchangeRate: exchangeRate,
                ProposedExchangeRate: proposedExchangeRate,
                PagosRealizados: 0m,
                EnCuenta: enCuenta,
                ProposedEnCuenta: proposedEnCuenta,
                SaldoAnterior: saldoAnterior > 0 ? saldoAnterior : null,
                Indicators: newIndicators),
            Errors: errors);
    }

    /// <summary>
    /// Month-close carry-over: the debt is paid from En Cuenta. What is not covered becomes the Saldo anterior;
    /// what is left stays En Cuenta. A negative debt (should not happen) is treated as 0.
    /// </summary>
    internal static (decimal SaldoAnterior, decimal ProposedEnCuenta) CarryOver(decimal deuda, decimal enCuenta)
    {
        var debt = Math.Max(0m, deuda);
        return (Math.Max(0m, debt - enCuenta), Math.Max(0m, enCuenta - debt));
    }

    private static (int Month, int Year) NextMonth(PeriodControl period) =>
        period.TransactionMonth == 12
            ? (1, period.TransactionYear + 1)
            : (period.TransactionMonth + 1, period.TransactionYear);

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

        decimal porPagarEnColones = 0;
        if (!exchangeRateWarning)
        {
            foreach (var kvp in porPagarPorMoneda)
            {
                if (Guid.TryParse(kvp.Key, out var cid) && cid == localCurrencyId)
                    porPagarEnColones += kvp.Value.Amount;
                else
                    porPagarEnColones += kvp.Value.Amount * period.ExchangeRate;
            }
        }

        var shippingCRPendientesDeAplicar = await ComputeShippingPendingAsync(period.ExchangeRate, localCurrencyId);

        var saldosRows = await paymentService.GetSaldosReportAsync(null);
        var saldosPorCobrar = saldosRows.Sum(r => r.Balance);

        var (deudaAPagar, pendienteDeRecoger, posicion) = ComputeDerived(
            porPagarEnColones, period.PagosRealizados, period.EnCuenta, saldosPorCobrar, shippingCRPendientesDeAplicar);

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

    /// <summary>Pending shipping per Active/Delivering order = max(0, estimated shipping - registered packages), in colones; 0 when the exchange rate is 0.</summary>
    private async Task<decimal> ComputeShippingPendingAsync(decimal exchangeRate, Guid localCurrencyId)
    {
        if (exchangeRate == 0) return 0m;

        var shippingRows = await context.Orders
            .Where(o => o.Status == OrderStatus.Active.Key || o.Status == OrderStatus.Delivering.Key)
            .Select(o => new
            {
                o.CurrencyId,
                o.ShippingAmountToCR,
                Packages = context.OrderPackages.Where(p => p.OrderId == o.Id).Sum(p => (decimal?)p.Amount) ?? 0m
            })
            .ToListAsync();
        return shippingRows.Sum(r =>
        {
            var pending = Math.Max(0m, r.ShippingAmountToCR - r.Packages);
            return r.CurrencyId == localCurrencyId ? pending : pending * exchangeRate;
        });
    }

    /// <summary>Indicator formulas shared by the panel and the close preview (unchanged since 009).</summary>
    private static (decimal Deuda, decimal Pendiente, decimal Posicion) ComputeDerived(
        decimal porPagarEnColones, decimal pagos, decimal enCuenta, decimal saldosPorCobrar, decimal shipping)
    {
        var deuda = porPagarEnColones - pagos;
        var pendiente = deuda - enCuenta;
        var posicion = saldosPorCobrar + enCuenta - deuda - shipping;
        return (deuda, pendiente, posicion);
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
            Type = CxPEntryType.Manual,
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
