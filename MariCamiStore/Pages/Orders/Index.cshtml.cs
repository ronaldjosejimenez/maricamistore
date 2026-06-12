using MariCamiStore.Model;
using MariCamiStore.Pages.Shared;
using MariCamiStore.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MariCamiStore.Pages.Orders;

public class IndexModel(IOrderService orderService, ICatalogService catalogService, ICurrentOrganizationService currentOrg)
    : OrganizationPageModel(currentOrg)
{
    public async Task<IActionResult> OnGetAsync()
    {
        var guard = CheckOrganization();
        if (guard != null) return guard;
        ViewData["LocalCurrencySign"] = await GetLocalCurrencySignAsync();
        return Page();
    }

    private async Task<string> GetLocalCurrencySignAsync()
    {
        var config = await catalogService.GetConfigurationAsync();
        if (config == null) return string.Empty;
        var currency = await catalogService.GetCurrencyByIdAsync(config.LocalCurrencyId);
        return currency?.Sign ?? string.Empty;
    }

    public async Task<JsonResult> OnGetLoadAsync(string? statusFilter = "Pending,Active")
    {
        var orders = await orderService.GetOrdersAsync(statusFilter);
        var itemCounts = await orderService.GetOrderItemCountsAsync(orders.Select(o => o.Id));
        return new JsonResult(orders.Select(o => new
        {
            o.Id, o.NameOfOrder, o.SupplierId, o.CurrencyId, o.Status,
            StatusLabel = OrderStatus.FromKey(o.Status).Name,
            o.ExchangeRate, o.TaxPercentage, o.ShippingAmountIntern,
            o.ShippingAmountToCR, o.DiscountAmount, o.TotalWithoutTaxes,
            o.TaxesAmount, o.TotalToPayToSupplier, o.TotalOfTheOrder,
            o.EstimatedProfitInLocal, o.CreatedAt,
            ItemCount = itemCounts.TryGetValue(o.Id, out var cnt) ? cnt : 0,
            CanEdit = o.Status == OrderStatus.Pending.Key,
            NextStatuses = GetNextStatuses(o.Status)
        }));
    }
    public async Task<JsonResult> OnPostCreateAsync([FromBody] Order item)
    {
        var created = await orderService.CreateOrderAsync(item);
        return new JsonResult(created);
    }
    public async Task<JsonResult> OnPostUpdateAsync([FromBody] Order item)
    {
        var updated = await orderService.UpdateOrderAsync(item);
        return new JsonResult(updated);
    }
    public async Task<JsonResult> OnPostTransitionAsync([FromBody] TransitionOrderDto dto)
    {
        var (success, error) = await orderService.TransitionOrderAsync(dto);
        if (!success) return new JsonResult(new { success = false, error });

        var order = await orderService.GetOrderAsync(dto.OrderId);
        return new JsonResult(new
        {
            success = true,
            newStatus = order!.Status,
            newStatusLabel = OrderStatus.FromKey(order.Status).Name
        });
    }

    public async Task<JsonResult> OnGetConfigurationAsync()
    {
        var config = await catalogService.GetConfigurationAsync();
        return new JsonResult(new { config?.ExchangeRate, config?.TaxPercentage, CurrencyId = config?.OrderCurrencyIdDefault });
    }

    public async Task<JsonResult> OnPostDeleteAsync([FromBody] DeleteRequest request)
    {
        var (success, error) = await orderService.DeleteOrderAsync(request.Id);
        return new JsonResult(new { success, error });
    }

    public record DeleteRequest(Guid Id);

    private static string[] GetNextStatuses(string current) => current switch
    {
        "Pending"    => ["Active"],
        "Active"     => ["Delivering", "Voided"],
        "Delivering" => ["Delivered", "Voided"],
        "Delivered"  => ["Completed", "Voided"],
        "Completed"  => ["Voided"],
        _            => []
    };
}
