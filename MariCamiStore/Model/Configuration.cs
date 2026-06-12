namespace MariCamiStore.Model;

/// <summary>A configuration.</summary>
public partial class Configuration
{
    /// <summary>Gets or sets the identifier.</summary>
    /// <value>The identifier.</value>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the identifier of the organization.</summary>
    /// <value>The identifier of the organization.</value>
    public Guid OrganizationId { get; set; }

    /// <summary>Gets or sets the exchange rate margin.</summary>
    /// <value>The exchange rate margin.</value>
    public decimal ExchangeRateMargin { get; set; }

    /// <summary>Gets or sets the exchange rate.</summary>
    /// <value>The exchange rate.</value>
    public decimal ExchangeRate { get; set; }

    /// <summary>Gets or sets the tax percentage.</summary>
    /// <value>The tax percentage.</value>
    public decimal TaxPercentage { get; set; }

    /// <summary>Gets or sets the local currency.</summary>
    /// <value>The local currency.</value>
    public Guid LocalCurrencyId { get; set; }

    /// <summary>Gets or sets the order currency identifier default.</summary>
    /// <value>The order currency identifier default.</value>
    public Guid OrderCurrencyIdDefault { get; set; }

    /// <summary>Gets or sets the product type identifier default.</summary>
    /// <value>The product type identifier default.</value>
    public Guid? ProductTypeIdDefault { get; set; }
}
