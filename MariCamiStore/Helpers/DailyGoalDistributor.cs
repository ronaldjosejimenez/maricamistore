namespace MariCamiStore.Helpers;

/// <summary>
/// Pure, isolated algorithm that proposes how a monthly sales goal is spread over the days of the month.
/// Fixed weekday profile x linear weekly ramp, rounded to blocks of <see cref="Unit"/> using the largest
/// remainder method; the residue (less than one block, or cents) is added to the heaviest day so the
/// sum is exactly the goal.
/// </summary>
public static class DailyGoalDistributor
{
    // ---- Tunable constants ------------------------------------------------------------------
    /// <summary>Rounding block for proposed amounts.</summary>
    public const decimal Unit = 1000m;

    /// <summary>Ramp factor applied to the first day of the month.</summary>
    public const decimal RampStart = 0.94m;

    /// <summary>Ramp factor applied to the last day of the month.</summary>
    public const decimal RampEnd = 1.06m;

    /// <summary>Base weekday weights (average observed in the Excel), indexed by <see cref="DayOfWeek"/>.</summary>
    private static readonly decimal[] WeekdayWeights =
    [
        36m, // Sunday / Domingo
        19m, // Monday / Lunes
        22m, // Tuesday / Martes
        26m, // Wednesday / Miercoles
        29m, // Thursday / Jueves
        36m, // Friday / Viernes
        51m, // Saturday / Sabado
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
            var date = new DateTime(year, month, i + 1);
            var ramp = days == 1
                ? RampStart
                : RampStart + (RampEnd - RampStart) * i / (days - 1);
            weights[i] = WeekdayWeights[(int)date.DayOfWeek] * ramp;
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
