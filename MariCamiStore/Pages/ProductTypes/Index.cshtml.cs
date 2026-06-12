using MariCamiStore.Model;
using MariCamiStore.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MariCamiStore.Pages.ProductTypes;

public class IndexModel(ICatalogService catalogService) : PageModel
{
    public async Task<IActionResult> OnGetAsync()
    {
        var config = await catalogService.GetConfigurationAsync();
        var localCurrency = config != null ? await catalogService.GetCurrencyByIdAsync(config.LocalCurrencyId) : null;
        ViewData["LocalCurrencySign"] = localCurrency?.Sign ?? string.Empty;
        return Page();
    }

    public async Task<JsonResult> OnGetLoadAsync() =>
        new JsonResult(await catalogService.GetProductTypesAsync());
    public async Task<JsonResult> OnPostInsertAsync([FromBody] ProductType item)
    {
        var created = await catalogService.CreateProductTypeAsync(item);
        return new JsonResult(created);
    }
    public async Task<JsonResult> OnPostUpdateAsync([FromBody] ProductType item)
    {
        var updated = await catalogService.UpdateProductTypeAsync(item);
        return new JsonResult(updated);
    }
    public async Task<JsonResult> OnPostDeleteAsync([FromBody] DeleteRequest request)
    {
        await catalogService.DeleteProductTypeAsync(request.Id);
        return new JsonResult(new { success = true });
    }

    public record DeleteRequest(Guid Id);
}

