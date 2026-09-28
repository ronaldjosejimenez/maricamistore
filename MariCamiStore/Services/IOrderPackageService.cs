namespace MariCamiStore.Services;

public record OrderPackageDto(
    Guid Id,
    DateTime DeliveryDate,
    decimal Amount,
    string? Description,
    DateTime CreatedAt);

public record OrderPackagesDto(
    decimal EstimatedShipping,
    decimal TotalPackages,
    decimal Pending,
    bool CanManage,
    List<OrderPackageDto> Packages);

public interface IOrderPackageService
{
    /// <summary>Gets the packages of an order with the shipping summary; null when the order is not found.</summary>
    Task<OrderPackagesDto?> GetByOrderAsync(Guid orderId);

    /// <summary>Adds a package and its AutoPaquete CxP entry atomically.</summary>
    Task<(bool Success, string? Error)> AddAsync(Guid orderId, DateTime deliveryDate, decimal amount, string? description);

    /// <summary>Deletes a package and records a ReversoPaquete CxP entry atomically.</summary>
    Task<(bool Success, string? Error)> DeleteAsync(Guid packageId);
}
