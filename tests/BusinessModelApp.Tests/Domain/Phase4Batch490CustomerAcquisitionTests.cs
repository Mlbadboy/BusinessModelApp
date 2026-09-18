using System;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Acquisition;
using BusinessModelApp.Infrastructure.Runtime.Enterprise.Acquisition;
using Xunit;

namespace BusinessModelApp.Tests.Domain
{
    /// <summary>
    /// Phase 4.9 Batch 490: Autonomous Customer Acquisition Runtime Tests.
    /// </summary>
    public class Phase4Batch490CustomerAcquisitionTests
    {
        [Fact]
        public async Task GovernedOutreach_WithBatch6Permit_ExecutesSuccessfully()
        {
            var store = new InMemoryCustomerAcquisitionRuntimeStore();
            var service = new CustomerAcquisitionRuntimeService(store);

            // 1. Prepare outreach
            var task = await service.PrepareOutreachAsync(
                "tenant-enterprise-scale",
                "PROSPECT-HORIZON-01",
                "david@horizonpay.com",
                "Continuous Treasury Reconciliation Automation",
                "David, our sovereign business operator Charlie eliminates manual monthly close reconciliation...",
                lastContactedUtc: null);

            Assert.Equal(OutreachDeliveryStatus.Draft, task.DeliveryStatus);
            Assert.Empty(task.SuppressionReason);

            // 2. Authorize with Batch 6 execution permit and deliver
            var executed = await service.ExecuteGovernedOutreachAsync(
                task.TaskId,
                "BATCH6-PERMIT-OUTREACH-889102",
                "SES-MESSAGE-ID-99827361");

            Assert.Equal(OutreachDeliveryStatus.Sent, executed.DeliveryStatus);
            Assert.Equal("BATCH6-PERMIT-OUTREACH-889102", executed.Batch6PermitId);
            Assert.Equal("SES-MESSAGE-ID-99827361", executed.ExternalProviderReference);
            Assert.NotNull(executed.SentAtUtc);
        }

        [Fact]
        public async Task FrequencyLimitGuard_SuppressesOverContacting()
        {
            var store = new InMemoryCustomerAcquisitionRuntimeStore();
            var service = new CustomerAcquisitionRuntimeService(store);

            // Set policy: Min 7 days between touches
            await store.SavePolicyAsync(new OutreachCampaignPolicy
            {
                TenantId = "tenant-enterprise-scale",
                MinDaysBetweenTouches = 7
            });

            // Contacted 2 days ago -> MUST BE SUPPRESSED
            var suppressedTask = await service.PrepareOutreachAsync(
                "tenant-enterprise-scale",
                "PROSPECT-HORIZON-01",
                "david@horizonpay.com",
                "Follow up inquiry",
                "Hi David, following up on our demonstration...",
                lastContactedUtc: DateTime.UtcNow.AddDays(-2));

            Assert.Equal(OutreachDeliveryStatus.SuppressedFrequencyLimit, suppressedTask.DeliveryStatus);
            Assert.Contains("Minimum contact interval", suppressedTask.SuppressionReason);

            // Attempting to execute suppressed task throws
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.ExecuteGovernedOutreachAsync(suppressedTask.TaskId, "PERMIT-001", "REF-001"));
        }
    }
}
