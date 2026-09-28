using MariCamiStore.Model;
using MariCamiStore.Pages.Shared;
using MariCamiStore.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MariCamiStore.Pages.Orders;

public class IndexModel(IOrderService orderService, ICatalogService catalogService, ICurrentOrganizationService currentOrg)
    : OrganizationPageModel(currentOrg)
{
    /// <summary>Gets the status keys initially selected in the filter checkboxes.</summary>
    public IReadOnlyList<string> SelectedStatuses { get; private set; } = OrderStatus.DefaultFilterKeys;

    public async Task<IActionResult> OnGetAsync()
    {
        var guard = CheckOrganization();
        if (guard != null) return guard;
        SelectedStatuses = OrderStatus.ParseFilter(Request.Query["statuses"], Request.Query.ContainsKey("statuses"));
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

    public async Task<JsonResult> OnGetLoadAsync(string? statuses)
    {
        var selected = OrderStatus.ParseFilter(statuses, Request.Query.ContainsKey("statuses"));
        var orders = await orderService.GetOrdersAsync(selected);
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
        "Completed"  => [], // final: a completed order cannot be voided
        _            => []
    };
}
