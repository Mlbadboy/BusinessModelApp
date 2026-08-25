using BusinessModelApp.Infrastructure.Data.Models;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace BusinessModelApp.Infrastructure.Repositories
{
    public interface IBusinessModelRepository
    {
        Task<BusinessModel> GetBusinessModelByIdAsync(Guid id);
        Task<IEnumerable<BusinessModel>> GetAllBusinessModelsAsync();
        Task AddBusinessModelAsync(BusinessModel model);
        Task UpdateBusinessModelAsync(BusinessModel model);
        Task DeleteBusinessModelAsync(Guid id);
        Task<IEnumerable<RevenueSource>> GetRevenueSourcesForModelAsync(Guid modelId);
        Task<IEnumerable<Expense>> GetExpensesForModelAsync(Guid modelId);
    }
}
