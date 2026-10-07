namespace MariCamiStore.Model;

/// <summary>Monthly sales goal of a salesperson (master record).</summary>
public class SalesGoal
{
    public Guid Id { get; set; }

    public Guid SalespersonId { get; set; }

    public Guid OrganizationId { get; set; }

    public int Month { get; set; }

    public int Year { get; set; }

    public decimal GoalAmount { get; set; }

    public decimal ActualAmount { get; set; }

    public decimal CompliancePercentage { get; set; }

    public Guid CurrencyId { get; set; }

    public Salesperson? Salesperson { get; set; }

    public Currency? Currency { get; set; }

    public List<SalesGoalDay> Days { get; set; } = [];
}
