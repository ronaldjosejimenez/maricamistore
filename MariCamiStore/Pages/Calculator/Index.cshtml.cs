using MariCamiStore.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MariCamiStore.Pages.Calculator
{
    public class IndexModel(ICatalogService catalogService) : PageModel
    {
        public void OnGet()
        {
        }

        // Only suppliers flagged "Activo para la calculadora" (spec 018, FR-005)
        public async Task<JsonResult> OnGetSuppliersAsync()
        {
            var suppliers = await catalogService.GetCalculatorSuppliersAsync();
            return new JsonResult(suppliers.Select(s => new
            {
                s.Id,
                s.Name,
                s.TaxPercentageDefault,
                s.ExchangeRateDefault,
                s.ShippingCostDefault
            }));
        }
    }
}
