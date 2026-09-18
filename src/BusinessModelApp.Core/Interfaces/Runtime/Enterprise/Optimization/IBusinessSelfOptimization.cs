using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Optimization;

namespace BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Optimization
{
    public interface IBusinessSelfOptimizationStore
    {
        Task SaveProposalAsync(AdaptationProposal proposal, CancellationToken cancellationToken = default);
        Task<AdaptationProposal?> GetProposalAsync(string proposalId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<AdaptationProposal>> ListProposalsForTenantAsync(string tenantId, CancellationToken cancellationToken = default);
    }

    public interface IBusinessSelfOptimizationService
    {
        Task<AdaptationProposal> ProposeAdaptationAsync(
            string tenantId,
            OptimizationDomain domain,
            string targetParameter,
            decimal currentValue,
            decimal proposedValue,
            decimal causalConfidenceScore,
            int empiricalObservationsCount,
            decimal safetyCeilingMin,
            decimal safetyCeilingMax,
            CancellationToken cancellationToken = default);

        Task<AdaptationProposal> EvaluateAndCertifyAsync(
            string proposalId,
            decimal minConfidenceThreshold = 0.80m,
            int minObservationsRequired = 50,
            CancellationToken cancellationToken = default);

        Task<AdaptationProposal> ApplyAdaptationAsync(
            string proposalId,
            CancellationToken cancellationToken = default);
    }
}
