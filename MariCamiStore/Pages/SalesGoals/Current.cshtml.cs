using MariCamiStore.Pages.Shared;
using MariCamiStore.Services;
using Microsoft.AspNetCore.Mvc;

namespace MariCamiStore.Pages.SalesGoals;

public class CurrentModel(
    ISalesGoalService salesGoalService,
    ICatalogService catalogService,
    ICurrentOrganizationService currentOrg) : OrganizationPageModel(currentOrg)
{
    public IActionResult OnGet() => CheckOrganization() ?? Page();

    public async Task<JsonResult> OnGetSalespeopleAsync()
    {
        var list = await catalogService.GetActiveSalespeopleAsync();
        return new JsonResult(list.Select(s => new
        {
            id = s.Id,
            text = string.IsNullOrWhiteSpace(s.NickName) ? s.Name : $"{s.Name} ({s.NickName})"
        }));
    }

    public Task<JsonResult> OnGetGoalAsync(Guid salespersonId) =>
        Run(() => salesGoalService.GetOrCreateCurrentAsync(salespersonId));

    public Task<JsonResult> OnPostUpdateHeaderAsync([FromBody] UpdateHeaderRequest request) =>
        Run(() => salesGoalService.UpdateHeaderAsync(request));

    public Task<JsonResult> OnPostUpdateDayAsync([FromBody] UpdateDayRequest request) =>
        Run(() => salesGoalService.UpdateDayAsync(request));

    private static async Task<JsonResult> Run(Func<Task<GoalDto>> action)
    {
        try
        {
            return new JsonResult(await action());
        }
        catch (SalesGoalException ex)
        {
            return new JsonResult(new { success = false, error = ex.Message });
        }
    }
}
