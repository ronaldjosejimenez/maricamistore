using MariCamiStore.Model;

namespace MariCamiStore.Services;

public interface IOrganizationService
{
    Task<List<Organization>> GetOrganizationsAsync();
    Task<Organization> CreateOrganizationAsync(Organization org);
    Task<Organization> UpdateOrganizationAsync(Organization org);
    Task<(bool Success, string? Error)> DeleteOrganizationAsync(Guid id);
}
