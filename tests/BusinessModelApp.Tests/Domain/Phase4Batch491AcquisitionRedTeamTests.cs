using System;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Acquisition;
using BusinessModelApp.Infrastructure.Runtime.Enterprise.Acquisition;
using Xunit;

namespace BusinessModelApp.Tests.Domain
{
    /// <summary>
    /// Phase 4.9 Batch 491: Customer Acquisition Adversarial & Red Team Tests (ACQ01 - ACQ05).
    /// </summary>
    public class Phase4Batch491AcquisitionRedTeamTests
    {
        [Fact]
        public async Task ACQ01_OutreachWithoutBatch6Permit_ThrowsAndFailsClosed()
        {
            var store = new InMemoryCustomerAcquisitionRuntimeStore();
            var service = new CustomerAcquisitionRuntimeService(store);

            var task = await service.PrepareOutreachAsync(
                "tenant-alpha",
                "prospect-red-01",
                "cto@target-enterprise.com",
                "Enterprise ERP Integration",
                "Value proposition content...",
                lastContactedUtc: null);

            // Attempting execution without Batch 6 permit ID
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.ExecuteGovernedOutreachAsync(task.TaskId, "", "SES-MOCK-123"));

            Assert.Contains("Batch 6 Execution Permit", ex.Message);
        }

        [Fact]
        public async Task ACQ02_OverContactingFrequencyViolation_FailsClosedWithSuppression()
        {
            var store = new InMemoryCustomerAcquisitionRuntimeStore();
            var service = new CustomerAcquisitionRuntimeService(store);

            await store.SavePolicyAsync(new OutreachCampaignPolicy
            {
                TenantId = "tenant-alpha",
                MinDaysBetweenTouches = 14
            });

            // Contacted 3 days ago (violates 14 day interval)
            var task = await service.PrepareOutreachAsync(
                "tenant-alpha",
                "prospect-red-02",
                "vp@target-enterprise.com",
                "Follow-up pitch",
                "Checking in...",
                lastContactedUtc: DateTime.UtcNow.AddDays(-3));

            Assert.Equal(OutreachDeliveryStatus.SuppressedFrequencyLimit, task.DeliveryStatus);
            Assert.Contains("Minimum contact interval", task.SuppressionReason);

            // Attempting to bypass suppression throws InvalidOperationException
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.ExecuteGovernedOutreachAsync(task.TaskId, "PERMIT-VALID-999", "SES-999"));

            Assert.Contains("Cannot execute suppressed outreach", ex.Message);
        }

        [Fact]
        public async Task ACQ03_NonExistentOutreachTask_FailsClosedWithKeyNotFound()
        {
            var store = new InMemoryCustomerAcquisitionRuntimeStore();
            var service = new CustomerAcquisitionRuntimeService(store);

            await Assert.ThrowsAsync<System.Collections.Generic.KeyNotFoundException>(() =>
                service.ExecuteGovernedOutreachAsync("non-existent-task-id", "PERMIT-123", "SES-123"));
        }

        [Fact]
        public async Task ACQ04_MultipleOutreachListing_MaintainsTenantAndProspectIntegrity()
        {
            var store = new InMemoryCustomerAcquisitionRuntimeStore();
            var service = new CustomerAcquisitionRuntimeService(store);

            var task1 = await service.PrepareOutreachAsync(
                "tenant-alpha",
                "prospect-target-X",
                "lead1@target.com",
                "Subject 1",
                "Content 1",
                null);

            var task2 = await service.PrepareOutreachAsync(
                "tenant-alpha",
                "prospect-target-X",
                "lead2@target.com",
                "Subject 2",
                "Content 2",
                null);

            var taskForeign = await service.PrepareOutreachAsync(
                "tenant-beta",
                "prospect-target-Y",
                "foreign@target.com",
                "Subject foreign",
                "Content foreign",
                null);

            var prospectXTasks = await store.ListTasksForProspectAsync("prospect-target-X");
            Assert.Equal(2, prospectXTasks.Count);
            Assert.DoesNotContain(prospectXTasks, t => t.ProspectId == "prospect-target-Y");
        }
    }
}
