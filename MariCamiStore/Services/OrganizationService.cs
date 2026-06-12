using MariCamiStore.Infrastructure.Persistance;
using MariCamiStore.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MariCamiStore.Services;

public class OrganizationService(
    MariCamiStoreContext context,
    ILogger<OrganizationService> logger) : IOrganizationService
{
    public Task<List<Organization>> GetOrganizationsAsync() =>
        context.Organizations.OrderBy(o => o.Name).ToListAsync();

    public async Task<Organization> CreateOrganizationAsync(Organization org)
    {
        try
        {
            org.Id = Guid.NewGuid();
            context.Organizations.Add(org);
            await context.SaveChangesAsync();
            return org;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating organization {Name}", org.Name);
            throw;
        }
    }

    public async Task<Organization> UpdateOrganizationAsync(Organization org)
    {
        try
        {
            context.Organizations.Update(org);
            await context.SaveChangesAsync();
            return org;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating organization {Id}", org.Id);
            throw;
        }
    }

    public async Task<(bool Success, string? Error)> DeleteOrganizationAsync(Guid id)
    {
        try
        {
            var hasOrders = await context.Orders.AnyAsync(o => o.OrganizationId == id);
            if (hasOrders)
                return (false, "No se puede eliminar la organización porque tiene órdenes asociadas.");

            var hasConfig = await context.Configurations.AnyAsync(c => c.OrganizationId == id);
            if (hasConfig)
                return (false, "No se puede eliminar la organización porque tiene configuraciones asociadas.");

            var org = await context.Organizations.FindAsync(id);
            if (org == null) return (false, "Organización no encontrada.");

            context.Organizations.Remove(org);
            await context.SaveChangesAsync();
            return (true, null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting organization {Id}", id);
            return (false, "Error interno al eliminar la organización.");
        }
    }
}
