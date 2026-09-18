using System;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Experiments;
using BusinessModelApp.Infrastructure.Runtime.Enterprise.Experiments;
using Xunit;

namespace BusinessModelApp.Tests.Domain
{
    /// <summary>
    /// Phase 4.14 Batch 4140: Autonomous Growth Experimentation Domain Tests.
    /// </summary>
    public class Phase4Batch4140ExperimentTests
    {
        [Fact]
        public async Task WinningVariant_WithSufficientSamples_PromotesExperiment()
        {
            var store = new InMemoryGrowthExperimentStore();
            var service = new GrowthExperimentRuntimeService(store);

            var exp = await service.CreateExperimentAsync(
                "tenant-alpha",
                "Personalized CFO Outreach vs Standard Pitch",
                "Personalized value articulation based on public 10-K filings increases response conversion.",
                GrowthExperimentType.OutreachMessaging,
                baselineConversionRate: 0.05m, // 5% baseline
                targetConversionRate: 0.10m,   // 10% target
                budgetAllocatedINR: 100_000m);

            Assert.Equal(GrowthExperimentStatus.Draft, exp.Status);

            await service.StartExperimentAsync(exp.ExperimentId);
            Assert.Equal(GrowthExperimentStatus.Active, exp.Status);

            // Record 150 control (7 wins = 4.67%) and 150 variant (18 wins = 12.0%)
            await service.RecordObservationsAsync(
                exp.ExperimentId,
                controlSamples: 150,
                controlSuccesses: 7,
                variantSamples: 150,
                variantSuccesses: 18,
                spendINR: 45_000m);

            await service.ConcludeExperimentAsync(exp.ExperimentId, minimumSamplesPerArm: 100);

            Assert.Equal(GrowthExperimentStatus.Concluded, exp.Status);
            Assert.Equal(ExperimentDecision.Promote, exp.Decision);
            Assert.Contains("Promote", exp.OutcomeNotes);
            Assert.True(exp.VariantRate > exp.ControlRate);
            Assert.True(exp.VariantRate >= exp.TargetConversionRate);
        }
    }
}
