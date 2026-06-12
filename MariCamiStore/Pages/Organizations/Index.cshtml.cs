using MariCamiStore.Model;
using MariCamiStore.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MariCamiStore.Pages.Organizations
{
    public class IndexModel(IOrganizationService organizationService) : PageModel
    {
        private const string SessionKey = "ActiveOrganizationId";

        public void OnGet() { }

        public async Task<JsonResult> OnGetLoadAsync()
        {
            var orgs = await organizationService.GetOrganizationsAsync();
            return new JsonResult(orgs.Select(o => new { o.Id, o.Name }));
        }

        public JsonResult OnPostSetActiveAsync([FromBody] SetActiveRequest request)
        {
            HttpContext.Session.SetString(SessionKey, request.OrganizationId.ToString());
            return new JsonResult(new { success = true });
        }

        public async Task<JsonResult> OnPostInsertAsync([FromBody] Organization item)
        {
            var created = await organizationService.CreateOrganizationAsync(item);
            return new JsonResult(new { created.Id, created.Name });
        }

        public async Task<JsonResult> OnPostUpdateAsync([FromBody] Organization item)
        {
            var updated = await organizationService.UpdateOrganizationAsync(item);
            return new JsonResult(new { updated.Id, updated.Name });
        }

        public async Task<JsonResult> OnPostDeleteAsync([FromBody] DeleteRequest request)
        {
            var (success, error) = await organizationService.DeleteOrganizationAsync(request.Id);
            return new JsonResult(new { success, error });
        }

        public record SetActiveRequest(Guid OrganizationId);
        public record DeleteRequest(Guid Id);
    }
}
