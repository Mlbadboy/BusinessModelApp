using System;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Experiments;
using BusinessModelApp.Infrastructure.Runtime.Enterprise.Experiments;
using Xunit;

namespace BusinessModelApp.Tests.Domain
{
    /// <summary>
    /// Phase 4.14 Batch 4141: Growth Experiment Red Team Tests (EXP01 - EXP04).
    /// </summary>
    public class Phase4Batch4141ExperimentRedTeamTests
    {
        [Fact]
        public async Task EXP01_BudgetOverrun_TriggersEmergencyTermination()
        {
            var store = new InMemoryGrowthExperimentStore();
            var service = new GrowthExperimentRuntimeService(store);

            var exp = await service.CreateExperimentAsync(
                "tenant-alpha",
                "Aggressive Ad Spend Test",
                "Hypothesis on high-budget paid search",
                GrowthExperimentType.OutreachMessaging,
                0.02m, 0.05m,
                budgetAllocatedINR: 50_000m);

            await service.StartExperimentAsync(exp.ExperimentId);

            // Spend ₹60,000 > ₹50,000 allocated
            await service.RecordObservationsAsync(
                exp.ExperimentId,
                controlSamples: 20, controlSuccesses: 1,
                variantSamples: 20, variantSuccesses: 1,
                spendINR: 60_000m);

            Assert.Equal(GrowthExperimentStatus.TerminatedBudgetLimit, exp.Status);
            Assert.Equal(ExperimentDecision.Invalid, exp.Decision);
            Assert.Contains("Emergency Stop: Budget limit", exp.OutcomeNotes);
        }

        [Fact]
        public async Task EXP02_SevereDegradation_TriggersAutomatedSafetyRollback()
        {
            var store = new InMemoryGrowthExperimentStore();
            var service = new GrowthExperimentRuntimeService(store);

            var exp = await service.CreateExperimentAsync(
                "tenant-alpha",
                "Risky Pricing Redesign",
                "Increasing baseline price by 50%",
                GrowthExperimentType.PricingStrategy,
                0.10m, 0.12m,
                budgetAllocatedINR: 100_000m,
                riskCeiling: 0.20m); // 20% max degradation

            await service.StartExperimentAsync(exp.ExperimentId);

            // 100 control with 10 successes (10.0%), 100 variant with 2 successes (2.0% -> 80% degradation!)
            await service.RecordObservationsAsync(
                exp.ExperimentId,
                controlSamples: 100, controlSuccesses: 10,
                variantSamples: 100, variantSuccesses: 2,
                spendINR: 15_000m);

            Assert.Equal(GrowthExperimentStatus.RolledBack, exp.Status);
            Assert.Equal(ExperimentDecision.Rollback, exp.Decision);
            Assert.Contains("Auto-Rollback: Variant conversion", exp.OutcomeNotes);
        }

        [Fact]
        public async Task EXP03_InsufficientSamples_YieldsInconclusive()
        {
            var store = new InMemoryGrowthExperimentStore();
            var service = new GrowthExperimentRuntimeService(store);

            var exp = await service.CreateExperimentAsync(
                "tenant-alpha",
                "Micro Sample Test",
                "Hypothesis",
                GrowthExperimentType.OnboardingFlow,
                0.10m, 0.20m,
                budgetAllocatedINR: 50_000m);

            await service.StartExperimentAsync(exp.ExperimentId);

            // Only 15 samples per arm
            await service.RecordObservationsAsync(
                exp.ExperimentId,
                controlSamples: 15, controlSuccesses: 2,
                variantSamples: 15, variantSuccesses: 4,
                spendINR: 2_000m);

            await service.ConcludeExperimentAsync(exp.ExperimentId, minimumSamplesPerArm: 100);

            Assert.Equal(GrowthExperimentStatus.Concluded, exp.Status);
            Assert.Equal(ExperimentDecision.Inconclusive, exp.Decision);
            Assert.Contains("Inconclusive: Sample size", exp.OutcomeNotes);
        }

        [Fact]
        public async Task EXP04_NegativeBudget_ThrowsArgumentOutOfRangeException()
        {
            var store = new InMemoryGrowthExperimentStore();
            var service = new GrowthExperimentRuntimeService(store);

            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
                service.CreateExperimentAsync("tenant-alpha", "Invalid", "H", GrowthExperimentType.OutreachMessaging, 0.05m, 0.10m, -100m));
        }
    }
}
