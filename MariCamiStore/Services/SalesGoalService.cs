using MariCamiStore.Helpers;
using MariCamiStore.Infrastructure.Persistance;
using MariCamiStore.Model;
using Microsoft.EntityFrameworkCore;

namespace MariCamiStore.Services;

public class SalesGoalService(
    MariCamiStoreContext context,
    ICurrentOrganizationService currentOrg) : ISalesGoalService
{
    private const decimal FallbackMonthlyGoal = 4000000m;
    private const decimal RedBelow = 70m;
    private const decimal GreenAbove = 90m;

    private static readonly string[] Weekdays =
        ["domingo", "lunes", "martes", "miércoles", "jueves", "viernes", "sábado"];

    // ── Get or create (current month, CR time) ───────────────────────────────

    public async Task<GoalDto> GetOrCreateCurrentAsync(Guid salespersonId)
    {
        var today = CostaRicaClock.Today();

        var existing = await LoadGoalAsync(salespersonId, today.Year, today.Month);
        if (existing != null)
            return BuildDto(existing, today);

        var salesperson = await context.Salespeople.FirstOrDefaultAsync(s => s.Id == salespersonId)
            ?? throw new SalesGoalException("Vendedor no encontrado.");
        if (!salesperson.IsActive)
            throw new SalesGoalException("El vendedor está inactivo.");

        var config = await context.Configurations.FirstOrDefaultAsync();
        var goalAmount = config is { DefaultMonthlyGoal: > 0 } ? config.DefaultMonthlyGoal : FallbackMonthlyGoal;
        var currencyId = config?.LocalCurrencyId ?? Guid.Empty;
        if (currencyId == Guid.Empty)
        {
            currencyId = await context.Currencies.OrderBy(c => c.Name).Select(c => c.Id).FirstOrDefaultAsync();
            if (currencyId == Guid.Empty)
                throw new SalesGoalException("No hay monedas configuradas.");
        }

        var proposals = DailyGoalDistributor.Distribute(today.Year, today.Month, goalAmount);
        var goal = new SalesGoal
        {
            Id = Guid.NewGuid(),
            SalespersonId = salespersonId,
            OrganizationId = currentOrg.OrganizationId,
            Month = today.Month,
            Year = today.Year,
            GoalAmount = goalAmount,
            ActualAmount = 0,
            CompliancePercentage = 0,
            CurrencyId = currencyId,
        };
        for (var i = 0; i < proposals.Length; i++)
        {
            goal.Days.Add(new SalesGoalDay
            {
                Id = Guid.NewGuid(),
                SalesGoalId = goal.Id,
                DayOfMonth = i + 1,
                ProposedAmount = proposals[i],
                GoalAmount = proposals[i],
                ActualAmount = 0,
                CompliancePercentage = 0,
            });
        }

        try
        {
            context.SalesGoals.Add(goal);
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Concurrent creation (unique index): discard ours and read the winner.
            context.ChangeTracker.Clear();
            var winner = await LoadGoalAsync(salespersonId, today.Year, today.Month);
            if (winner == null)
                throw;
            return BuildDto(winner, today);
        }

        var created = await LoadGoalAsync(salespersonId, today.Year, today.Month);
        return BuildDto(created!, today);
    }

    // ── Updates ──────────────────────────────────────────────────────────────

    public async Task<GoalDto> UpdateDayAsync(UpdateDayRequest request)
    {
        var goal = await context.SalesGoals
            .Include(g => g.Currency)
            .Include(g => g.Days)
            .FirstOrDefaultAsync(g => g.Days.Any(d => d.Id == request.DayId))
            ?? throw new SalesGoalException("El día indicado no existe.");
        var day = goal.Days.First(d => d.Id == request.DayId);

        var today = CostaRicaClock.Today();
        EnsureCurrentMonth(goal, today);

        if (request.GoalAmount is < 0 || request.ActualAmount is < 0)
            throw new SalesGoalException("Los montos no pueden ser negativos.");

        if (request.GoalAmount.HasValue)
            day.GoalAmount = Math.Round(request.GoalAmount.Value, 2);
        if (request.ActualAmount.HasValue)
            day.ActualAmount = Math.Round(request.ActualAmount.Value, 2);

        Recompute(goal);
        await context.SaveChangesAsync();
        return BuildDto(goal, today);
    }

    public async Task<GoalDto> UpdateHeaderAsync(UpdateHeaderRequest request)
    {
        var goal = await context.SalesGoals
            .Include(g => g.Currency)
            .Include(g => g.Days)
            .FirstOrDefaultAsync(g => g.Id == request.GoalId)
            ?? throw new SalesGoalException("La meta indicada no existe.");

        var today = CostaRicaClock.Today();
        EnsureCurrentMonth(goal, today);

        if (request.GoalAmount <= 0)
            throw new SalesGoalException("La meta debe ser mayor a cero.");

        var currency = await context.Currencies.FirstOrDefaultAsync(c => c.Id == request.CurrencyId)
            ?? throw new SalesGoalException("La moneda indicada no existe.");

        var newGoal = Math.Round(request.GoalAmount, 2);
        var proposals = DailyGoalDistributor.Distribute(goal.Year, goal.Month, newGoal);
        foreach (var day in goal.Days)
        {
            var newProposal = proposals[day.DayOfMonth - 1];
            // Only days that still followed the old proposal follow the new one.
            if (day.GoalAmount == day.ProposedAmount)
                day.GoalAmount = newProposal;
            day.ProposedAmount = newProposal;
        }

        goal.GoalAmount = newGoal;
        goal.CurrencyId = currency.Id;
        goal.Currency = currency;

        Recompute(goal);
        await context.SaveChangesAsync();
        return BuildDto(goal, today);
    }

    // ── History ──────────────────────────────────────────────────────────────

    public async Task<List<HistoryRowDto>> GetHistoryAsync(Guid salespersonId)
    {
        var rows = await context.SalesGoals
            .Where(g => g.SalespersonId == salespersonId)
            .OrderByDescending(g => g.Year).ThenByDescending(g => g.Month)
            .Select(g => new HistoryRowDto(
                g.Id, g.Month, g.Year, g.GoalAmount, g.ActualAmount, g.CompliancePercentage, g.Currency!.Sign))
            .ToListAsync();
        return rows;
    }

    public async Task<GoalDto> GetDetailAsync(Guid goalId)
    {
        var goal = await context.SalesGoals
            .AsNoTracking()
            .Include(g => g.Currency)
            .Include(g => g.Days)
            .FirstOrDefaultAsync(g => g.Id == goalId)
            ?? throw new SalesGoalException("La meta indicada no existe.");
        return BuildDto(goal, CostaRicaClock.Today());
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private Task<SalesGoal?> LoadGoalAsync(Guid salespersonId, int year, int month) =>
        context.SalesGoals
            .Include(g => g.Currency)
            .Include(g => g.Days)
            .FirstOrDefaultAsync(g => g.SalespersonId == salespersonId && g.Year == year && g.Month == month);

    private static void EnsureCurrentMonth(SalesGoal goal, DateOnly today)
    {
        if (goal.Year != today.Year || goal.Month != today.Month)
            throw new SalesGoalException("Solo se puede editar la meta del mes actual.");
    }

    private static decimal Percentage(decimal actual, decimal goal) =>
        goal == 0 ? 0m : Math.Round(actual / goal * 100m, 1, MidpointRounding.AwayFromZero);

    private static void Recompute(SalesGoal goal)
    {
        foreach (var day in goal.Days)
            day.CompliancePercentage = Percentage(day.ActualAmount, day.GoalAmount);
        goal.ActualAmount = goal.Days.Sum(d => d.ActualAmount);
        goal.CompliancePercentage = Percentage(goal.ActualAmount, goal.GoalAmount);
    }

    private static GoalDto BuildDto(SalesGoal goal, DateOnly today)
    {
        var isCurrent = goal.Year == today.Year && goal.Month == today.Month;

        var todayPercentage = 0m;
        var status = "none";
        if (isCurrent)
        {
            var upToToday = goal.Days.Where(d => d.DayOfMonth <= today.Day).ToList();
            todayPercentage = Percentage(upToToday.Sum(d => d.ActualAmount), upToToday.Sum(d => d.GoalAmount));
            status = todayPercentage < RedBelow ? "red"
                : todayPercentage > GreenAbove ? "green"
                : "yellow";
        }

        var days = goal.Days
            .OrderBy(d => d.DayOfMonth)
            .Select(d =>
            {
                var date = new DateTime(goal.Year, goal.Month, d.DayOfMonth);
                return new GoalDayDto(
                    d.Id,
                    d.DayOfMonth,
                    Weekdays[(int)date.DayOfWeek],
                    date.ToString("dd/MM/yyyy"),
                    d.ProposedAmount,
                    d.GoalAmount,
                    d.ActualAmount,
                    d.CompliancePercentage);
            })
            .ToList();

        return new GoalDto(
            goal.Id,
            goal.SalespersonId,
            goal.Month,
            goal.Year,
            goal.GoalAmount,
            goal.ActualAmount,
            goal.CompliancePercentage,
            goal.CurrencyId,
            goal.Currency?.Sign ?? string.Empty,
            todayPercentage,
            status,
            isCurrent,
            days);
    }
}
