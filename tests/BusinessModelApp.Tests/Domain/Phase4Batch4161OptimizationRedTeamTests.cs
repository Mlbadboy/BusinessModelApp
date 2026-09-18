using System;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Optimization;
using BusinessModelApp.Infrastructure.Runtime.Enterprise.Optimization;
using Xunit;

namespace BusinessModelApp.Tests.Domain
{
    /// <summary>
    /// Phase 4.16 Batch 4161: Business Self-Optimization Red Team Tests (OPT01 - OPT04).
    /// </summary>
    public class Phase4Batch4161OptimizationRedTeamTests
    {
        [Fact]
        public async Task OPT01_ExceedingSafetyCeilingBounds_FailsClosedWithRejectedUnsafe()
        {
            var store = new InMemoryBusinessSelfOptimizationStore();
            var service = new BusinessSelfOptimizationService(store);

            // Propose 5.0m (exceeds safety ceiling max 2.0m)
            var proposal = await service.ProposeAdaptationAsync(
                "tenant-alpha",
                OptimizationDomain.ResourceAllocationWeight,
                "MarketingBudgetMultiplier",
                currentValue: 1.0m,
                proposedValue: 5.0m, // UNSAFE!
                causalConfidenceScore: 0.99m,
                empiricalObservationsCount: 200,
                safetyCeilingMin: 0.50m,
                safetyCeilingMax: 2.00m);

            var evaluated = await service.EvaluateAndCertifyAsync(proposal.ProposalId);

            Assert.Equal(AdaptationStatus.RejectedUnsafe, evaluated.Status);
            Assert.Contains("violates constitutional safety ceiling bounds", evaluated.EvaluationReason);
        }

        [Fact]
        public async Task OPT02_LowEmpiricalConfidence_FailsClosedWithRejectedLowConfidence()
        {
            var store = new InMemoryBusinessSelfOptimizationStore();
            var service = new BusinessSelfOptimizationService(store);

            // Low confidence 0.40m (< 0.80m threshold)
            var proposal = await service.ProposeAdaptationAsync(
                "tenant-alpha",
                OptimizationDomain.ModelProviderSelection,
                "ModelRoutingPreference",
                currentValue: 1.0m,
                proposedValue: 1.2m,
                causalConfidenceScore: 0.40m, // Low confidence
                empiricalObservationsCount: 15, // Low observations
                safetyCeilingMin: 0.50m,
                safetyCeilingMax: 2.00m);

            var evaluated = await service.EvaluateAndCertifyAsync(proposal.ProposalId);

            Assert.Equal(AdaptationStatus.RejectedLowConfidence, evaluated.Status);
            Assert.Contains("insufficient", evaluated.EvaluationReason);
        }

        [Fact]
        public async Task OPT03_ApplyingUncertifiedAdaptation_ThrowsInvalidOperation()
        {
            var store = new InMemoryBusinessSelfOptimizationStore();
            var service = new BusinessSelfOptimizationService(store);

            var proposal = await service.ProposeAdaptationAsync(
                "tenant-alpha",
                OptimizationDomain.OutreachScheduleTiming,
                "OutreachIntervalHours",
                currentValue: 24m,
                proposedValue: 36m,
                causalConfidenceScore: 0.90m,
                empiricalObservationsCount: 100,
                safetyCeilingMin: 12m,
                safetyCeilingMax: 72m);

            // Attempting to apply directly while still PendingVerification
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.ApplyAdaptationAsync(proposal.ProposalId));
        }

        [Fact]
        public async Task OPT04_InvalidSafetyCeilingMinGreaterThanMax_ThrowsArgumentException()
        {
            var store = new InMemoryBusinessSelfOptimizationStore();
            var service = new BusinessSelfOptimizationService(store);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.ProposeAdaptationAsync("tenant-alpha", OptimizationDomain.AgentRouting, "Param", 1m, 2m, 0.9m, 100, safetyCeilingMin: 5m, safetyCeilingMax: 1m));
        }
    }
}
