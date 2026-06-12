using Microsoft.Extensions.Options;

namespace MariCamiStore.Services;

public class CurrentOrganizationService(
    IHttpContextAccessor httpContextAccessor,
    IOptions<BaseSettings> settings) : ICurrentOrganizationService
{
    private const string SessionKey = "ActiveOrganizationId";

    public Guid OrganizationId
    {
        get
        {
            var session = httpContextAccessor.HttpContext?.Session;
            if (session == null) return Guid.Empty;

            var value = session.GetString(SessionKey);
            if (Guid.TryParse(value, out var id)) return id;

            var defaultId = settings.Value.DefaultOrganizationId;
            if (defaultId.HasValue && defaultId.Value != Guid.Empty)
            {
                session.SetString(SessionKey, defaultId.Value.ToString());
                return defaultId.Value;
            }

            return Guid.Empty;
        }
    }
}
