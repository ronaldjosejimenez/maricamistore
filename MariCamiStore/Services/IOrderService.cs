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
    bool HasImage,
    Guid ProductTypeId,
    decimal ListPrice,
    decimal ListPriceTaxWithTax,
    decimal RealPrice,
    decimal EstimateShipping,
    decimal ServiceFeeInLocal,
    decimal AgreedPriceInLocal,
    bool IsReceived,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record OrderTotalsDto(
    Guid OrderId,
    decimal TotalAgreedPriceInLocal,
    decimal ShippingAmountToCR,
    decimal TotalWithoutTaxes,
    decimal TaxesAmount,
    decimal TotalToPayToSupplier,
    decimal TotalOfTheOrder,
    decimal EstimatedProfitInLocal);

public record TransitionOrderDto(
    Guid OrderId,
    string ToStatus,
    DateTime TransitionDate,
    string? Notes,
    string? Justification,
    decimal? ActualShippingAmountToCR = null);

public interface IOrderService
{
    Task<List<Order>> GetOrdersAsync(string? statusFilter = null);
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
}
