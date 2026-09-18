using System;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Growth;
using BusinessModelApp.Infrastructure.Runtime.Enterprise.Growth;
using Xunit;

namespace BusinessModelApp.Tests.Domain
{
    public class Phase4Batch453GrowthExperimentTests
    {
        [Fact]
        public async Task CreateExperiment_ValidInput_CreatesApprovedExperiment()
        {
            var store = new InMemoryGrowthExperimentStore();
            var service = new GrowthExperimentService(store);

            var exp = await service.CreateExperimentAsync(
                hypothesisStatement: "Targeting CFOs with automated reconciliation ROI metrics increases demo booking rate by 20%.",
                primaryMetricName: "DemoBookingRate",
                baselineMetricValue: 0.05m,
                minimumDetectableEffectPercent: 20.0m,
                requiredSampleSize: 200,
                allocatedBudget: 5_000m,
                governanceSignoffId: "PRG1_EXP_SIGNOFF_881");

            Assert.NotNull(exp);
            Assert.Equal(GrowthExperimentStatus.Approved, exp.Status);
            Assert.Equal("DemoBookingRate", exp.PrimaryMetricName);
            Assert.Equal(200, exp.RequiredSampleSize);
        }

        [Fact]
        public async Task CreateExperiment_MissingGovernanceSignoff_ThrowsInvalidOperationException()
        {
            var store = new InMemoryGrowthExperimentStore();
            var service = new GrowthExperimentService(store);

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await service.CreateExperimentAsync(
                    hypothesisStatement: "Hypothesis",
                    primaryMetricName: "Conversion",
                    baselineMetricValue: 0.1m,
                    minimumDetectableEffectPercent: 15m,
                    requiredSampleSize: 100,
                    allocatedBudget: 1000m,
                    governanceSignoffId: ""); // Missing
            });
        }

        [Fact]
        public async Task RecordExperimentTelemetry_StatisticallySignificantWinner_DetectsSignificance()
        {
            var store = new InMemoryGrowthExperimentStore();
            var service = new GrowthExperimentService(store);

            var exp = await service.CreateExperimentAsync(
                "Hypothesis",
                "ConversionRate",
                0.04m,
                25m,
                150,
                3000m,
                "GOV-SIGNOFF-1");

            // Record telemetry: 180 samples, control 4%, variant 7%, p = 0.012 (< 0.05)
            await service.RecordExperimentTelemetryAsync(exp.ExperimentId, 180, 0.04m, 0.07m, 0.012m);

            var fetched = await store.GetExperimentAsync(exp.ExperimentId);
            Assert.NotNull(fetched);
            Assert.Equal(GrowthExperimentStatus.Running, fetched!.Status);
            Assert.True(fetched.IsStatisticallySignificant);

            await service.ConcludeExperimentAsync(exp.ExperimentId, GrowthExperimentStatus.ConcludedWinner, "Variant increased demo conversions significantly.");
            Assert.Equal(GrowthExperimentStatus.ConcludedWinner, fetched.Status);
        }

        [Fact]
        public async Task CreateGrowthMission_EnforcesHierarchyContinuityAndFloors()
        {
            var store = new InMemoryGrowthExperimentStore();
            var service = new GrowthExperimentService(store);

            // Missing growth objective link -> Throws
            await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await service.CreateGrowthMissionAsync(
                    growthObjectiveId: "",
                    missionName: "Mid-Market FinTech Expansion",
                    targetIcpDescription: "US FinTechs $20M-$100M ARR",
                    budgetLimit: 50_000m);
            });

            // Target LTV:CAC < 3.0x -> Throws
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await service.CreateGrowthMissionAsync(
                    growthObjectiveId: "GRO-OBJ-01",
                    missionName: "Low margin push",
                    targetIcpDescription: "ICP",
                    budgetLimit: 50_000m,
                    minTargetLtvToCac: 2.0m);
            });

            // Target Margin < 35% -> Throws
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await service.CreateGrowthMissionAsync(
                    growthObjectiveId: "GRO-OBJ-01",
                    missionName: "Low margin push",
                    targetIcpDescription: "ICP",
                    budgetLimit: 50_000m,
                    minTargetLtvToCac: 3.5m,
                    minTargetMarginPercent: 25.0m);
            });

            // Valid mission -> Pass
            var mission = await service.CreateGrowthMissionAsync(
                growthObjectiveId: "GRO-OBJ-01",
                missionName: "Mid-Market FinTech Expansion",
                targetIcpDescription: "US FinTechs $20M-$100M ARR",
                budgetLimit: 50_000m,
                minTargetLtvToCac: 4.0m,
                minTargetMarginPercent: 45.0m);

            Assert.NotNull(mission);
            Assert.Equal(GrowthMissionStatus.Planned, mission.Status);
        }

        [Fact]
        public async Task ActivateMissionAndProgress_BudgetOverrun_TriggersGuardrailBreach()
        {
            var store = new InMemoryGrowthExperimentStore();
            var service = new GrowthExperimentService(store);

            var mission = await service.CreateGrowthMissionAsync(
                "GRO-OBJ-01",
                "Outbound Blitz Q1",
                "Series B SaaS",
                budgetLimit: 10_000m);

            await service.ActivateMissionAsync(mission.MissionId, new[] { "LeadGenAgent", "OutreachCloserAgent" });
            Assert.Equal(GrowthMissionStatus.Active, mission.Status);
            Assert.Equal(2, mission.AssignedAgentRoles.Count);

            // Spend within budget
            await service.RecordMissionProgressAsync(mission.MissionId, addedSpend: 6_000m, addedRealizedRevenue: 15_000m);
            Assert.Equal(GrowthMissionStatus.Active, mission.Status);

            // Spend exceeds budget (6k + 5k = 11k > 10k) -> Guardrail breach
            await service.RecordMissionProgressAsync(mission.MissionId, addedSpend: 5_000m, addedRealizedRevenue: 5_000m);
            Assert.Equal(GrowthMissionStatus.PausedGuardrailBreach, mission.Status);
            Assert.Contains("exceeded approved budget limit", mission.GuardrailBreachReason);
        }
    }
}
