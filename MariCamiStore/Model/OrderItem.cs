namespace MariCamiStore.Model;

/// <summary>An order item.</summary>
public class OrderItem
{
    /// <summary>Gets or sets the identifier.</summary>
    /// <value>The identifier.</value>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the identifier of the order.</summary>
    /// <value>The identifier of the order.</value>
    public Guid OrderId { get; set; }

    /// <summary>Gets or sets the identifier of the customer.</summary>
    /// <value>The identifier of the customer.</value>
    public Guid CustomerId { get; set; }

    /// <summary>Gets or sets information describing the product.</summary>
    /// <value>Information describing the product.</value>
    public string ProductDescription { get; set; } = string.Empty;

    /// <summary>Gets or sets the product link.</summary>
    /// <value>The product link.</value>
    public string ProductLink { get; set; } = string.Empty;

    /// <summary>Gets or sets the product source code.</summary>
    /// <value>The product source code.</value>
    public string ProductSourceCode { get; set; } = string.Empty;

    /// <summary>Gets or sets the image.</summary>
    /// <value>The image.</value>
    public byte[]? ProductImage { get; set; }

    /// <summary>Gets or sets the identifier of the product type.</summary>
    /// <value>The identifier of the product type.</value>
    public Guid ProductTypeId { get; set; }

    /// <summary>Gets or sets the list price.</summary>
    /// <value>The list price.</value>
    public decimal ListPrice { get; set; }

    public decimal ListPriceTaxWithTax { get; set; }

    /// <summary>Gets or sets the real price.</summary>
    /// <value>The real price.</value>
    public decimal RealPrice { get; set; }

    /// <summary>Gets or sets the estimate shipping.</summary>
    /// <value>The estimate shipping.</value>
    public decimal EstimateShipping { get; set; }

    /// <summary>Gets or sets the service fee in col.</summary>
    /// <value>The service fee in col.</value>
    public decimal ServiceFeeInLocal { get; set; }

    /// <summary>Gets or sets the agreed price in col.</summary>
    /// <value>The agreed price in col.</value>
    public decimal AgreedPriceInLocal { get; set; }

    public bool IsReceived { get; set; }

    /// <summary>Gets or sets the Date/Time of the created at.</summary>
    /// <value>The created at.</value>
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets the Date/Time of the updated at.</summary>
    /// <value>The updated at.</value>
    public DateTime UpdatedAt { get; set; }

    public Order Order { get; set; } = null!;
}
