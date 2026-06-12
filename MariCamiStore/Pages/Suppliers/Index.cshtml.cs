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
    public async Task<JsonResult> OnPostInsertAsync([FromBody] Supplier item)
    {
        var created = await catalogService.CreateSupplierAsync(item);
        return new JsonResult(created);
    }
    public async Task<JsonResult> OnPostUpdateAsync([FromBody] Supplier item)
    {
        var updated = await catalogService.UpdateSupplierAsync(item);
        return new JsonResult(updated);
    }
    public async Task<JsonResult> OnPostDeleteAsync([FromBody] DeleteRequest request)
    {
        await catalogService.DeleteSupplierAsync(request.Id);
        return new JsonResult(new { success = true });
    }

    public record DeleteRequest(Guid Id);
}

