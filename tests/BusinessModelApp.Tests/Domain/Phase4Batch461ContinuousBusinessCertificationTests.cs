using System;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Growth;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Operations;
using BusinessModelApp.Infrastructure.Runtime.Enterprise.Growth;
using BusinessModelApp.Infrastructure.Runtime.Enterprise.Operations;
using Xunit;

namespace BusinessModelApp.Tests.Domain
{
    /// <summary>
    /// Phase 4.6 Master Certification Test Suite.
    /// Certifies G1 (Continuous Engineering), G2 (Production Resilience),
    /// G3 (Business Reality Gate), and G4 (Continuous Multi-Cycle Learning).
    /// </summary>
    public class Phase4Batch461ContinuousBusinessCertificationTests
    {
        [Fact]
        public async Task G1_And_G2_ContinuousOperatingKernel_And_CrashRecovery_Certified()
        {
            var kernelStore = new InMemoryContinuousBusinessOperatingKernelStore();
            var kernelService = new ContinuousBusinessOperatingKernelService(kernelStore);

            // 1. Start bounded cycle
            var cycle = await kernelService.StartNewCycleAsync(
                "tenant-enterprise-ops",
                "BO-ENTERPRISE-CORE",
                "GO-OPS-2026",
                maxDuration: TimeSpan.FromHours(4),
                maxBudgetINR: 300_000m);

            Assert.Equal(BusinessCycleStatus.Running, cycle.Status);
            Assert.Equal(BusinessCycleStage.Observe, cycle.CurrentStage);
            Assert.Single(cycle.Checkpoints);

            // 2. Advance through stages with durable checkpoints
            await kernelService.AdvanceStageAsync(cycle.CycleId, BusinessCycleStage.Evidence);
            await kernelService.AdvanceStageAsync(cycle.CycleId, BusinessCycleStage.Interpret);
            await kernelService.AdvanceStageAsync(cycle.CycleId, BusinessCycleStage.Prioritize);
            await kernelService.AdvanceStageAsync(cycle.CycleId, BusinessCycleStage.Allocate);
            await kernelService.AdvanceStageAsync(cycle.CycleId, BusinessCycleStage.Plan);

            cycle.AssignMission("MISSION-DELIVERY-001");
            cycle.RecordSpend(25_000m);
            await kernelService.CheckpointCycleAsync(cycle.CycleId, "CHECKPOINT-HASH-MID-CYCLE");

            Assert.Equal(BusinessCycleStage.Plan, cycle.CurrentStage);
            Assert.Equal(7, cycle.Checkpoints.Count);

            // 3. Simulate process crash and verify recovery
            var recoveredCycle = await kernelService.RecoverCycleAfterCrashAsync(
                cycle.CycleId, "Restored after node restart; external state verified");

            Assert.Equal(BusinessCycleStatus.Running, recoveredCycle.Status);
            Assert.Equal(BusinessCycleStage.Plan, recoveredCycle.CurrentStage);
            Assert.Equal(25_000m, recoveredCycle.CurrentSpentBudgetINR);

            // 4. Conclude cycle with positive outcome
            var outcome = new BusinessCycleOutcome
            {
                CycleId = cycle.CycleId,
                OpportunitiesIdentified = 8,
                OutboundTouchesSent = 20,
                DealsNegotiated = 2,
                CustomersOnboarded = 2,
                RealizedCashINR = 300_000m,
                FullyLoadedCostsINR = 75_000m,
                HealthScoreDelta = 5.0m
            };

            var concludedCycle = await kernelService.ConcludeCycleAsync(cycle.CycleId, outcome);
            Assert.Equal(BusinessCycleStatus.Completed, concludedCycle.Status);
            Assert.Equal(BusinessCycleStage.Concluded, concludedCycle.CurrentStage);
            Assert.True(concludedCycle.Outcome!.IsEconomicallyPositive);
            Assert.Equal(225_000m, concludedCycle.Outcome.NetContributionINR);
        }

        [Fact]
        public async Task G3_ProductionRealityLedger_And_ExternalGrounding_Certified()
        {
            var ledgerStore = new InMemoryProductionRealityLedgerStore();
            var ledgerService = new ProductionRealityLedgerService(ledgerStore);

            // 1. Record Level 5 External Wire Transfer in Reality Ledger
            var realityEvent = await ledgerService.RecordExternalEventAsync(
                tenantId: "tenant-enterprise-ops",
                businessObjectiveId: "BO-ENTERPRISE-CORE",
                growthObjectiveId: "GO-OPS-2026",
                businessObjectId: "INV-2026-PRODUCTION-001",
                eventType: ProductionRealityEventType.PaymentSettled,
                evidenceLevel: EpistemicEvidenceLevel.BankVerifiedCash,
                source: "FederalReserveFedwire",
                connectorName: "JPMorganFedwireConnector",
                externalProvider: "JPMorganChase",
                externalReferenceId: "WIRE-FED-JPMC-20260917-991827",
                rawPayloadForDigest: "FEDWIRE:JPMC:USD:500000:CREDIT:TX-991827",
                revenueImpact: 500_000m,
                costImpact: 50_000m,
                counterpartyReference: "CP-FORTUNE-500-CLIENT");

            Assert.NotEmpty(realityEvent.EvidenceDigestSha256);
            Assert.Equal(ExternalReconciliationStatus.PendingVerification, realityEvent.VerificationStatus);

            // 2. Reconcile via Authorized External Auditor Authority
            var reconciled = await ledgerService.ReconcileEventAsync(
                realityEvent.EventId,
                "AuditorGeneralAuthority",
                ExternalReconciliationStatus.Reconciled,
                "Cryptographic proof matches Fedwire settlement statement");

            Assert.Equal(ExternalReconciliationStatus.Reconciled, reconciled.VerificationStatus);
            Assert.True(reconciled.IsAuthoritative);

            // 3. Verify Reality Evidence Integrity passes strict gating
            bool isVerified = await ledgerService.VerifyRealityEvidenceIntegrityAsync(realityEvent.EventId);
            Assert.True(isVerified);
        }

        [Fact]
        public async Task G4_ContinuousMultiCycleOperation_ThreeSequentialCycles_Passes()
        {
            var kernelStore = new InMemoryContinuousBusinessOperatingKernelStore();
            var realityStore = new InMemoryProductionRealityLedgerStore();
            var controlStore = new InMemoryGrowthControlPlaneStore();
            var acquisitionStore = new InMemoryCustomerAcquisitionAndRetentionStore();
            var treasuryStore = new InMemoryUnitEconomicsAndTreasuryStore();
            var experimentStore = new InMemoryGrowthExperimentStore();

            var kernelService = new ContinuousBusinessOperatingKernelService(kernelStore);
            var realityService = new ProductionRealityLedgerService(realityStore);
            var stateBuilder = new BusinessGrowthStateBuilder(controlStore, acquisitionStore, treasuryStore, experimentStore);
            var economicsController = new BusinessUnitEconomicsController();
            var cycleManager = new AutonomousBusinessCycleManager(kernelService, realityService, stateBuilder, economicsController);

            var policy = UnitEconomicsPolicy.CreateDefaultSaaS("tenant-multicycle", "BO-MULTI-CYCLE-2026");

            // Execute 3 Sequential Bounded Business Cycles (G4 Continuous Operation)
            // Cycle 1: Acquire & Deliver
            var outcome1 = await cycleManager.ExecuteFullAutonomousCycleAsync(
                "tenant-multicycle", "BO-MULTI-CYCLE-2026", "GO-2026", policy);
            Assert.True(outcome1.IsEconomicallyPositive);
            Assert.Equal(65_000m, outcome1.NetContributionINR);

            // Cycle 2: Measure & Optimize
            var outcome2 = await cycleManager.ExecuteFullAutonomousCycleAsync(
                "tenant-multicycle", "BO-MULTI-CYCLE-2026", "GO-2026", policy);
            Assert.True(outcome2.IsEconomicallyPositive);

            // Cycle 3: Expand & Retain
            var outcome3 = await cycleManager.ExecuteFullAutonomousCycleAsync(
                "tenant-multicycle", "BO-MULTI-CYCLE-2026", "GO-2026", policy);
            Assert.True(outcome3.IsEconomicallyPositive);

            // Verify all 3 completed cycles in history
            var allCycles = await kernelStore.ListCyclesAsync("tenant-multicycle");
            Assert.Equal(3, allCycles.Count);
            Assert.All(allCycles, c => Assert.Equal(BusinessCycleStatus.Completed, c.Status));
        }
    }
}
