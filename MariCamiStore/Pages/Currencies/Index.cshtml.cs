using MariCamiStore.Model;
using MariCamiStore.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MariCamiStore.Pages.Currencies;

public class IndexModel(ICatalogService catalogService) : PageModel
{
    public IActionResult OnGet() => Page();

    public async Task<JsonResult> OnGetLoadAsync() =>
        new JsonResult(await catalogService.GetCurrenciesAsync());
    public async Task<JsonResult> OnPostInsertAsync([FromBody] Currency item)
    {
        var created = await catalogService.CreateCurrencyAsync(item);
        return new JsonResult(created);
    }
    public async Task<JsonResult> OnPostUpdateAsync([FromBody] Currency item)
    {
        var updated = await catalogService.UpdateCurrencyAsync(item);
        return new JsonResult(updated);
    }
    public async Task<JsonResult> OnPostDeleteAsync([FromBody] DeleteRequest request)
    {
        await catalogService.DeleteCurrencyAsync(request.Id);
        return new JsonResult(new { success = true });
    }

    public record DeleteRequest(Guid Id);
}
