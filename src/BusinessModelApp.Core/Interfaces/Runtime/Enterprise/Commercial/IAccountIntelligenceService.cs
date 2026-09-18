using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Commercial;

namespace BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Commercial
{
    public interface IAccountIntelligenceStore
    {
        Task SaveAccountGraphAsync(EnterpriseAccountGraph graph);
        Task<EnterpriseAccountGraph?> GetAccountGraphAsync(string tenantId, string accountId);
        Task<IReadOnlyList<EnterpriseAccountGraph>> ListAccountGraphsAsync(string tenantId);
    }

    public interface IAccountIntelligenceService
    {
        Task<EnterpriseAccountGraph> UpsertAccountGraphAsync(EnterpriseAccountGraph graph);
        Task<EnterpriseAccountGraph?> GetAccountGraphAsync(string tenantId, string accountId);
        Task<BuyingCenterContact> AddBuyingCenterContactAsync(string tenantId, string accountId, BuyingCenterContact contact);
        Task<IReadOnlyList<EnterpriseAccountGraph>> ListAccountsAsync(string tenantId);
    }
}
