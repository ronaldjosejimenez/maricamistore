namespace MariCamiStore;

public class BaseSettings
{
    public string ConnectionString { get; set; } = string.Empty;

    public string DefaultCulture { get; set; } = string.Empty;

    public Guid? DefaultOrganizationId { get; set; }
}
