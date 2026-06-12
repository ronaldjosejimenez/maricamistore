namespace MariCamiStore.Model
{
    /// <summary>A product type.</summary>
    public class ProductType
    {
        /// <summary>Gets or sets the identifier.</summary>
        /// <value>The identifier.</value>
        public Guid Id { get; set; }

        /// <summary>Gets or sets the name.</summary>
        /// <value>The name.</value>
        public string Name { get; set; } = string.Empty;

        /// <summary>Gets or sets the description.</summary>
        /// <value>The description.</value>
        public string? Description { get; set; }

        /// <summary>Gets or sets the estimate shipping.</summary>
        /// <value>The estimate shipping.</value>
        public decimal EstimateShipping { get; set; }

        /// <summary>Gets or sets the service fee in col.</summary>
        /// <value>The service fee in col.</value>
        public decimal ServiceFeeInLocal { get; set; }

        /// <summary>Gets or sets the identifier of the currency.</summary>
        /// <value>The identifier of the currency.</value>
        public Guid CurrencyId { get; set; }
    }
}
