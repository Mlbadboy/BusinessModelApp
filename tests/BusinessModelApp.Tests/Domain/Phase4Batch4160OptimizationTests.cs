using System;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Optimization;
using BusinessModelApp.Infrastructure.Runtime.Enterprise.Optimization;
using Xunit;

namespace BusinessModelApp.Tests.Domain
{
    /// <summary>
    /// Phase 4.16 Batch 4160: Business Self-Optimization Domain Tests.
    /// </summary>
    public class Phase4Batch4160OptimizationTests
    {
        [Fact]
        public async Task VerifiedAdaptation_WithinSafetyBounds_CertifiesAndApplies()
        {
            var store = new InMemoryBusinessSelfOptimizationStore();
            var service = new BusinessSelfOptimizationService(store);

            // Propose tuning agent prompt temperature / concurrency weight
            var proposal = await service.ProposeAdaptationAsync(
                "tenant-alpha",
                OptimizationDomain.AgentRouting,
                "ReconciliationWorkerConcurrencyWeight",
                currentValue: 1.0m,
                proposedValue: 1.25m,
                causalConfidenceScore: 0.94m,
                empiricalObservationsCount: 120,
                safetyCeilingMin: 0.50m,
                safetyCeilingMax: 2.00m);

            Assert.Equal(AdaptationStatus.PendingVerification, proposal.Status);

            // Evaluate and certify
            var evaluated = await service.EvaluateAndCertifyAsync(proposal.ProposalId, minConfidenceThreshold: 0.85m, minObservationsRequired: 50);
            Assert.Equal(AdaptationStatus.CertifiedSafe, evaluated.Status);
            Assert.Contains("Certified Safe", evaluated.EvaluationReason);

            // Apply adaptation
            var applied = await service.ApplyAdaptationAsync(proposal.ProposalId);
            Assert.Equal(AdaptationStatus.Applied, applied.Status);
        }
    }
}
