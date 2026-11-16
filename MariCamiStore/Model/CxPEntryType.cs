namespace MariCamiStore.Model;

/// <summary>Known values for <see cref="CxPEntry.Type"/>.</summary>
public static class CxPEntryType
{
    /// <summary>(Immutable) entry created manually by the user.</summary>
    public const string Manual = "Manual";

    /// <summary>(Immutable) entry created automatically when an order is activated.</summary>
    public const string AutoActiva = "AutoActiva";

    /// <summary>(Immutable) legacy entry created when an order was delivered (rule retired; kept for history).</summary>
    public const string AutoDelivered = "AutoDelivered";

    /// <summary>(Immutable) balance carried over from the previous period.</summary>
    public const string SaldoAnterior = "SaldoAnterior";

    /// <summary>(Immutable) entry created automatically when a shipping package is added to an order.</summary>
    public const string AutoPaquete = "AutoPaquete";

    /// <summary>(Immutable) negative entry created when a shipping package is deleted. Unlike <see cref="AnulacionItems"/>, it is stored with a NEGATIVE amount.</summary>
    public const string ReversoPaquete = "ReversoPaquete";

    /// <summary>Items voided from an active order. Stored with a positive amount; the type is what subtracts.</summary>
    public const string AnulacionItems = "AnulacionItems";

    /// <summary>Gets the amount with the sign this entry type has when added up (negative for the types that subtract).</summary>
    /// <param name="type">The entry type.</param>
    /// <param name="amount">The stored amount.</param>
    /// <returns>The amount to add to the totals.</returns>
    public static decimal SignedAmount(string type, decimal amount) =>
        type == AnulacionItems ? -amount : amount;
}
