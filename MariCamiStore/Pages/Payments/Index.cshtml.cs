using MariCamiStore.Pages.Shared;
using MariCamiStore.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MariCamiStore.Pages.Payments;

public class IndexModel(IPaymentService paymentService, ICatalogService catalogService, ICurrentOrganizationService currentOrg)
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

    public async Task<JsonResult> OnGetBalanceAsync(Guid customerId)
    {
        var balance = await paymentService.GetCustomerBalanceAsync(customerId);
        if (balance == null) return new JsonResult(new { error = "Cliente no encontrado." });
        return new JsonResult(balance);
    }

    public async Task<JsonResult> OnGetSaldosAsync()
    {
        var rows = await paymentService.GetSaldosReportAsync();
        return new JsonResult(rows);
    }
    public async Task<JsonResult> OnPostRegisterPaymentAsync([FromBody] PaymentRequest request)
    {
        if (request.CustomerId == Guid.Empty || request.Amount <= 0)
            return new JsonResult(new { success = false, error = "Cliente y monto son requeridos. El monto debe ser mayor a cero." });

        var result = await paymentService.RegisterPaymentAsync(request.CustomerId, request.Amount);
        return new JsonResult(new { success = true, balance = result });
    }

    public record PaymentRequest(Guid CustomerId, decimal Amount);
}

