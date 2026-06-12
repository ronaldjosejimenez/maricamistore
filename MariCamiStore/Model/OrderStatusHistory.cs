namespace MariCamiStore.Model;

public class OrderStatusHistory
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string FromStatus { get; set; } = string.Empty;
    public string ToStatus { get; set; } = string.Empty;
    public DateTime TransitionDate { get; set; }
    public string? Notes { get; set; }
    public string? Justification { get; set; }
    public DateTime CreatedAt { get; set; }
}
