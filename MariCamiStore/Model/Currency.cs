namespace MariCamiStore.Model
{
    /// <summary>A currency.</summary>
    public class Currency
    {
        /// <summary>Gets or sets the identifier.</summary>
        /// <value>The identifier.</value>
        public Guid Id { get; set; }

        /// <summary>Gets or sets the name.</summary>
        /// <value>The name.</value>
        public string Name { get; set; } = string.Empty;

        /// <summary>Gets or sets the abbreviation.</summary>
        /// <value>The abbreviation.</value>
        public string Abbreviation { get; set; } = string.Empty;

        /// <summary>Gets or sets the currency sign (e.g. $, ₡).</summary>
        /// <value>The sign.</value>
        public string Sign { get; set; } = string.Empty;
    }
}
