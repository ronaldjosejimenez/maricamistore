using MariCamiStore.Model;
using MariCamiStore.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MariCamiStore.Pages.Suppliers;

public class IndexModel(ICatalogService catalogService) : PageModel
{
    public IActionResult OnGet() => Page();

    public async Task<JsonResult> OnGetLoadAsync() =>
        new JsonResult(await catalogService.GetSuppliersAsync());

    // Defaults for the insert row (spec 018, FR-002)
    public async Task<JsonResult> OnGetConfigurationAsync()
    {
        var config = await catalogService.GetConfigurationAsync();
        return new JsonResult(config == null
            ? null
            : new { config.TaxPercentage, config.ExchangeRate, CurrencyId = config.OrderCurrencyIdDefault });
    }

    public async Task<IActionResult> OnPostInsertAsync([FromBody] Supplier item)
    {
        try
        {
            var created = await catalogService.CreateSupplierAsync(item);
            return new JsonResult(created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
    public async Task<IActionResult> OnPostUpdateAsync([FromBody] Supplier item)
    {
        try
        {
            var updated = await catalogService.UpdateSupplierAsync(item);
            return new JsonResult(updated);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
    public async Task<JsonResult> OnPostDeleteAsync([FromBody] DeleteRequest request)
    {
        await catalogService.DeleteSupplierAsync(request.Id);
        return new JsonResult(new { success = true });
    }

    public record DeleteRequest(Guid Id);
}
