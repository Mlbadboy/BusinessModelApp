using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Workforce;

namespace BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Workforce
{
    public interface IGovernedCrmService
    {
        Task<bool> SubmitGovernedMutationAsync(string tenantId, GovernedCrmMutation mutation);
        Task<IReadOnlyList<GovernedCrmMutation>> ListMutationsAsync(string tenantId);
    }

    public interface IAccountGraphService
    {
        Task<AccountGraph> SaveAccountGraphAsync(string tenantId, AccountGraph graph);
        Task<AccountGraph?> GetAccountGraphAsync(string tenantId, string accountId);
        Task<IReadOnlyList<AccountGraph>> ListAccountGraphsAsync(string tenantId);
    }

    public interface ICommercialLineageService
    {
        Task<CommercialLineageNode> AppendLineageNodeAsync(string tenantId, CommercialLineageNode node);
        Task<IReadOnlyList<CommercialLineageNode>> GetLineageChainAsync(string tenantId, string opportunityId);
        Task<bool> VerifyLineageIntegrityAsync(string tenantId, string opportunityId);
        Task<ImmutableExternalEffect> RecordExternalEffectAsync(string tenantId, ImmutableExternalEffect effect);
        Task<ImmutableExternalEffect?> GetExternalEffectAsync(string tenantId, string effectId);
    }
}
