using MariCamiStore.Pages.Shared;
using MariCamiStore.Services;
using Microsoft.AspNetCore.Mvc;

namespace MariCamiStore.Pages.CxP;

public class IndexModel(
    ICxPService cxpService,
    ICatalogService catalogService,
    ICurrentOrganizationService currentOrg)
    : OrganizationPageModel(currentOrg)
{
    public async Task<IActionResult> OnGetAsync()
    {
        var guard = CheckOrganization();
        if (guard != null) return guard;

        var config = await catalogService.GetConfigurationAsync();
        var localCurrency = config != null ? await catalogService.GetCurrencyByIdAsync(config.LocalCurrencyId) : null;
        ViewData["LocalCurrencySign"] = localCurrency?.Sign ?? string.Empty;
        ViewData["InitExchangeRate"] = config?.ExchangeRate ?? 0m;

        var currencies = await catalogService.GetCurrenciesAsync();
        ViewData["Currencies"] = currencies;

        return Page();
    }

    public async Task<JsonResult> OnGetPeriodAsync()
    {
        var period = await cxpService.GetOpenPeriodAsync();
        if (period == null)
            return new JsonResult(new { noPeriod = true });

        try
        {
            var indicators = await cxpService.GetPeriodIndicatorsAsync(period.Id);
            return new JsonResult(indicators);
        }
        catch (Exception ex)
        {
            return new JsonResult(new { error = ex.Message });
        }
    }

    public async Task<JsonResult> OnGetEntriesAsync()
    {
        var period = await cxpService.GetOpenPeriodAsync();
        if (period == null)
            return new JsonResult(new List<object>());

        var entries = await cxpService.GetEntriesByPeriodAsync(period.Id);

        var grouped = entries
            .GroupBy(e => new { e.CurrencyId, e.CurrencyName, e.Sign })
            .Select(g => new
            {
                currencyId = g.Key.CurrencyId,
                currencyName = g.Key.CurrencyName,
                sign = g.Key.Sign,
                entries = g.Select(e => new
                {
                    e.Id,
                    e.Reference,
                    e.Type,
                    e.Amount,
                    e.CreatedAt
                }),
                total = g.Sum(e => e.Amount)
            });

        return new JsonResult(grouped);
    }

    public async Task<JsonResult> OnPostInitPeriodAsync([FromBody] InitPeriodRequest req)
    {
        if (req.Month < 1 || req.Month > 12)
            return new JsonResult(new { success = false, error = "El mes debe estar entre 1 y 12." });
        if (req.Year < 2020)
            return new JsonResult(new { success = false, error = "El año debe ser mayor o igual a 2020." });
        if (req.ExchangeRate <= 0)
            return new JsonResult(new { success = false, error = "El tipo de cambio debe ser mayor a cero." });

        try
        {
            await cxpService.InitializePeriodAsync(req.Month, req.Year, req.ExchangeRate);
            return new JsonResult(new { success = true });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, error = ex.Message });
        }
    }

    public async Task<JsonResult> OnPostAddEntryAsync([FromBody] CreateManualCxPEntryRequest req)
    {
        var period = await cxpService.GetOpenPeriodAsync();
        if (period == null)
            return new JsonResult(new { success = false, error = "No hay un período abierto." });

        try
        {
            await cxpService.CreateManualEntryAsync(period.Id, req);
            return new JsonResult(new { success = true });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, error = ex.Message });
        }
    }

    public async Task<JsonResult> OnPostDeleteEntryAsync([FromBody] DeleteEntryRequest req)
    {
        try
        {
            await cxpService.DeleteEntryAsync(req.EntryId);
            return new JsonResult(new { success = true });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, error = ex.Message });
        }
    }

    public async Task<JsonResult> OnPostUpdatePeriodAsync([FromBody] UpdatePeriodFieldsRequest req)
    {
        var period = await cxpService.GetOpenPeriodAsync();
        if (period == null)
            return new JsonResult(new { success = false, error = "No hay un período abierto." });

        if (req.ExchangeRate < 0 || req.PagosRealizados < 0 || req.EnCuenta < 0)
            return new JsonResult(new { success = false, error = "Los valores no pueden ser negativos." });

        try
        {
            await cxpService.UpdatePeriodFieldsAsync(period.Id, req);
            return new JsonResult(new { success = true });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, error = ex.Message });
        }
    }

    public async Task<JsonResult> OnPostClosePeriodAsync()
    {
        var period = await cxpService.GetOpenPeriodAsync();
        if (period == null)
            return new JsonResult(new { success = false, error = "No hay un período abierto." });

        try
        {
            await cxpService.ClosePeriodAsync(period.Id);
            return new JsonResult(new { success = true });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, error = ex.Message });
        }
    }
}
