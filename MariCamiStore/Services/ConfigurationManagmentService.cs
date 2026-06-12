using MariCamiStore.Infrastructure.Persistance;
using MariCamiStore.Model;
using Microsoft.EntityFrameworkCore;

namespace MariCamiStore.Services;

/// <summary>A service for accessing configuration managments information.</summary>
public class ConfigurationManagmentService(
    ILogger<ConfigurationManagmentService> logger,
    MariCamiStoreContext context) : IConfigurationManagmentService
{
    /// <summary>(Immutable) the logger.</summary>
    private readonly ILogger<ConfigurationManagmentService> _logger = logger;

    /// <summary>(Immutable) the context.</summary>
    private readonly MariCamiStoreContext _context = context;

    /// <summary>Gets currencies asynchronous.</summary>
    /// <returns>The currencies.</returns>
    public async Task<List<Currency>> GetCurrenciesAsync()
    {
        try
        {
            return await _context.Currencies.ToListAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting currencies");
            throw;
        }
    }
}
