using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Commercial;

namespace BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Commercial
{
    public interface IProposalAndDealStore
    {
        Task SaveProposalAsync(CommercialProposal proposal);
        Task<CommercialProposal?> GetProposalAsync(string tenantId, string proposalId);
        Task<IReadOnlyList<CommercialProposal>> ListProposalsAsync(string tenantId);
        Task SaveContractAsync(AuthoritativeDealContract contract);
        Task<AuthoritativeDealContract?> GetContractAsync(string tenantId, string contractId);
        Task<IReadOnlyList<AuthoritativeDealContract>> ListContractsAsync(string tenantId);
    }

    public interface IProposalAndDealService
    {
        Task<CommercialProposal> CreateProposalAsync(CommercialProposal proposal);
        Task<bool> ApproveProposalForSubmissionAsync(string tenantId, string proposalId, string humanSignoffId);
        Task<NegotiationAnalysis> AnalyzeNegotiationCounterOfferAsync(string tenantId, string proposalId, decimal customerOfferedPriceINR);
        Task<AuthoritativeDealContract> RecordAuthoritativeContractAsync(AuthoritativeDealContract contract);
        Task<bool> VerifyContractIntegrityAsync(string tenantId, string contractId);
    }
}
