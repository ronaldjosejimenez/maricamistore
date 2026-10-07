using MariCamiStore.Model;
using MariCamiStore.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MariCamiStore.Pages.Salespeople;

public class IndexModel(ICatalogService catalogService) : PageModel
{
    public IActionResult OnGet() => Page();

    public async Task<JsonResult> OnGetLoadAsync() =>
        new JsonResult(await catalogService.GetSalespeopleAsync());

    public async Task<IActionResult> OnPostInsertAsync([FromBody] Salesperson item)
    {
        if (string.IsNullOrWhiteSpace(item.Name))
            return BadRequest(new { success = false, error = "El nombre es requerido." });
        var created = await catalogService.CreateSalespersonAsync(item);
        return new JsonResult(created);
    }

    public async Task<IActionResult> OnPostUpdateAsync([FromBody] Salesperson item)
    {
        if (string.IsNullOrWhiteSpace(item.Name))
            return BadRequest(new { success = false, error = "El nombre es requerido." });
        var updated = await catalogService.UpdateSalespersonAsync(item);
        return new JsonResult(updated);
    }
}
