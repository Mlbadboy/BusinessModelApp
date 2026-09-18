using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Workforce;

namespace BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Workforce
{
    public interface ICommercialTruthEngine
    {
        Task<CommercialFact> RecordCommercialClaimAsync(string tenantId, string opportunityId, CommercialTruthState claimedState, string description);
        Task<CommercialFact> CorroborateCommercialFactAsync(string tenantId, string opportunityId, CommercialEvidenceItem evidence);
        Task<CommercialFact?> GetCommercialFactAsync(string tenantId, string opportunityId);
        Task<IReadOnlyList<CommercialFact>> ListCommercialFactsAsync(string tenantId);
    }

    public interface IBusinessSopCompiler
    {
        Task<BusinessSopDefinition> RegisterSopDefinitionAsync(string tenantId, BusinessSopDefinition sop);
        Task<BusinessSopDefinition?> GetSopDefinitionAsync(string tenantId, string sopId);
        Task<IReadOnlyList<BusinessSopDefinition>> ListSopDefinitionsAsync(string tenantId);
        Task<CompiledWorkProposal> CompileSopToWorkProposalAsync(string tenantId, string sopId, string opportunityId, string title);
    }
}
