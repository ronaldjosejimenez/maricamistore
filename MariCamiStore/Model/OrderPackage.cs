namespace MariCamiStore.Model;

/// <summary>A shipping package delivered for an order (immutable once created).</summary>
public class OrderPackage
{
    /// <summary>(Immutable) the maximum length of the description.</summary>
    public const int DescriptionMaxLength = 500;

    /// <summary>Gets or sets the identifier.</summary>
    /// <value>The identifier.</value>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the identifier of the order.</summary>
    /// <value>The identifier of the order.</value>
    public Guid OrderId { get; set; }

    /// <summary>Gets or sets the delivery date of the package.</summary>
    /// <value>The delivery date.</value>
    public DateTime DeliveryDate { get; set; }

    /// <summary>Gets or sets the shipping amount paid for the package, in the order currency.</summary>
    /// <value>The amount.</value>
    public decimal Amount { get; set; }

    /// <summary>Gets or sets the identifier of the currency (always the order currency).</summary>
    /// <value>The identifier of the currency.</value>
    public Guid CurrencyId { get; set; }

    /// <summary>Gets or sets the optional description.</summary>
    /// <value>The description.</value>
    public string? Description { get; set; }

    /// <summary>Gets or sets the Date/Time of the created at (UTC).</summary>
    /// <value>The created at.</value>
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets the order.</summary>
    /// <value>The order.</value>
    public Order Order { get; set; } = null!;
}
