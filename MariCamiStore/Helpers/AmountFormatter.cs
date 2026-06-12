using System.Globalization;

namespace MariCamiStore.Helpers;

/// <summary>Formats monetary amounts with an optional currency sign.</summary>
public static class AmountFormatter
{
    private static readonly CultureInfo EsCr = new("es-CR");

    /// <summary>
    /// Returns "{sign} {amount:N2}" when sign is non-empty,
    /// otherwise returns "{amount:N2}". Culture: es-CR.
    /// </summary>
    public static string Format(decimal amount, string sign)
    {
        var formatted = amount.ToString("N2", EsCr);
        return string.IsNullOrWhiteSpace(sign)
            ? formatted
            : $"{sign} {formatted}";
    }
}
