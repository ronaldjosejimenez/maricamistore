using MariCamiStore.Model;
using MariCamiStore.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MariCamiStore.Pages.Customers;

public class IndexModel(ICatalogService catalogService) : PageModel
{
    public IActionResult OnGet() => Page();

    public async Task<JsonResult> OnGetLoadAsync() =>
        new JsonResult(await catalogService.GetCustomersAsync());

    public async Task<JsonResult> OnGetLoadPayableAsync() =>
        new JsonResult(await catalogService.GetPayableCustomersAsync());
    public async Task<JsonResult> OnPostInsertAsync([FromBody] Customer item)
    {
        if (string.IsNullOrWhiteSpace(item.NickName))
            return new JsonResult(new { error = "El apodo es requerido." });
        var created = await catalogService.CreateCustomerAsync(item);
        return new JsonResult(created);
    }
    public async Task<JsonResult> OnPostUpdateAsync([FromBody] Customer item)
    {
        if (string.IsNullOrWhiteSpace(item.NickName))
            return new JsonResult(new { error = "El apodo es requerido." });
        var updated = await catalogService.UpdateCustomerAsync(item);
        return new JsonResult(updated);
    }
    public async Task<JsonResult> OnPostDeleteAsync([FromBody] DeleteRequest request)
    {
        await catalogService.DeleteCustomerAsync(request.Id);
        return new JsonResult(new { success = true });
    }

    public record DeleteRequest(Guid Id);
}

