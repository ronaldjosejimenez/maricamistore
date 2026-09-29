using MariCamiStore.Pages.Shared;
using MariCamiStore.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MariCamiStore.Pages.Payments;

public class IndexModel(IPaymentService paymentService, ICatalogService catalogService, IOrganizationService organizationService, ICurrentOrganizationService currentOrg)
    : OrganizationPageModel(currentOrg)
{
    public async Task<IActionResult> OnGetAsync()
    {
        var guard = CheckOrganization();
        if (guard != null) return guard;
        var config = await catalogService.GetConfigurationAsync();
        var localCurrency = config != null ? await catalogService.GetCurrencyByIdAsync(config.LocalCurrencyId) : null;
        ViewData["LocalCurrencySign"] = localCurrency?.Sign ?? string.Empty;
        ViewData["Organizations"] = await organizationService.GetOrganizationsAsync();
        ViewData["CurrentOrganizationId"] = CurrentOrg.OrganizationId;
        return Page();
    }

    public async Task<JsonResult> OnGetBalanceAsync(Guid customerId, Guid? organizationId)
    {
        var balance = await paymentService.GetCustomerBalanceAsync(customerId, organizationId);
        if (balance == null) return new JsonResult(new { error = "Cliente no encontrado." });
        return new JsonResult(balance);
    }

    public async Task<JsonResult> OnGetSaldosAsync(Guid? organizationId)
    {
        var rows = await paymentService.GetSaldosReportAsync(organizationId);
        return new JsonResult(rows);
    }
    public async Task<JsonResult> OnPostRegisterPaymentAsync([FromBody] PaymentRequest request)
    {
        if (request.CustomerId == Guid.Empty || request.Amount <= 0)
            return new JsonResult(new { success = false, error = "Cliente y monto son requeridos. El monto debe ser mayor a cero." });

        if (request.OrganizationId == null)
            return new JsonResult(new { success = false, error = "Seleccione una organización específica para registrar el pago." });

        var (success, error, balance) = await paymentService.RegisterPaymentAsync(request.CustomerId, request.Amount, request.OrganizationId);
        return new JsonResult(new { success, error, balance });
    }

    public record PaymentRequest(Guid CustomerId, decimal Amount, Guid? OrganizationId);
}

