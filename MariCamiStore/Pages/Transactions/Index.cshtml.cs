using MariCamiStore.Pages.Shared;
using MariCamiStore.Services;
using Microsoft.AspNetCore.Mvc;

namespace MariCamiStore.Pages.Transactions;

public class IndexModel(ITransactionService txService, ICatalogService catalogService, ICurrentOrganizationService currentOrg)
    : OrganizationPageModel(currentOrg)
{
    public async Task<IActionResult> OnGetAsync()
    {
        var guard = CheckOrganization();
        if (guard != null) return guard;
        var config = await catalogService.GetConfigurationAsync();
        var localCurrency = config != null ? await catalogService.GetCurrencyByIdAsync(config.LocalCurrencyId) : null;
        ViewData["LocalCurrencySign"] = localCurrency?.Sign ?? string.Empty;
        return Page();
    }

    public async Task<JsonResult> OnGetLoadAsync(
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] Guid? customerId,
        [FromQuery] string? transactionType)
    {
        var filter = new TransactionFilterDto(dateFrom, dateTo, customerId, transactionType);
        var rows = await txService.GetTransactionsAsync(filter);
        return new JsonResult(rows);
    }

    public async Task<JsonResult> OnPostCreateManualAsync([FromBody] ManualTransactionRequest request)
    {
        if (request.CustomerId == Guid.Empty || request.Amount <= 0 || string.IsNullOrEmpty(request.TransactionType))
            return new JsonResult(new { success = false, error = "Cliente, tipo y monto son requeridos." });

        try
        {
            await txService.CreateManualTransactionAsync(request);
            return new JsonResult(new { success = true });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, error = ex.Message });
        }
    }
}
