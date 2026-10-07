namespace MariCamiStore.Services;

public record GoalDayDto(
    Guid Id,
    int DayOfMonth,
    string Weekday,
    string Date,
    decimal ProposedAmount,
    decimal GoalAmount,
    decimal ActualAmount,
    decimal CompliancePercentage);

public record GoalDto(
    Guid Id,
    Guid SalespersonId,
    int Month,
    int Year,
    decimal GoalAmount,
    decimal ActualAmount,
    decimal CompliancePercentage,
    Guid CurrencyId,
    string CurrencySign,
    decimal TodayPercentage,
    string TodayStatus,
    bool IsCurrentMonth,
    List<GoalDayDto> Days)
{
    public bool Success => true;
}

public record HistoryRowDto(
    Guid Id,
    int Month,
    int Year,
    decimal GoalAmount,
    decimal ActualAmount,
    decimal CompliancePercentage,
    string CurrencySign);

public record UpdateHeaderRequest(Guid GoalId, decimal GoalAmount, Guid CurrencyId);

public record UpdateDayRequest(Guid DayId, decimal? GoalAmount, decimal? ActualAmount);

/// <summary>Raised for validation problems that should be shown to the user.</summary>
public class SalesGoalException(string message) : Exception(message);

public interface ISalesGoalService
{
    /// <summary>Gets (creating on demand) the current Costa Rica month goal of a salesperson.</summary>
    Task<GoalDto> GetOrCreateCurrentAsync(Guid salespersonId);

    Task<GoalDto> UpdateHeaderAsync(UpdateHeaderRequest request);

    Task<GoalDto> UpdateDayAsync(UpdateDayRequest request);

    /// <summary>History of a salesperson, newest first. Never creates records.</summary>
    Task<List<HistoryRowDto>> GetHistoryAsync(Guid salespersonId);

    /// <summary>Read-only detail of an existing goal. Never creates records.</summary>
    Task<GoalDto> GetDetailAsync(Guid goalId);
}
