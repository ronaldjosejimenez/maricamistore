namespace MariCamiStore.Model;

public class PeriodControl
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public int TransactionMonth { get; set; }
    public int TransactionYear { get; set; }
    public decimal ExchangeRate { get; set; }
    public decimal PagosRealizados { get; set; }
    public decimal EnCuenta { get; set; }
    public bool IsClosed { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<CxPEntry> Entries { get; set; } = [];
}
