using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Commercial;

namespace BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Commercial
{
    public interface ISalesIntelligenceStore
    {
        Task SaveStrategyAsync(CommercialAccountStrategy strategy);
        Task<CommercialAccountStrategy?> GetStrategyAsync(string tenantId, string strategyId);
        Task<IReadOnlyList<CommercialAccountStrategy>> ListStrategiesAsync(string tenantId);
    }

    public interface ISalesIntelligenceService
    {
        Task<CommercialAccountStrategy> FormulateAccountStrategyAsync(string tenantId, string accountId, string opportunityId, string agentId);
        Task<CommercialAccountStrategy?> GetAccountStrategyAsync(string tenantId, string strategyId);
    }
}
