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

    /// <summary>(Immutable) negative entry created when a shipping package is deleted.</summary>
    public const string ReversoPaquete = "ReversoPaquete";
}
