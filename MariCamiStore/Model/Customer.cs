namespace MariCamiStore.Model;

/// <summary>A customer.</summary>
public partial class Customer
{
    /// <summary>Gets or sets the identifier.</summary>
    /// <value>The identifier.</value>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the name.</summary>
    /// <value>The name.</value>
    public string? Name { get; set; }

    /// <summary>Gets or sets the name of the nick.</summary>
    /// <value>The name of the nick.</value>
    public string NickName { get; set; } = string.Empty;

    /// <summary>Gets or sets the address.</summary>
    /// <value>The address.</value>
    public string? Address { get; set; }

    /// <summary>Gets or sets the location link.</summary>
    /// <value>The location link.</value>
    public string? LocationLink { get; set; }

    /// <summary>Gets or sets the phone number.</summary>
    /// <value>The phone number.</value>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>Gets or sets the email.</summary>
    /// <value>The email.</value>
    public string? Email { get; set; }

    public bool IsGeneric { get; set; } = false;
}
