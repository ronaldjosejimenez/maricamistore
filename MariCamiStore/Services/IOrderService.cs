using MariCamiStore.Model;

namespace MariCamiStore.Services;

public record OrderItemWithCustomerDto(
    Guid Id,
    Guid OrderId,
    Guid CustomerId,
    string CustomerDisplayName,
    string ProductDescription,
    string? ProductLink,
    string? ProductSourceCode,
    string Size,
    bool HasImage,
    Guid ProductTypeId,
    decimal ListPrice,
    decimal ListPriceTaxWithTax,
    decimal RealPrice,
    decimal RealPriceInLocal,
    decimal EstimateShipping,
    decimal ServiceFeeInLocal,
    decimal AgreedPriceInLocal,
    bool IsReceived,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string Status);

/// <summary>The monetary totals of an order, computed from its active items.</summary>
public record OrderTotals(
    decimal TotalAgreedPriceInLocal,
    decimal ShippingAmountToCR,
    decimal TotalWithoutTaxes,
    decimal TaxesAmount,
    decimal TotalToPayToSupplier,
    decimal TotalOfTheOrder,
    decimal EstimatedProfitInLocal);

public record OrderTotalsDto(
    Guid OrderId,
    decimal TotalAgreedPriceInLocal,
    decimal ShippingAmountToCR,
    decimal TotalWithoutTaxes,
    decimal TaxesAmount,
    decimal TotalToPayToSupplier,
    decimal TotalOfTheOrder,
    decimal EstimatedProfitInLocal);

/// <summary>One item to void, with the amount the supplier refunds for it (in the order currency).</summary>
public record VoidItemLine(Guid ItemId, decimal RefundAmount);

/// <summary>An item shown in the void confirmation.</summary>
public record VoidPreviewItemDto(
    Guid Id,
    string ProductDescription,
    string CustomerDisplayName,
    decimal AgreedPriceInLocal,
    decimal DefaultRefund);

/// <summary>
/// What the CxP Posición of the open period would become after the void:
/// Posición after = Current + SaldosDelta + ShippingDelta + refund × RefundRate (all in local currency).
/// The refund is typed by the user, so the client adds that last term.
/// </summary>
/// <param name="Current">Posición of the open period today.</param>
/// <param name="SaldosDelta">Change in Saldos por cobrar (negative: the customers are no longer charged).</param>
/// <param name="ShippingDelta">Reduction of the pending shipping to CR of the order (raises the Posición).</param>
/// <param name="RefundRate">Factor that converts a refund in the order currency to local currency.</param>
public record VoidPositionPreviewDto(
    decimal Current,
    decimal SaldosDelta,
    decimal ShippingDelta,
    decimal RefundRate);

/// <summary>The before/after comparison shown before voiding items.</summary>
/// <param name="Position">Null when there is no open CxP period (the void is blocked in that case).</param>
public record VoidItemsPreviewDto(
    bool AllSelected,
    OrderTotals Before,
    OrderTotals After,
    decimal TheoreticalRefund,
    VoidPositionPreviewDto? Position,
    List<VoidPreviewItemDto> Items);

public record TransitionOrderDto(
    Guid OrderId,
    string ToStatus,
    DateTime TransitionDate,
    string? Notes,
    string? Justification);

public interface IOrderService
{
    Task<List<Order>> GetOrdersAsync(IReadOnlyCollection<string> statuses);
    Task<Dictionary<Guid, int>> GetOrderItemCountsAsync(IEnumerable<Guid> orderIds);
    Task<Order?> GetOrderAsync(Guid id);
    Task<Order> CreateOrderAsync(Order order);
    Task<Order> UpdateOrderAsync(Order order);
    Task UpdateOrderTotalsAsync(OrderTotalsDto totals);
    Task RecalcItemsOnOrderSaveAsync(Guid orderId, decimal newTaxPercentage, decimal newExchangeRate, bool exchangeRateChanged);

    Task<List<OrderItem>> GetOrderItemsAsync(Guid orderId);
    Task<List<OrderItemWithCustomerDto>> GetOrderItemsWithCustomerAsync(Guid orderId);
    Task<OrderItem?> GetOrderItemAsync(Guid itemId);
    Task<OrderItem> CreateOrderItemAsync(OrderItem item);
    Task<OrderItem> UpdateOrderItemAsync(OrderItem item);
    Task<(bool Success, string? Error)> DeleteOrderItemAsync(Guid itemId);
    Task<(bool Success, string? Error)> ToggleIsReceivedAsync(Guid itemId, bool isReceived);
    Task<ProductType?> GetProductTypeValuesAsync(Guid productTypeId);

    Task<(bool Success, string? Error)> TransitionOrderAsync(TransitionOrderDto dto);
    Task<List<OrderStatusHistory>> GetOrderStatusHistoryAsync(Guid orderId);
    Task<(bool Success, string? Error)> DeleteOrderAsync(Guid orderId);
    Task<(bool Success, string? Error)> ReasignarItemAsync(Guid itemId, Guid newCustomerId, decimal newAgreedPriceInLocal);

    Task<(VoidItemsPreviewDto? Preview, string? Error)> PreviewVoidItemsAsync(Guid orderId, IReadOnlyList<Guid> itemIds);
    Task<(bool Success, string? Error)> VoidItemsAsync(Guid orderId, string? justification, IReadOnlyList<VoidItemLine> items);
}
