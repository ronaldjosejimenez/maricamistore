namespace MariCamiStore.Model;

/// <summary>An order.</summary>
public partial class Order
{
    /// <summary>Gets or sets the identifier.</summary>
    /// <value>The identifier.</value>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the name of the order.</summary>
    /// <value>The name of the order.</value>
    public string NameOfOrder { get; set; } = string.Empty;

    /// <summary>Gets or sets the identifier of the organization.</summary>
    /// <value>The identifier of the organization.</value>
    public Guid OrganizationId { get; set; }

    /// <summary>Gets or sets the identifier of the supplier.</summary>
    /// <value>The identifier of the supplier.</value>
    public Guid SupplierId { get; set; }

    /// <summary>Gets or sets the exchange rate.</summary>
    /// <value>The exchange rate.</value>
    public decimal ExchangeRate { get; set; }

    /// <summary>Gets or sets the tax percentage.</summary>
    /// <value>The tax percentage.</value>
    public decimal TaxPercentage { get; set; }

    /// <summary>Gets or sets the shipping amount.</summary>
    /// <value>The shipping amount.</value>
    public decimal ShippingAmountToCR { get; set; }

    /// <summary>Gets or sets the shipping amount intern.</summary>
    /// <value>The shipping amount intern.</value>
    public decimal ShippingAmountIntern { get; set; }

    /// <summary>Gets or sets the discount amount.</summary>
    /// <value>The discount amount.</value>
    public decimal DiscountAmount { get; set; }

    /// <summary>Gets or sets the total number of without taxes.</summary>
    /// <value>The total number of without taxes.</value>
    public decimal TotalWithoutTaxes { get; set; }

    /// <summary>Gets or sets the taxes amount.</summary>
    /// <value>The taxes amount.</value>
    public decimal TaxesAmount { get; set; }

    /// <summary>Gets or sets the total number of to pay to supplier.</summary>
    /// <value>The total number of to pay to supplier.</value>
    public decimal TotalToPayToSupplier { get; set; }

    /// <summary>Gets or sets the total number of the order.</summary>
    /// <value>The total number of the order.</value>
    public decimal TotalOfTheOrder { get; set; }

    /// <summary>Gets or sets the estimated profit in col.</summary>
    /// <value>The estimated profit in col.</value>
    public decimal EstimatedProfitInLocal { get; set; }

    /// <summary>Gets or sets the Date/Time of the created at.</summary>
    /// <value>The created at.</value>
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets the Date/Time of the updated at.</summary>
    /// <value>The updated at.</value>
    public DateTime UpdatedAt { get; set; }

    /// <summary>Gets or sets the total agreed price in local currency.</summary>
    /// <value>The total agreed price in local currency.</value>
    public decimal TotalAgreedPriceInLocal { get; set; }

    /// <summary>Gets or sets the status.</summary>
    /// <value>The status.</value>
    public string Status { get; set; } = string.Empty;

    /// <summary>Gets or sets the identifier of the currency.</summary>
    /// <value>The identifier of the currency.</value>
    public Guid CurrencyId { get; set; }

    public decimal ActualShippingAmountToCR { get; set; }
}
