using MariCamiStore.Model;
using MariCamiStore.Pages.Shared;
using MariCamiStore.Services;
using Microsoft.AspNetCore.Mvc;

namespace MariCamiStore.Pages.Orders;

public record OrderItemDto(
    Guid Id,
    Guid OrderId,
    Guid CustomerId,
    string ProductDescription,
    string? ProductLink,
    string? ProductSourceCode,
    string? ProductImageBase64,
    Guid ProductTypeId,
    decimal ListPrice,
    decimal ListPriceTaxWithTax,
    decimal RealPrice,
    decimal EstimateShipping,
    decimal ServiceFeeInLocal,
    decimal AgreedPriceInLocal);

public record OrderHeaderDto(
    Guid OrderId,
    decimal ExchangeRate,
    decimal TaxPercentage,
    decimal ShippingAmountIntern,
    decimal DiscountAmount);

public class ItemsModel(
    IOrderService orderService,
    ICatalogService catalogService,
    ICurrentOrganizationService currentOrg)
    : OrganizationPageModel(currentOrg)
{
    public Order? Order { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid orderId)
    {
        var guard = CheckOrganization();
        if (guard != null) return guard;
        Order = await orderService.GetOrderAsync(orderId);
        if (Order == null) return NotFound();

        var config = await catalogService.GetConfigurationAsync();
        var localCurrency = config != null ? await catalogService.GetCurrencyByIdAsync(config.LocalCurrencyId) : null;
        ViewData["LocalCurrencySign"] = localCurrency?.Sign ?? string.Empty;

        var orderCurrency = await catalogService.GetCurrencyByIdAsync(Order.CurrencyId);
        ViewData["OrderCurrencySign"] = orderCurrency?.Sign ?? string.Empty;

        return Page();
    }

    public async Task<JsonResult> OnGetLoadAsync(Guid orderId)
    {
        var items = await orderService.GetOrderItemsWithCustomerAsync(orderId);
        return new JsonResult(items);
    }

    public async Task<JsonResult> OnPostToggleReceivedAsync([FromBody] ToggleReceivedRequest request)
    {
        var (success, error) = await orderService.ToggleIsReceivedAsync(request.ItemId, request.IsReceived);
        return new JsonResult(new { success, error });
    }

    public record ToggleReceivedRequest(Guid ItemId, bool IsReceived);

    public async Task<JsonResult> OnGetProductTypeAsync(Guid id)
    {
        var pt = await orderService.GetProductTypeValuesAsync(id);
        if (pt == null) return new JsonResult(null);
        return new JsonResult(new { pt.EstimateShipping, pt.ServiceFeeInLocal, pt.CurrencyId });
    }

    // T014: filter ProductTypes by currency
    public async Task<JsonResult> OnGetProductTypesByCurrencyAsync(Guid currencyId)
    {
        var types = await catalogService.GetProductTypesByCurrencyAsync(currencyId);
        return new JsonResult(types.Select(t => new { t.Id, t.Name }));
    }

    // T015: serve item image as binary on demand
    public async Task<IActionResult> OnGetItemImageAsync(Guid itemId)
    {
        try
        {
            var item = await orderService.GetOrderItemAsync(itemId);
            if (item?.ProductImage == null) return NotFound();

            var contentType = DetectImageContentType(item.ProductImage);
            return File(item.ProductImage, contentType);
        }
        catch
        {
            return StatusCode(500);
        }
    }

    private static string DetectImageContentType(byte[] bytes)
    {
        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xD8) return "image/jpeg";
        if (bytes.Length >= 4 && bytes[0] == 0x89 && bytes[1] == 0x50) return "image/png";
        if (bytes.Length >= 3 && bytes[0] == 0x47 && bytes[1] == 0x49) return "image/gif";
        return "image/jpeg";
    }

    public async Task<JsonResult> OnGetHistoryAsync(Guid orderId) =>
        new JsonResult(await orderService.GetOrderStatusHistoryAsync(orderId));

    // T012: insert with DTO supporting image as base64
    public async Task<JsonResult> OnPostInsertAsync([FromBody] OrderItemDto dto)
    {
        byte[]? imageBytes = null;
        if (!string.IsNullOrEmpty(dto.ProductImageBase64))
        {
            imageBytes = Convert.FromBase64String(dto.ProductImageBase64);
            if (imageBytes.Length > 2097152)
                return new JsonResult(new { error = "La imagen supera el límite de 2 MB." });
        }

        var item = new OrderItem
        {
            OrderId = dto.OrderId,
            CustomerId = dto.CustomerId,
            ProductDescription = dto.ProductDescription,
            ProductLink = dto.ProductLink ?? string.Empty,
            ProductSourceCode = dto.ProductSourceCode ?? string.Empty,
            ProductImage = imageBytes,
            ProductTypeId = dto.ProductTypeId,
            ListPrice = dto.ListPrice,
            ListPriceTaxWithTax = dto.ListPriceTaxWithTax,
            RealPrice = dto.RealPrice,
            EstimateShipping = dto.EstimateShipping,
            ServiceFeeInLocal = dto.ServiceFeeInLocal,
            AgreedPriceInLocal = dto.AgreedPriceInLocal
        };
        var created = await orderService.CreateOrderItemAsync(item);
        return new JsonResult(new
        {
            created.Id,
            created.OrderId,
            created.CustomerId,
            created.ProductDescription,
            created.ProductLink,
            created.ProductSourceCode,
            HasImage = created.ProductImage != null,
            created.ProductTypeId,
            created.ListPrice,
            created.ListPriceTaxWithTax,
            created.RealPrice,
            created.EstimateShipping,
            created.ServiceFeeInLocal,
            created.AgreedPriceInLocal
        });
    }

    // T013: update with DTO; null image = preserve; "" = clear
    public async Task<JsonResult> OnPostUpdateAsync([FromBody] OrderItemDto dto)
    {
        var existing = (await orderService.GetOrderItemsAsync(dto.OrderId))
            .FirstOrDefault(i => i.Id == dto.Id);
        if (existing == null)
            return new JsonResult(new { error = "Artículo no encontrado." });

        byte[]? imageBytes = existing.ProductImage;
        if (dto.ProductImageBase64 == string.Empty)
        {
            imageBytes = null;
        }
        else if (!string.IsNullOrEmpty(dto.ProductImageBase64))
        {
            imageBytes = Convert.FromBase64String(dto.ProductImageBase64);
            if (imageBytes.Length > 2097152)
                return new JsonResult(new { error = "La imagen supera el límite de 2 MB." });
        }

        existing.CustomerId = dto.CustomerId;
        existing.ProductDescription = dto.ProductDescription;
        existing.ProductLink = dto.ProductLink ?? string.Empty;
        existing.ProductSourceCode = dto.ProductSourceCode ?? string.Empty;
        existing.ProductImage = imageBytes;
        existing.ProductTypeId = dto.ProductTypeId;
        existing.ListPrice = dto.ListPrice;
        existing.ListPriceTaxWithTax = dto.ListPriceTaxWithTax;
        existing.RealPrice = dto.RealPrice;
        existing.EstimateShipping = dto.EstimateShipping;
        existing.ServiceFeeInLocal = dto.ServiceFeeInLocal;
        existing.AgreedPriceInLocal = dto.AgreedPriceInLocal;

        var updated = await orderService.UpdateOrderItemAsync(existing);
        return new JsonResult(new
        {
            updated.Id,
            updated.OrderId,
            updated.CustomerId,
            updated.ProductDescription,
            updated.ProductLink,
            updated.ProductSourceCode,
            HasImage = updated.ProductImage != null,
            updated.ProductTypeId,
            updated.ListPrice,
            updated.ListPriceTaxWithTax,
            updated.RealPrice,
            updated.EstimateShipping,
            updated.ServiceFeeInLocal,
            updated.AgreedPriceInLocal
        });
    }

    public async Task<JsonResult> OnPostDeleteAsync([FromBody] DeleteRequest request)
    {
        var (success, error) = await orderService.DeleteOrderItemAsync(request.Id);
        return new JsonResult(new { success, error });
    }

    public async Task<JsonResult> OnPostUpdateTotalsAsync([FromBody] OrderTotalsDto totals)
    {
        await orderService.UpdateOrderTotalsAsync(totals);
        return new JsonResult(new { success = true });
    }

    // T016: in-place order header update + Trigger A recalculation
    public async Task<JsonResult> OnPostUpdateOrderAsync([FromBody] OrderHeaderDto dto)
    {
        var order = await orderService.GetOrderAsync(dto.OrderId);
        if (order == null)
            return new JsonResult(new { success = false, error = "Orden no encontrada." });
        if (order.Status != "Pending")
            return new JsonResult(new { success = false, error = "Solo se pueden editar órdenes en estado Pendiente." });
        if (dto.ExchangeRate <= 0)
            return new JsonResult(new { success = false, error = "El tipo de cambio debe ser mayor a cero." });

        bool taxChanged = order.TaxPercentage != dto.TaxPercentage;
        bool rateChanged = order.ExchangeRate != dto.ExchangeRate;

        order.ExchangeRate = dto.ExchangeRate;
        order.TaxPercentage = dto.TaxPercentage;
        order.ShippingAmountIntern = dto.ShippingAmountIntern;
        order.DiscountAmount = dto.DiscountAmount;

        await orderService.UpdateOrderAsync(order);

        // Trigger A: recalculate item-level fields when tax or rate changed
        if (taxChanged || rateChanged)
            await orderService.RecalcItemsOnOrderSaveAsync(dto.OrderId, dto.TaxPercentage, dto.ExchangeRate, rateChanged);

        return new JsonResult(new { success = true });
    }

    public record DeleteRequest(Guid Id);

    public record ReasignarRequest(Guid ItemId, Guid NewCustomerId, decimal NewAgreedPriceInLocal);

    public async Task<JsonResult> OnPostReasignarAsync([FromBody] ReasignarRequest request)
    {
        var (success, error) = await orderService.ReasignarItemAsync(
            request.ItemId, request.NewCustomerId, request.NewAgreedPriceInLocal);
        return new JsonResult(new { success, error });
    }
}
