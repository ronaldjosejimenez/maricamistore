namespace MariCamiStore.Helpers;

/// <summary>
/// Pure, isolated algorithm that proposes how a monthly sales goal is spread over the days of the month.
/// Each day's weight comes from a reference table indexed by (day of week, n-th occurrence of that weekday in
/// the month), taken from the October 2026 budget sheet, so that month reproduces the sheet exactly. Amounts are
/// rounded to blocks of <see cref="Unit"/> using the largest remainder method; the residue (less than one block,
/// or cents) is added to the heaviest day so the sum is exactly the goal.
/// </summary>
public static class DailyGoalDistributor
{
    // ---- Tunable constants ------------------------------------------------------------------
    /// <summary>Rounding block for proposed amounts.</summary>
    public const decimal Unit = 1000m;

    /// <summary>
    /// Reference weights (relative to a total of 1000 for a 31-day month), indexed by <see cref="DayOfWeek"/> and then by
    /// occurrence number (1st..5th) of that weekday in the month. Values for 1st-4th (and 5th of Thu/Fri/Sat) come from
    /// the October 2026 sheet. The 5th occurrence of Sun/Mon/Tue/Wed was not in the sample, so it repeats the 4th.
    /// </summary>
    private static readonly decimal[][] OccurrenceWeights =
    [
        [39m, 32m, 38m, 35m, 35m], // Sunday / Domingo
        [20m, 18m, 18m, 19m, 19m], // Monday / Lunes
        [21m, 22m, 22m, 24m, 24m], // Tuesday / Martes
        [25m, 25m, 26m, 28m, 28m], // Wednesday / Miercoles
        [28m, 27m, 31m, 30m, 32m], // Thursday / Jueves
        [37m, 34m, 35m, 37m, 38m], // Friday / Viernes
        [52m, 48m, 53m, 50m, 56m], // Saturday / Sabado
    ];
    // -----------------------------------------------------------------------------------------

    /// <summary>Distributes <paramref name="goal"/> across the days of the month. Index 0 is day 1.</summary>
    public static decimal[] Distribute(int year, int month, decimal goal)
    {
        var days = DateTime.DaysInMonth(year, month);
        var result = new decimal[days];
        if (goal <= 0m)
            return result;

        var weights = new decimal[days];
        decimal total = 0m;
        for (var i = 0; i < days; i++)
        {
            var occurrence = i / 7; // 0-based: day 1-7 => 1st occurrence of its weekday, 8-14 => 2nd, ...
            var weekday = (int)new DateTime(year, month, i + 1).DayOfWeek;
            weights[i] = OccurrenceWeights[weekday][occurrence];
            total += weights[i];
        }

        var heaviest = 0;
        for (var i = 1; i < days; i++)
            if (weights[i] > weights[heaviest])
                heaviest = i;

        var totalBlocks = Math.Floor(goal / Unit);
        var blocks = new decimal[days];
        var remainders = new decimal[days];
        decimal assigned = 0m;
        for (var i = 0; i < days; i++)
        {
            var ideal = totalBlocks * weights[i] / total;
            blocks[i] = Math.Floor(ideal);
            remainders[i] = ideal - blocks[i];
            assigned += blocks[i];
        }

        // Largest remainder: hand out the leftover blocks.
        var leftover = (int)(totalBlocks - assigned);
        var order = Enumerable.Range(0, days)
            .OrderByDescending(i => remainders[i])
            .ThenByDescending(i => weights[i])
            .ToArray();
        for (var k = 0; k < leftover; k++)
            blocks[order[k % days]] += 1m;

        for (var i = 0; i < days; i++)
            result[i] = blocks[i] * Unit;

        // Residue (goal not multiple of Unit) goes to the heaviest day so the sum is exact.
        result[heaviest] += goal - result.Sum();
        return result;
    }
}
