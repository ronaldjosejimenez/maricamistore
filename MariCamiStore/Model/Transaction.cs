namespace MariCamiStore.Model;

/// <summary>A transaction.</summary>
public class Transaction
{
    /// <summary>Gets or sets the identifier.</summary>
    /// <value>The identifier.</value>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the identifier of the source.</summary>
    /// <value>The identifier of the source.</value>
    public Guid? SourceId { get; set; }

    /// <summary>Gets or sets the source for the.</summary>
    /// <value>The source.</value>
    public string Source { get; set; } = string.Empty;

    /// <summary>Gets or sets the identifier of the customer.</summary>
    /// <value>The identifier of the customer.</value>
    public Guid? CustomerId { get; set; }

    /// <summary>Gets or sets the type of the transaction.</summary>
    /// <value>The type of the transaction.</value>
    public string TransactionType { get; set; } = string.Empty;

    /// <summary>Gets or sets information describing the transaction.</summary>
    /// <value>Information describing the transaction.</value>
    public string TransactionDescription { get; set; } = string.Empty;

    /// <summary>Gets or sets the transaction amount.</summary>
    /// <value>The transaction amount.</value>
    public decimal TransactionAmount { get; set; }

    /// <summary>Gets or sets the transaction date.</summary>
    /// <value>The transaction date.</value>
    public DateTime TransactionDate { get; set; }

    /// <summary>Gets or sets the status.</summary>
    /// <value>The status.</value>
    public string Status { get; set; } = string.Empty;

    /// <summary>Gets or sets the Date/Time of the created at.</summary>
    /// <value>The created at.</value>
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets the Date/Time of the updated at.</summary>
    /// <value>The updated at.</value>
    public DateTime UpdatedAt { get; set; }

    /// <summary>Gets or sets the identifier of the currency.</summary>
    /// <value>The identifier of the currency.</value>
    public Guid CurrencyId { get; set; }

    /// <summary>Gets or sets the identifier of the organization.</summary>
    /// <value>The identifier of the organization.</value>
    public Guid OrganizationId { get; set; }
}
