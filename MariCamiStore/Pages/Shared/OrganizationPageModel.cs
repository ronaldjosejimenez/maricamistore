using MariCamiStore.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MariCamiStore.Pages.Shared;

public abstract class OrganizationPageModel(ICurrentOrganizationService currentOrg) : PageModel
{
    protected ICurrentOrganizationService CurrentOrg { get; } = currentOrg;

    protected IActionResult? CheckOrganization()
    {
        if (CurrentOrg.OrganizationId == Guid.Empty)
            return RedirectToPage("/Organizations/Index",
                new { error = "Debe seleccionar una organización antes de continuar." });
        return null;
    }
}
