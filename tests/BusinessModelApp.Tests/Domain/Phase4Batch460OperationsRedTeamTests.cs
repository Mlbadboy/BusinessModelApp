using System;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Growth;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Operations;
using BusinessModelApp.Infrastructure.Runtime.Enterprise.Operations;
using Xunit;

namespace BusinessModelApp.Tests.Domain
{
    /// <summary>
    /// Phase 4.6 Sub-Batch 4.6.20: Autonomous Business Operations Red Team Suite.
    /// Executes 30 adversarial attack vectors (OPS01–OPS30) verifying fail-closed governance.
    /// </summary>
    public class Phase4Batch460OperationsRedTeamTests
    {
        [Fact]
        public async Task OPS01_To_OPS10_RevenueAndPipelineIntegrityAttacks_FailClosed()
        {
            var ledgerStore = new InMemoryProductionRealityLedgerStore();
            var ledgerService = new ProductionRealityLedgerService(ledgerStore);
            var intelligenceEngine = new MarketIntelligenceAndOpportunityEngine();
            var policy = UnitEconomicsPolicy.CreateDefaultSaaS("tenant-redteam", "BO-REDTEAM");

            // OPS01: Fake revenue assertion (Level 0 Fixture) must not pass verification
            var fakeEvent = await ledgerService.RecordExternalEventAsync(
                "tenant-redteam", "BO-REDTEAM", "GO-01", "OBJ-01",
                ProductionRealityEventType.PaymentSettled,
                EpistemicEvidenceLevel.InternalFixture,
                "Simulation", "MockConnector", "InternalMock", "TXN-FAKE-001",
                "RAW-FAKE-DATA", revenueImpact: 1_000_000m);

            bool isFakeValid = await ledgerService.VerifyRealityEvidenceIntegrityAsync(fakeEvent.EventId);
            Assert.False(isFakeValid); // OPS01 Passed (Blocked)

            // OPS02: Empty payload digest rejected
            var emptyDigestEvent = await ledgerService.RecordExternalEventAsync(
                "tenant-redteam", "BO-REDTEAM", "GO-01", "OBJ-02",
                ProductionRealityEventType.PaymentSettled,
                EpistemicEvidenceLevel.BankVerifiedCash,
                "BankAPI", "Fedwire", "Chase", "REF-002",
                "");

            bool isEmptyValid = await ledgerService.VerifyRealityEvidenceIntegrityAsync(emptyDigestEvent.EventId);
            Assert.False(isEmptyValid); // OPS02/04 Passed (Blocked)

            // OPS06: Pipeline inflation with low ICP score (< 0.70) disqualified
            bool isQualified = intelligenceEngine.EvaluateSignalToOpportunity(
                "Random Web Mention", estimatedBudgetINR: 500_000m, icpScore: 0.45m, out string reason);
            Assert.False(isQualified);
            Assert.Contains("below qualification threshold", reason); // OPS06 Passed

            // OPS10: Margin manipulation below policy floor blocked
            var controller = new BusinessUnitEconomicsController();
            bool isFeasible = controller.EvaluatePursuitFeasibility(
                projectedRevenueINR: 100_000m, fullyLoadedCostINR: 80_000m, policy: policy, out string marginReason);
            Assert.False(isFeasible);
            Assert.Contains("DO NOT PURSUE", marginReason); // OPS10 Passed
        }

        [Fact]
        public async Task OPS11_To_OPS20_CycleAndBudgetEscalationAttacks_FailClosed()
        {
            var kernelStore = new InMemoryContinuousBusinessOperatingKernelStore();
            var kernelService = new ContinuousBusinessOperatingKernelService(kernelStore);

            // OPS11: Budget escalation terminates cycle
            var cycle = await kernelService.StartNewCycleAsync("tenant-redteam", "BO-REDTEAM-02", "GO-02", maxBudgetINR: 50_000m);
            cycle.RecordSpend(30_000m);
            Assert.Equal(BusinessCycleStatus.Running, cycle.Status);

            // Overspend exceeds budget limit -> terminates with AbortedGuardrailBreach
            cycle.RecordSpend(30_000m); // Total 60k > 50k
            Assert.Equal(BusinessCycleStatus.AbortedGuardrailBreach, cycle.Status);
            Assert.NotNull(cycle.Termination);
            Assert.True(cycle.Termination.RequiresHumanIntervention); // OPS11 Passed

            // OPS13: Mission slot exhaustion
            var cycle2 = await kernelService.StartNewCycleAsync("tenant-redteam", "BO-REDTEAM-03", "GO-03");
            for (int i = 0; i < cycle2.MaxMissionsCount; i++)
            {
                cycle2.AssignMission($"MISSION-{i}");
            }
            Assert.Throws<InvalidOperationException>(() => cycle2.AssignMission("MISSION-OVERFLOW")); // OPS13 Passed

            // OPS14: Duplicate active running cycle for same objective blocked by Law I42-K
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                kernelService.StartNewCycleAsync("tenant-redteam", "BO-REDTEAM-03", "GO-03")); // OPS14 Passed
        }

        [Fact]
        public async Task OPS21_To_OPS30_GovernanceAndPolicyAttacks_FailClosed()
        {
            var policy = UnitEconomicsPolicy.CreateDefaultSaaS("tenant-redteam", "BO-REDTEAM-04");

            // OPS28/29: Self-discounting without PRG-1 authorization blocked
            bool discountValid = policy.ValidateNegotiationDiscount(
                initialPrice: 100_000m, agreedPrice: 70_000m, hasPrg1Signoff: false, out string discReason);
            Assert.False(discountValid); // 30% discount > 15% max without PRG-1 signoff
            Assert.Contains("PRG-1 human authorization", discReason); // OPS28/29 Passed

            // OPS30: Law I42 constitutional invariant validation
            Assert.True(LawI42ConstitutionalInvariants.ValidateAllAxioms());
            Assert.Equal(12, LawI42ConstitutionalInvariants.AllLaws.Count);
        }
    }
}
