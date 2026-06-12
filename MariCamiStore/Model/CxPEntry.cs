namespace MariCamiStore.Model;

public class CxPEntry
{
    public Guid Id { get; set; }
    public Guid PeriodControlId { get; set; }
    public Guid CurrencyId { get; set; }
    public decimal Amount { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public Guid? OrderId { get; set; }
    public DateTime CreatedAt { get; set; }

    public PeriodControl? Period { get; set; }
    public Currency? Currency { get; set; }
}
