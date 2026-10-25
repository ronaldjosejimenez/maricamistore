namespace MariCamiStore.Model
{
    /// <summary>A supplier.</summary>
    public class Supplier
    {
        /// <summary>Gets or sets the identifier.</summary>
        /// <value>The identifier.</value>
        public Guid Id { get; set; }

        /// <summary>Gets or sets the name.</summary>
        /// <value>The name.</value>
        public string Name { get; set; } = string.Empty;

        /// <summary>Gets or sets the default tax percentage.</summary>
        /// <value>The default tax percentage.</value>
        public decimal TaxPercentageDefault { get; set; }

        /// <summary>Gets or sets the default currency identifier.</summary>
        /// <value>The default currency identifier.</value>
        public Guid? CurrencyIdDefault { get; set; }

        /// <summary>Gets or sets the default exchange rate.</summary>
        /// <value>The default exchange rate.</value>
        public decimal ExchangeRateDefault { get; set; }

        /// <summary>Gets or sets the default shipping cost, in the default currency.</summary>
        /// <value>The default shipping cost.</value>
        public decimal ShippingCostDefault { get; set; }

        /// <summary>Gets or sets a value indicating whether the supplier is listed in the calculator.</summary>
        /// <value>True if the supplier is active for the calculator.</value>
        public bool IsActiveForCalculator { get; set; }
    }
}
