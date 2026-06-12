using MariCamiStore.Infrastructure.Persistance;
using MariCamiStore.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MariCamiStore.Services;

public class OrderService(
    MariCamiStoreContext context,
    ICurrentOrganizationService currentOrg,
    ICxPService cxpService,
    ILogger<OrderService> logger) : IOrderService
{
    // ── Orders ───────────────────────────────────────────────────────────────

    public async Task<List<Order>> GetOrdersAsync(string? statusFilter = null)
    {
        var query = context.Orders.AsQueryable();
        if (!string.IsNullOrEmpty(statusFilter))
        {
            var statuses = statusFilter.Split(',', StringSplitOptions.RemoveEmptyEntries);
            query = query.Where(o => statuses.Contains(o.Status));
        }
        return await query.OrderByDescending(o => o.CreatedAt).ToListAsync();
    }

    public async Task<Dictionary<Guid, int>> GetOrderItemCountsAsync(IEnumerable<Guid> orderIds)
    {
        var ids = orderIds.ToList();
        return await context.OrderItems
            .Where(i => ids.Contains(i.OrderId))
            .GroupBy(i => i.OrderId)
            .Select(g => new { OrderId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.OrderId, x => x.Count);
    }

    public Task<Order?> GetOrderAsync(Guid id) =>
        context.Orders.FirstOrDefaultAsync(o => o.Id == id);

    public async Task<Order> CreateOrderAsync(Order order)
    {
        order.Id = Guid.NewGuid();
        order.OrganizationId = currentOrg.OrganizationId;
        order.Status = OrderStatus.Pending.Key;
        order.CreatedAt = DateTime.UtcNow;
        order.UpdatedAt = DateTime.UtcNow;

        // Pre-fill from active configuration if not provided
        if (order.ExchangeRate == 0 || order.TaxPercentage == 0)
        {
            var config = await context.Configurations.FirstOrDefaultAsync();
            if (config != null)
            {
                if (order.ExchangeRate == 0) order.ExchangeRate = config.ExchangeRate;
                if (order.TaxPercentage == 0) order.TaxPercentage = config.TaxPercentage;
            }
        }

        context.Orders.Add(order);
        await context.SaveChangesAsync();
        return order;
    }

    public async Task<Order> UpdateOrderAsync(Order order)
    {
        order.UpdatedAt = DateTime.UtcNow;
        context.Orders.Update(order);
        await context.SaveChangesAsync();
        return order;
    }

    public async Task UpdateOrderTotalsAsync(OrderTotalsDto totals)
    {
        var order = await context.Orders.FirstOrDefaultAsync(o => o.Id == totals.OrderId);
        if (order == null) return;

        order.TotalAgreedPriceInLocal = totals.TotalAgreedPriceInLocal;
        order.ShippingAmountToCR = totals.ShippingAmountToCR;
        order.TotalWithoutTaxes = totals.TotalWithoutTaxes;
        order.TaxesAmount = totals.TaxesAmount;
        order.TotalToPayToSupplier = totals.TotalToPayToSupplier;
        order.TotalOfTheOrder = totals.TotalOfTheOrder;
        order.EstimatedProfitInLocal = totals.EstimatedProfitInLocal;
        order.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
    }

    // Trigger A: recalculate item-level fields + order totals when TaxPercentage or ExchangeRate changes
    public async Task RecalcItemsOnOrderSaveAsync(Guid orderId, decimal newTaxPercentage, decimal newExchangeRate, bool exchangeRateChanged)
    {
        var order = await context.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
        if (order == null) return;

        var items = await context.OrderItems.Where(i => i.OrderId == orderId).ToListAsync();

        foreach (var item in items)
        {
            item.ListPriceTaxWithTax = Math.Round(item.ListPrice + item.ListPrice * (newTaxPercentage / 100), 2);

            // FR-020: force-recalculate AgreedPriceInLocal when ExchangeRate changed
            if (exchangeRateChanged)
                item.AgreedPriceInLocal = Math.Round(item.ListPriceTaxWithTax * newExchangeRate + item.ServiceFeeInLocal, 2);
        }

        // Order Calculation Suite — run after item fields are updated
        ApplyOrderCalculationSuite(order, items);

        await context.SaveChangesAsync();
    }

    private static void ApplyOrderCalculationSuite(Order order, List<OrderItem> items)
    {
        order.TotalAgreedPriceInLocal = Math.Round(items.Sum(i => i.AgreedPriceInLocal), 2);
        order.ShippingAmountToCR     = Math.Round(items.Sum(i => i.EstimateShipping), 2);
        order.TotalWithoutTaxes      = Math.Round(items.Sum(i => i.RealPrice), 2);
        order.TaxesAmount            = Math.Round((order.TotalWithoutTaxes - order.DiscountAmount) * (order.TaxPercentage / 100), 2);
        order.TotalToPayToSupplier   = Math.Round(order.ShippingAmountIntern + order.TotalWithoutTaxes + order.TaxesAmount - order.DiscountAmount, 2);
        order.TotalOfTheOrder        = Math.Round(order.TotalToPayToSupplier + order.ShippingAmountToCR, 2);
        order.EstimatedProfitInLocal = Math.Round(order.TotalAgreedPriceInLocal - order.TotalOfTheOrder * order.ExchangeRate, 2);
        order.UpdatedAt              = DateTime.UtcNow;
    }

    // ── Order Items ───────────────────────────────────────────────────────────

    public Task<List<OrderItem>> GetOrderItemsAsync(Guid orderId) =>
        context.OrderItems.Where(i => i.OrderId == orderId).ToListAsync();

    public async Task<List<OrderItemWithCustomerDto>> GetOrderItemsWithCustomerAsync(Guid orderId)
    {
        var items = await context.OrderItems
            .Where(i => i.OrderId == orderId)
            .ToListAsync();

        var customerIds = items.Select(i => i.CustomerId).Distinct().ToList();
        var customers = await context.Customers
            .Where(c => customerIds.Contains(c.Id))
            .ToListAsync();

        var displayNames = customers.ToDictionary(
            c => c.Id,
            c => c.NickName != "" ? c.NickName : (c.Name ?? c.Id.ToString()));

        return items
            .Select(i => new OrderItemWithCustomerDto(
                i.Id, i.OrderId, i.CustomerId,
                displayNames.GetValueOrDefault(i.CustomerId, i.CustomerId.ToString()),
                i.ProductDescription, i.ProductLink, i.ProductSourceCode,
                i.ProductImage != null, i.ProductTypeId,
                i.ListPrice, i.ListPriceTaxWithTax, i.RealPrice,
                i.EstimateShipping, i.ServiceFeeInLocal, i.AgreedPriceInLocal,
                i.IsReceived, i.CreatedAt, i.UpdatedAt))
            .OrderBy(x => x.CustomerDisplayName)
            .ThenByDescending(x => x.CreatedAt)
            .ToList();
    }

    public async Task<(bool Success, string? Error)> ToggleIsReceivedAsync(Guid itemId, bool isReceived)
    {
        var item = await context.OrderItems.FindAsync(itemId);
        if (item == null) return (false, "Artículo no encontrado.");

        var order = await context.Orders.FirstOrDefaultAsync(o => o.Id == item.OrderId);
        if (order == null) return (false, "Orden no encontrada.");

        if (order.Status != OrderStatus.Delivering.Key && order.Status != OrderStatus.Delivered.Key)
            return (false, "Solo se puede marcar recepción en órdenes en estado Delivering o Delivered.");

        item.IsReceived = isReceived;
        item.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return (true, null);
    }

    public Task<OrderItem?> GetOrderItemAsync(Guid itemId) =>
        context.OrderItems.FirstOrDefaultAsync(i => i.Id == itemId);

    public async Task<OrderItem> CreateOrderItemAsync(OrderItem item)
    {
        item.Id = Guid.NewGuid();
        item.CreatedAt = DateTime.UtcNow;
        item.UpdatedAt = DateTime.UtcNow;
        context.OrderItems.Add(item);
        await context.SaveChangesAsync();
        return item;
    }

    public async Task<OrderItem> UpdateOrderItemAsync(OrderItem item)
    {
        item.UpdatedAt = DateTime.UtcNow;
        context.OrderItems.Update(item);
        await context.SaveChangesAsync();
        return item;
    }

    public async Task<(bool Success, string? Error)> DeleteOrderItemAsync(Guid itemId)
    {
        var item = await context.OrderItems.FindAsync(itemId);
        if (item == null) return (false, "Artículo no encontrado.");

        var order = await context.Orders.FirstOrDefaultAsync(o => o.Id == item.OrderId);
        if (order?.Status != OrderStatus.Pending.Key)
            return (false, "Solo se pueden eliminar artículos de órdenes en estado Pendiente.");

        context.OrderItems.Remove(item);
        await context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteOrderAsync(Guid orderId)
    {
        var order = await context.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
        if (order == null) return (false, "Orden no encontrada.");

        if (order.Status != OrderStatus.Pending.Key)
            return (false, "Solo se pueden eliminar órdenes en estado Pendiente.");

        var items = await context.OrderItems.Where(i => i.OrderId == orderId).ToListAsync();
        context.OrderItems.RemoveRange(items);
        context.Orders.Remove(order);
        await context.SaveChangesAsync();
        return (true, null);
    }

    public Task<ProductType?> GetProductTypeValuesAsync(Guid productTypeId) =>
        context.ProductTypes.FirstOrDefaultAsync(p => p.Id == productTypeId);

    // ── Status Transitions ────────────────────────────────────────────────────

    public async Task<(bool Success, string? Error)> TransitionOrderAsync(TransitionOrderDto dto)
    {
        var order = await context.Orders.FirstOrDefaultAsync(o => o.Id == dto.OrderId);
        if (order == null) return (false, "Orden no encontrada.");

        var validationError = ValidateTransition(order.Status, dto.ToStatus, dto.Justification);
        if (validationError != null) return (false, validationError);

        // Validate at least 1 item for Active transition
        if (dto.ToStatus == OrderStatus.Active.Key)
        {
            var itemCount = await context.OrderItems.CountAsync(i => i.OrderId == order.Id);
            if (itemCount == 0) return (false, "La orden debe tener al menos un artículo para activarse.");
        }

        var history = new OrderStatusHistory
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            FromStatus = order.Status,
            ToStatus = dto.ToStatus,
            TransitionDate = dto.TransitionDate,
            Notes = dto.Notes,
            Justification = dto.Justification,
            CreatedAt = DateTime.UtcNow
        };

        var previousStatus = order.Status;
        order.Status = dto.ToStatus;
        order.UpdatedAt = DateTime.UtcNow;

        context.OrderStatusHistories.Add(history);

        // Transaction automation
        if (dto.ToStatus == OrderStatus.Active.Key)
        {
            var items = await context.OrderItems.Where(i => i.OrderId == order.Id).ToListAsync();
            foreach (var item in items)
            {
                context.Transactions.Add(BuildTransaction(order, item, TransactionType.Charge.Key));
            }
        }
        else if (dto.ToStatus == OrderStatus.Voided.Key && previousStatus != OrderStatus.Pending.Key)
        {
            var items = await context.OrderItems.Where(i => i.OrderId == order.Id).ToListAsync();
            foreach (var item in items)
            {
                context.Transactions.Add(BuildTransaction(order, item, TransactionType.Void.Key));
            }
        }

        await context.SaveChangesAsync();

        // CxP auto-entries (fire-and-forget style; order transition must not fail due to CxP errors)
        try
        {
            if (dto.ToStatus == OrderStatus.Active.Key)
            {
                var period = await cxpService.GetOpenPeriodAsync();
                if (period != null)
                    await cxpService.CreateAutoEntryAsync(period.Id, order.Id, order.CurrencyId, order.TotalToPayToSupplier, order.NameOfOrder, "AutoActiva");
                else
                    logger.LogWarning("No open CxP period when activating order {OrderId} — AutoActiva entry skipped.", order.Id);
            }
            else if (dto.ToStatus == OrderStatus.Delivered.Key)
            {
                order.ActualShippingAmountToCR = dto.ActualShippingAmountToCR ?? order.ShippingAmountToCR;
                await context.SaveChangesAsync();

                var period = await cxpService.GetOpenPeriodAsync();
                if (period != null)
                    await cxpService.CreateAutoEntryAsync(period.Id, order.Id, order.CurrencyId, order.ActualShippingAmountToCR, order.NameOfOrder, "AutoDelivered");
                else
                    logger.LogWarning("No open CxP period when delivering order {OrderId} — AutoDelivered entry skipped.", order.Id);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "CxP auto-entry failed for order {OrderId} transitioning to {ToStatus}.", order.Id, dto.ToStatus);
        }

        return (true, null);
    }

    public Task<List<OrderStatusHistory>> GetOrderStatusHistoryAsync(Guid orderId) =>
        context.OrderStatusHistories
            .Where(h => h.OrderId == orderId)
            .OrderBy(h => h.CreatedAt)
            .ToListAsync();

    // ── Private helpers ───────────────────────────────────────────────────────

    private static string? ValidateTransition(string fromStatus, string toStatus, string? justification)
    {
        var allowed = new Dictionary<string, string[]>
        {
            [OrderStatus.Pending.Key]    = [OrderStatus.Active.Key],
            [OrderStatus.Active.Key]     = [OrderStatus.Delivering.Key, OrderStatus.Voided.Key],
            [OrderStatus.Delivering.Key] = [OrderStatus.Delivered.Key,  OrderStatus.Voided.Key],
            [OrderStatus.Delivered.Key]  = [OrderStatus.Completed.Key,  OrderStatus.Voided.Key],
            [OrderStatus.Completed.Key]  = [OrderStatus.Voided.Key],
        };

        if (!allowed.TryGetValue(fromStatus, out var targets) || !targets.Contains(toStatus))
            return $"Transición '{fromStatus}' → '{toStatus}' no permitida.";

        if (toStatus == OrderStatus.Voided.Key && string.IsNullOrWhiteSpace(justification))
            return "Se requiere justificación para anular una orden.";

        return null;
    }

    // ── Reassignment ─────────────────────────────────────────────────────────

    public async Task<(bool Success, string? Error)> ReasignarItemAsync(
        Guid itemId, Guid newCustomerId, decimal newAgreedPriceInLocal)
    {
        var item = await context.OrderItems.FindAsync(itemId);
        if (item == null) return (false, "Ítem no encontrado.");

        var order = await context.Orders.FindAsync(item.OrderId);
        if (order == null) return (false, "Orden no encontrada.");

        var allowed = new[] {
            OrderStatus.Active.Key, OrderStatus.Delivering.Key,
            OrderStatus.Delivered.Key, OrderStatus.Completed.Key
        };
        if (!allowed.Contains(order.Status))
            return (false, $"No se puede reasignar en estado {order.Status}.");

        bool customerChanged = item.CustomerId != newCustomerId;
        bool priceChanged    = item.AgreedPriceInLocal != newAgreedPriceInLocal;
        if (!customerChanged && !priceChanged)
            return (true, null);

        var config    = context.Configurations.FirstOrDefault();
        var txDate    = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(
                            DateTime.UtcNow, "Central America Standard Time");
        var truncDesc = item.ProductDescription.Length > 100
                            ? item.ProductDescription[..100]
                            : item.ProductDescription;

        if (customerChanged)
        {
            var description = $"Reasignación – {order.NameOfOrder} – {truncDesc}";

            context.Transactions.Add(new Transaction
            {
                Id                     = Guid.NewGuid(),
                OrganizationId         = order.OrganizationId,
                SourceId               = item.Id,
                Source                 = TransactionSource.OrderItem.Key,
                CustomerId             = item.CustomerId,
                TransactionType        = TransactionType.Void.Key,
                TransactionDescription = description,
                TransactionAmount      = item.AgreedPriceInLocal,
                TransactionDate        = txDate,
                Status                 = TransactionStatus.Applied.Key,
                CurrencyId             = config?.LocalCurrencyId ?? Guid.Empty
            });

            context.Transactions.Add(new Transaction
            {
                Id                     = Guid.NewGuid(),
                OrganizationId         = order.OrganizationId,
                SourceId               = item.Id,
                Source                 = TransactionSource.OrderItem.Key,
                CustomerId             = newCustomerId,
                TransactionType        = TransactionType.Charge.Key,
                TransactionDescription = description,
                TransactionAmount      = newAgreedPriceInLocal,
                TransactionDate        = txDate,
                Status                 = TransactionStatus.Applied.Key,
                CurrencyId             = config?.LocalCurrencyId ?? Guid.Empty
            });

            item.CustomerId = newCustomerId;
        }
        else
        {
            var diff   = Math.Abs(newAgreedPriceInLocal - item.AgreedPriceInLocal);
            bool isUp  = newAgreedPriceInLocal > item.AgreedPriceInLocal;
            var txType = isUp ? TransactionType.Charge.Key : TransactionType.Payment.Key;
            var txDesc = isUp
                ? $"Ajuste de precio – {truncDesc}"
                : $"Descuento por ajuste de precio – {truncDesc}";

            context.Transactions.Add(new Transaction
            {
                Id                     = Guid.NewGuid(),
                OrganizationId         = order.OrganizationId,
                SourceId               = item.Id,
                Source                 = TransactionSource.OrderItem.Key,
                CustomerId             = item.CustomerId,
                TransactionType        = txType,
                TransactionDescription = txDesc,
                TransactionAmount      = diff,
                TransactionDate        = txDate,
                Status                 = TransactionStatus.Applied.Key,
                CurrencyId             = config?.LocalCurrencyId ?? Guid.Empty
            });
        }

        item.AgreedPriceInLocal = newAgreedPriceInLocal;
        item.UpdatedAt          = DateTime.UtcNow;

        var otherTotal = await context.OrderItems
            .Where(i => i.OrderId == item.OrderId && i.Id != itemId)
            .SumAsync(i => i.AgreedPriceInLocal);
        order.TotalAgreedPriceInLocal = Math.Round(otherTotal + newAgreedPriceInLocal, 2);
        order.EstimatedProfitInLocal  = Math.Round(
            order.TotalAgreedPriceInLocal - order.TotalOfTheOrder * order.ExchangeRate, 2);
        order.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return (true, null);
    }

    private Transaction BuildTransaction(Order order, OrderItem item, string type)
    {
        var config = context.Configurations.FirstOrDefault();
        var description = type == TransactionType.Charge.Key
            ? $"Cargo – {item.ProductDescription} – Orden {order.NameOfOrder}"
            : $"Anulación – {item.ProductDescription} – Orden {order.NameOfOrder}";

        return new Transaction
        {
            Id = Guid.NewGuid(),
            OrganizationId = order.OrganizationId,
            SourceId = item.Id,
            Source = TransactionSource.OrderItem.Key,
            CustomerId = item.CustomerId,
            TransactionType = type,
            TransactionDescription = description,
            TransactionAmount = item.AgreedPriceInLocal,
            TransactionDate = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Central America Standard Time"),
            Status = TransactionStatus.Applied.Key,
            CurrencyId = config?.LocalCurrencyId ?? Guid.Empty
        };
    }
}
