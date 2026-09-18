using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Delivery;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Growth;
using BusinessModelApp.Infrastructure.Runtime.Enterprise.Delivery;
using Xunit;

namespace BusinessModelApp.Tests.Domain
{
    /// <summary>
    /// Phase 4.10 Batch 4101: Delivery & Value Realization Red Team Tests (DELIV01 - DELIV04).
    /// </summary>
    public class Phase4Batch4101DeliveryRedTeamTests
    {
        private static string Sha256(string input)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        [Fact]
        public async Task DELIV01_AcceptanceWithIncompleteMilestones_FailsClosed()
        {
            var store = new InMemoryCustomerDeliveryRuntimeStore();
            var service = new CustomerDeliveryRuntimeService(store);

            var m1 = new DeliveryMilestone { Title = "Task A", TargetDateUtc = DateTime.UtcNow };
            var m2 = new DeliveryMilestone { Title = "Task B", TargetDateUtc = DateTime.UtcNow };

            var project = await service.CreateProjectAsync(
                "tenant-alpha",
                "customer-01",
                "contract-01",
                new List<DeliveryMilestone> { m1, m2 },
                new List<ValueRealizationMetric>());

            await service.AdvanceProjectStatusAsync(project.ProjectId, DeliveryProjectStatus.Onboarding);
            await service.AdvanceProjectStatusAsync(project.ProjectId, DeliveryProjectStatus.InProgress);

            // Only complete m1, m2 is still pending!
            m1.Complete(Sha256("artifact-a"));

            // Requesting acceptance must fail
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.AdvanceProjectStatusAsync(project.ProjectId, DeliveryProjectStatus.DeliveredPendingAcceptance));

            Assert.Contains("All milestones must be completed", ex.Message);
        }

        [Fact]
        public async Task DELIV02_ValueRealizationMetric_WithL1SimulationEvidence_FailsClosed()
        {
            var store = new InMemoryCustomerDeliveryRuntimeStore();
            var service = new CustomerDeliveryRuntimeService(store);

            var metric = new ValueRealizationMetric
            {
                ProjectId = "proj-01",
                MetricName = "CostReductionPct",
                BaselineValue = 0m,
                TargetValue = 25m,
                Unit = "Percent"
            };

            var project = await service.CreateProjectAsync(
                "tenant-alpha",
                "customer-01",
                "contract-01",
                new List<DeliveryMilestone>(),
                new List<ValueRealizationMetric> { metric });

            // Attempting to record measurement using L1_Simulation
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.RecordMetricMeasurementAsync(
                    project.ProjectId,
                    metric.MetricId,
                    28m,
                    EpistemicEvidenceLevel.Simulation, // DISALLOWED
                    Sha256("simulated-run")));

            Assert.Contains("at least ConnectorObserved", ex.Message);
        }

        [Fact]
        public async Task DELIV03_ConfirmValueRealization_WithoutRecordedMetrics_FailsClosed()
        {
            var store = new InMemoryCustomerDeliveryRuntimeStore();
            var service = new CustomerDeliveryRuntimeService(store);

            var metric = new ValueRealizationMetric
            {
                ProjectId = "proj-01",
                MetricName = "ThroughputUplift",
                BaselineValue = 100m,
                TargetValue = 200m,
                Unit = "TPS"
            };

            var m = new DeliveryMilestone { Title = "Core Setup", TargetDateUtc = DateTime.UtcNow };

            var project = await service.CreateProjectAsync(
                "tenant-alpha",
                "customer-01",
                "contract-01",
                new List<DeliveryMilestone> { m },
                new List<ValueRealizationMetric> { metric });

            await service.AdvanceProjectStatusAsync(project.ProjectId, DeliveryProjectStatus.Onboarding);
            await service.AdvanceProjectStatusAsync(project.ProjectId, DeliveryProjectStatus.InProgress);

            m.Complete(Sha256("core-setup-artifact"));

            await service.AdvanceProjectStatusAsync(project.ProjectId, DeliveryProjectStatus.DeliveredPendingAcceptance);
            await service.AdvanceProjectStatusAsync(project.ProjectId, DeliveryProjectStatus.DeliveryAccepted, Sha256("signed-acceptance"));
            await service.AdvanceProjectStatusAsync(project.ProjectId, DeliveryProjectStatus.ValueRealizationUnderway);

            // Attempt to confirm value realization before measuring metric
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.AdvanceProjectStatusAsync(project.ProjectId, DeliveryProjectStatus.ValueRealized));

            Assert.Contains("must have verified L3+ measurements", ex.Message);
        }

        [Fact]
        public async Task DELIV04_PrematureJumpToValueRealized_BypassingAcceptance_FailsClosed()
        {
            var store = new InMemoryCustomerDeliveryRuntimeStore();
            var service = new CustomerDeliveryRuntimeService(store);

            var project = await service.CreateProjectAsync(
                "tenant-alpha",
                "customer-01",
                "contract-01",
                new List<DeliveryMilestone>(),
                new List<ValueRealizationMetric>());

            // Attempting jump straight from Initiated to ValueRealized
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.AdvanceProjectStatusAsync(project.ProjectId, DeliveryProjectStatus.ValueRealized));

            Assert.Contains("active ValueRealizationUnderway status", ex.Message);
        }
    }
}
