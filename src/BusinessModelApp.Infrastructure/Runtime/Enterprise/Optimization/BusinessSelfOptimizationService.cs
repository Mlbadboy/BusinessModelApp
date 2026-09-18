using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Optimization;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Optimization;

namespace BusinessModelApp.Infrastructure.Runtime.Enterprise.Optimization
{
    public sealed class InMemoryBusinessSelfOptimizationStore : IBusinessSelfOptimizationStore
    {
        private readonly ConcurrentDictionary<string, AdaptationProposal> _proposals = new();

        public Task SaveProposalAsync(AdaptationProposal proposal, CancellationToken cancellationToken = default)
        {
            _proposals[proposal.ProposalId] = proposal;
            return Task.CompletedTask;
        }

        public Task<AdaptationProposal?> GetProposalAsync(string proposalId, CancellationToken cancellationToken = default)
        {
            _proposals.TryGetValue(proposalId, out var prop);
            return Task.FromResult(prop);
        }

        public Task<IReadOnlyList<AdaptationProposal>> ListProposalsForTenantAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            var list = _proposals.Values.Where(p => p.TenantId == tenantId).ToList();
            return Task.FromResult<IReadOnlyList<AdaptationProposal>>(list);
        }
    }

    public sealed class BusinessSelfOptimizationService : IBusinessSelfOptimizationService
    {
        private readonly IBusinessSelfOptimizationStore _store;

        public BusinessSelfOptimizationService(IBusinessSelfOptimizationStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public async Task<AdaptationProposal> ProposeAdaptationAsync(
            string tenantId,
            OptimizationDomain domain,
            string targetParameter,
            decimal currentValue,
            decimal proposedValue,
            decimal causalConfidenceScore,
            int empiricalObservationsCount,
            decimal safetyCeilingMin,
            decimal safetyCeilingMax,
            CancellationToken cancellationToken = default)
        {
            if (safetyCeilingMin > safetyCeilingMax)
                throw new ArgumentException("Safety ceiling min cannot exceed max.");

            var proposal = new AdaptationProposal
            {
                TenantId = tenantId,
                Domain = domain,
                TargetParameterName = targetParameter,
                CurrentValue = currentValue,
                ProposedValue = proposedValue,
                CausalConfidenceScore = Math.Clamp(causalConfidenceScore, 0m, 1m),
                EmpiricalObservationsCount = Math.Max(0, empiricalObservationsCount),
                SafetyCeilingMin = safetyCeilingMin,
                SafetyCeilingMax = safetyCeilingMax
            };

            await _store.SaveProposalAsync(proposal, cancellationToken);
            return proposal;
        }

        public async Task<AdaptationProposal> EvaluateAndCertifyAsync(
            string proposalId,
            decimal minConfidenceThreshold = 0.80m,
            int minObservationsRequired = 50,
            CancellationToken cancellationToken = default)
        {
            var proposal = await _store.GetProposalAsync(proposalId, cancellationToken);
            if (proposal == null) throw new KeyNotFoundException($"Adaptation proposal '{proposalId}' not found.");

            proposal.EvaluateSafety(minConfidenceThreshold, minObservationsRequired);
            await _store.SaveProposalAsync(proposal, cancellationToken);
            return proposal;
        }

        public async Task<AdaptationProposal> ApplyAdaptationAsync(
            string proposalId,
            CancellationToken cancellationToken = default)
        {
            var proposal = await _store.GetProposalAsync(proposalId, cancellationToken);
            if (proposal == null) throw new KeyNotFoundException($"Adaptation proposal '{proposalId}' not found.");

            proposal.Apply();
            await _store.SaveProposalAsync(proposal, cancellationToken);
            return proposal;
        }
    }
}
