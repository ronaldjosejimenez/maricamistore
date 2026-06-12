using MariCamiStore.Infrastructure.Persistance;
using MariCamiStore.Model;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace MariCamiStore.Services;

/// <summary>A service for accessing sales information.</summary>
public class SalesService(
    ILogger<SalesService> logger,
    MariCamiStoreContext context) : ISalesService
{
    /// <summary>(Immutable) the logger.</summary>
    private readonly ILogger<SalesService> _logger = logger;

    /// <summary>(Immutable) the context.</summary>
    private readonly MariCamiStoreContext _context = context;

    /// <summary>Gets active orders asynchronous.</summary>
    /// <returns>The active orders.</returns>
    public async Task<IEnumerable<Order>> GetActiveOrdersAsync()
    {
        try
        {
            return await _context.Orders
                .Where(o => o.Status == OrderStatus.Active.Key)
                .ToListAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active orders");
            throw;
        }
    }
}