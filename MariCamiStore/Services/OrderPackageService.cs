using MariCamiStore.Infrastructure.Persistance;
using MariCamiStore.Model;
using Microsoft.EntityFrameworkCore;

namespace MariCamiStore.Services;

public class OrderPackageService(
    MariCamiStoreContext context,
    ICxPService cxpService) : IOrderPackageService
{
    private const int ReferenceMaxLength = 500;

    public async Task<OrderPackagesDto?> GetByOrderAsync(Guid orderId)
    {
        var order = await context.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == orderId);
        if (order == null) return null;

        var packages = await context.OrderPackages
            .AsNoTracking()
            .Where(p => p.OrderId == orderId)
            .OrderBy(p => p.DeliveryDate)
            .ThenBy(p => p.CreatedAt)
            .Select(p => new OrderPackageDto(p.Id, p.DeliveryDate, p.Amount, p.Description, p.CreatedAt))
            .ToListAsync();

        var total = packages.Sum(p => p.Amount);
        var estimated = order.ShippingAmountToCR;
        return new OrderPackagesDto(
            EstimatedShipping: estimated,
            TotalPackages: total,
            Pending: Math.Max(0m, estimated - total),
            CanManage: CanManage(order.Status),
            Packages: packages);
    }

    public async Task<(bool Success, string? Error)> AddAsync(Guid orderId, DateTime deliveryDate, decimal amount, string? description)
    {
        var order = await context.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
        if (order == null)
            return (false, "Orden no encontrada.");
        if (!CanManage(order.Status))
            return (false, "Solo se pueden agregar paquetes en órdenes Activas o Entregando.");
        if (amount <= 0)
            return (false, "El monto debe ser mayor a cero.");

        var desc = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        if (desc != null && desc.Length > OrderPackage.DescriptionMaxLength)
            return (false, $"La descripción no puede superar {OrderPackage.DescriptionMaxLength} caracteres.");

        var period = await cxpService.GetOpenPeriodAsync();
        if (period == null)
            return (false, "No hay un período CxP abierto.");

        var now = DateTime.UtcNow;
        var package = new OrderPackage
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            DeliveryDate = deliveryDate.Date,
            Amount = Math.Round(amount, 2),
            CurrencyId = order.CurrencyId,
            Description = desc,
            CreatedAt = now
        };
        context.OrderPackages.Add(package);

        context.CxPEntries.Add(new CxPEntry
        {
            Id = Guid.NewGuid(),
            PeriodControlId = period.Id,
            CurrencyId = package.CurrencyId,
            Amount = package.Amount,
            Reference = BuildReference(order.NameOfOrder, desc),
            Type = CxPEntryType.AutoPaquete,
            OrderId = order.Id,
            OrderPackageId = package.Id,
            CreatedAt = now
        });

        // Single SaveChanges: package and CxP entry are persisted atomically.
        await context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(Guid packageId)
    {
        var package = await context.OrderPackages
            .Include(p => p.Order)
            .FirstOrDefaultAsync(p => p.Id == packageId);
        if (package == null)
            return (false, "Paquete no encontrado.");
        if (!CanManage(package.Order.Status))
            return (false, "Solo se pueden eliminar paquetes en órdenes Activas o Entregando.");

        var period = await cxpService.GetOpenPeriodAsync();
        if (period == null)
            return (false, "No hay un período CxP abierto.");

        context.CxPEntries.Add(new CxPEntry
        {
            Id = Guid.NewGuid(),
            PeriodControlId = period.Id,
            CurrencyId = package.CurrencyId,
            Amount = -package.Amount,
            Reference = Truncate("Reverso: " + BuildReference(package.Order.NameOfOrder, package.Description)),
            Type = CxPEntryType.ReversoPaquete,
            OrderId = package.OrderId,
            OrderPackageId = null,
            CreatedAt = DateTime.UtcNow
        });

        // The original AutoPaquete entry keeps its data; its OrderPackageId is cleared by the SetNull FK.
        context.OrderPackages.Remove(package);

        // Single SaveChanges: reversal entry and package removal are persisted atomically.
        await context.SaveChangesAsync();
        return (true, null);
    }

    private static bool CanManage(string status) =>
        status == OrderStatus.Active.Key || status == OrderStatus.Delivering.Key;

    private static string BuildReference(string orderName, string? description) =>
        Truncate(string.IsNullOrWhiteSpace(description) ? orderName : $"{orderName} - {description}");

    private static string Truncate(string value) =>
        value.Length > ReferenceMaxLength ? value[..ReferenceMaxLength] : value;
}
