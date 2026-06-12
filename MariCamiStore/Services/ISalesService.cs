using MariCamiStore.Model;

namespace MariCamiStore.Services
{
    public interface ISalesService
    {
        Task<IEnumerable<Order>> GetActiveOrdersAsync();
    }
}