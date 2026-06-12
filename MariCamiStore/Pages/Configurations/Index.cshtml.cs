using MariCamiStore.Model;
using MariCamiStore.Pages.Shared;
using MariCamiStore.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MariCamiStore.Pages.Configurations;

public class IndexModel(ICatalogService catalogService, ICurrentOrganizationService currentOrg)
    : OrganizationPageModel(currentOrg)
{
    public IActionResult OnGet() => CheckOrganization() ?? Page();

    public async Task<JsonResult> OnGetLoadAsync()
    {
        var config = await catalogService.GetConfigurationAsync();
        var list = config == null ? Array.Empty<Configuration>() : new[] { config };
        return new JsonResult(list);
    }
    public async Task<JsonResult> OnPostUpsertAsync([FromBody] Configuration item)
    {
        var result = await catalogService.UpsertConfigurationAsync(item);
        return new JsonResult(result);
    }
}

