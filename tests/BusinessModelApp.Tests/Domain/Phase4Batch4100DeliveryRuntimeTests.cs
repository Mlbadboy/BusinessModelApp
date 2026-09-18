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
    /// Phase 4.10 Batch 4100: Customer Delivery & Value Realization Domain Tests.
    /// </summary>
    public class Phase4Batch4100DeliveryRuntimeTests
    {
        private static string Sha256(string input)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        [Fact]
        public async Task FullPostSaleLifecycle_FromOnboardingToValueRealized_SucceedsWithEvidence()
        {
            var store = new InMemoryCustomerDeliveryRuntimeStore();
            var service = new CustomerDeliveryRuntimeService(store);

            var milestone1 = new DeliveryMilestone
            {
                Title = "Agent Integration & Tenant Provisioning",
                TargetDateUtc = DateTime.UtcNow.AddDays(7)
            };
            var milestone2 = new DeliveryMilestone
            {
                Title = "Autonomous Reconciliation Pipeline Setup",
                TargetDateUtc = DateTime.UtcNow.AddDays(14)
            };

            var valueMetric = new ValueRealizationMetric
            {
                ProjectId = "proj-temp",
                MetricName = "MonthlyReconciliationHoursSaved",
                BaselineValue = 0m,
                TargetValue = 120m,
                Unit = "Hours"
            };

            var project = await service.CreateProjectAsync(
                "tenant-enterprise-scale",
                "customer-fintech-global",
                "contract-msa-8819",
                new List<DeliveryMilestone> { milestone1, milestone2 },
                new List<ValueRealizationMetric> { valueMetric });

            Assert.Equal(DeliveryProjectStatus.Initiated, project.Status);

            // Step 1: Advance to Onboarding
            await service.AdvanceProjectStatusAsync(project.ProjectId, DeliveryProjectStatus.Onboarding);
            Assert.Equal(DeliveryProjectStatus.Onboarding, project.Status);

            // Step 2: Advance to InProgress
            await service.AdvanceProjectStatusAsync(project.ProjectId, DeliveryProjectStatus.InProgress);
            Assert.Equal(DeliveryProjectStatus.InProgress, project.Status);

            // Complete milestones
            milestone1.Complete(Sha256("artifact-agent-integration-v1"));
            milestone2.Complete(Sha256("artifact-pipeline-setup-v1"));

            // Step 3: Request Acceptance
            await service.AdvanceProjectStatusAsync(project.ProjectId, DeliveryProjectStatus.DeliveredPendingAcceptance);
            Assert.Equal(DeliveryProjectStatus.DeliveredPendingAcceptance, project.Status);

            // Step 4: Customer Signs off Acceptance (Law I40: Delivery != Value Realization)
            var signoffHash = Sha256("counterparty-acceptance-certificate-signed-by-cfo");
            await service.AdvanceProjectStatusAsync(project.ProjectId, DeliveryProjectStatus.DeliveryAccepted, signoffHash);
            Assert.Equal(DeliveryProjectStatus.DeliveryAccepted, project.Status);
            Assert.Equal(signoffHash, project.CounterpartySignoffSha256);

            // Step 5: Begin Value Realization Tracking
            await service.AdvanceProjectStatusAsync(project.ProjectId, DeliveryProjectStatus.ValueRealizationUnderway);
            Assert.Equal(DeliveryProjectStatus.ValueRealizationUnderway, project.Status);

            // Step 6: Record L3+ Observed Value Measurement (135 hours saved vs 120 target)
            var metricProofHash = Sha256("datadog-telemetry-connector-automated-run-logs-30-days");
            await service.RecordMetricMeasurementAsync(
                project.ProjectId,
                valueMetric.MetricId,
                135m,
                EpistemicEvidenceLevel.ConnectorObserved,
                metricProofHash);

            // Step 7: Confirm Certified Value Realization
            await service.AdvanceProjectStatusAsync(project.ProjectId, DeliveryProjectStatus.ValueRealized);
            Assert.Equal(DeliveryProjectStatus.ValueRealized, project.Status);
            Assert.NotNull(project.ActualCompletionUtc);
            Assert.NotNull(project.TimeToValueDays);
        }
    }
}
