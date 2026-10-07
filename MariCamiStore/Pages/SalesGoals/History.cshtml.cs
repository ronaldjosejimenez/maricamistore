using MariCamiStore.Pages.Shared;
using MariCamiStore.Services;
using Microsoft.AspNetCore.Mvc;

namespace MariCamiStore.Pages.SalesGoals;

public class HistoryModel(
    ISalesGoalService salesGoalService,
    ICatalogService catalogService,
    ICurrentOrganizationService currentOrg) : OrganizationPageModel(currentOrg)
{
    public IActionResult OnGet() => CheckOrganization() ?? Page();

    /// <summary>All salespeople, including inactive ones.</summary>
    public async Task<JsonResult> OnGetSalespeopleAsync()
    {
        var list = await catalogService.GetSalespeopleAsync();
        return new JsonResult(list.Select(s => new
        {
            id = s.Id,
            text = (string.IsNullOrWhiteSpace(s.NickName) ? s.Name : $"{s.Name} ({s.NickName})")
                   + (s.IsActive ? string.Empty : " [inactivo]")
        }));
    }

    public async Task<JsonResult> OnGetListAsync(Guid salespersonId) =>
        new JsonResult(await salesGoalService.GetHistoryAsync(salespersonId));

    public async Task<JsonResult> OnGetDetailAsync(Guid goalId)
    {
        try
        {
            return new JsonResult(await salesGoalService.GetDetailAsync(goalId));
        }
        catch (SalesGoalException ex)
        {
            return new JsonResult(new { success = false, error = ex.Message });
        }
    }
}
