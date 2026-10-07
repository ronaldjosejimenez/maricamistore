namespace MariCamiStore.Model;

/// <summary>Daily detail of a monthly sales goal.</summary>
public class SalesGoalDay
{
    public Guid Id { get; set; }

    public Guid SalesGoalId { get; set; }

    public int DayOfMonth { get; set; }

    public decimal ProposedAmount { get; set; }

    public decimal GoalAmount { get; set; }

    public decimal ActualAmount { get; set; }

    public decimal CompliancePercentage { get; set; }

    public SalesGoal? SalesGoal { get; set; }
}
