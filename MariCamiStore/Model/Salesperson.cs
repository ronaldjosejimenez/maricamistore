namespace MariCamiStore.Model;

/// <summary>A salesperson (global catalog, not organization scoped).</summary>
public class Salesperson
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? NickName { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Email { get; set; }

    public bool IsActive { get; set; } = true;
}
