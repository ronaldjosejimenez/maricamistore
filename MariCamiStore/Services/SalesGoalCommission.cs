namespace MariCamiStore.Services;

/// <summary>
/// Pure commission rules for sales goals. The base percentage must already be rounded to 1 decimal
/// (as shown on screen), so the range limits apply to the displayed value: 85.0 → 2.5%, 110.0 → 5%, 110.1 → 6%.
/// The commission is informative only; it is never persisted.
/// </summary>
public static class SalesGoalCommission
{
    /// <summary>Gets the commission rate and its display status for a base percentage.</summary>
    /// <param name="basePercentage">The base percentage, rounded to 1 decimal.</param>
    /// <returns>
    /// The commission rate (in percent) and its status: <c>red</c> (below 85), <c>yellow</c> (85 to below 86),
    /// <c>green</c> (86 to 110 inclusive) or <c>green-plus</c> (above 110).
    /// </returns>
    public static (decimal Rate, string Status) For(decimal basePercentage)
    {
        if (basePercentage < 85m) return (0m, "red");
        if (basePercentage < 86m) return (2.5m, "yellow");
        if (basePercentage <= 110m) return (5m, "green");
        return (6m, "green-plus");
    }

    /// <summary>Computes the commission amount for an actual sales amount and a commission rate.</summary>
    /// <param name="actual">The accumulated actual amount of the month.</param>
    /// <param name="rate">The commission rate, in percent.</param>
    /// <returns>The amount rounded to 2 decimals; never negative.</returns>
    public static decimal Amount(decimal actual, decimal rate) =>
        Math.Max(0m, Math.Round(actual * rate / 100m, 2, MidpointRounding.AwayFromZero));

    /// <summary>Gets the next commission level and the actual amount still missing to reach it.</summary>
    /// <param name="goal">The goal amount of the month.</param>
    /// <param name="actual">The accumulated actual amount of the month.</param>
    /// <param name="basePercentage">The base percentage, rounded to 1 decimal.</param>
    /// <returns>
    /// The next commission rate and the missing amount (rounded up to cents), or <c>null</c> for both when
    /// the highest level is already reached or there is no goal.
    /// </returns>
    public static (decimal? Rate, decimal? Missing) Next(decimal goal, decimal actual, decimal basePercentage)
    {
        // Unrounded percentage that rounds (1 decimal, away from zero) to each level's lower limit:
        // 84.95 → 85.0, 85.95 → 86.0, 110.05 → 110.1.
        (decimal Rate, decimal RawPercentage)? next = basePercentage switch
        {
            < 85m => (2.5m, 84.95m),
            < 86m => (5m, 85.95m),
            <= 110m => (6m, 110.05m),
            _ => null
        };
        if (next is null || goal <= 0m) return (null, null);

        var required = Math.Ceiling(goal * next.Value.RawPercentage) / 100m;
        return (next.Value.Rate, Math.Max(0m, required - actual));
    }
}
