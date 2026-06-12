using MariCamiStore.Model;

namespace MariCamiStore.Services
{
    public interface IConfigurationManagmentService
    {
        Task<List<Currency>> GetCurrenciesAsync();
    }
}