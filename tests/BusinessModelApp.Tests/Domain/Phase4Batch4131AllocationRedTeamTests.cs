using System;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Allocation;
using BusinessModelApp.Infrastructure.Runtime.Enterprise.Allocation;
using Xunit;

namespace BusinessModelApp.Tests.Domain
{
    /// <summary>
    /// Phase 4.13 Batch 4131: Autonomous Resource Allocation Red Team Tests (ALLOC01 - ALLOC04).
    /// </summary>
    public class Phase4Batch4131AllocationRedTeamTests
    {
        [Fact]
        public async Task ALLOC01_LowRunway_ThrottlesNonCriticalAllocations()
        {
            var store = new InMemoryBusinessResourceAllocationStore();
            var service = new BusinessResourceAllocationService(store);

            await service.ConfigureBudgetAsync("tenant-alpha", ResourcePoolType.MarketingSpendINR, 500_000m, 10m);

            // Request with runway = 2.1 months (< 3.0 months) for P2 normal
            var req = await service.RequestAllocationAsync(
                "tenant-alpha",
                "mission-campaign",
                ResourcePoolType.MarketingSpendINR,
                50_000m,
                AllocationPriority.P2_Normal,
                currentRunwayMonths: 2.1m);

            Assert.Equal(AllocationDecisionStatus.RejectedRunwayRisk, req.Status);
            Assert.Contains("Runway is critically low", req.DecisionReason);
        }

        [Fact]
        public async Task ALLOC02_ExceedingTotalCapacity_FailsClosedEvenForCritical()
        {
            var store = new InMemoryBusinessResourceAllocationStore();
            var service = new BusinessResourceAllocationService(store);

            await service.ConfigureBudgetAsync("tenant-alpha", ResourcePoolType.ComputeTokens, 100_000m, 10m);

            // Request 150_000 tokens (exceeds total capacity 100_000)
            var req = await service.RequestAllocationAsync(
                "tenant-alpha",
                "mission-urgent",
                ResourcePoolType.ComputeTokens,
                150_000m,
                AllocationPriority.P0_Critical);

            Assert.Equal(AllocationDecisionStatus.RejectedExceedsBudget, req.Status);
            Assert.Contains("Total capacity exceeded", req.DecisionReason);
        }

        [Fact]
        public async Task ALLOC03_UnconfiguredBudgetPool_FailsClosed()
        {
            var store = new InMemoryBusinessResourceAllocationStore();
            var service = new BusinessResourceAllocationService(store);

            var req = await service.RequestAllocationAsync(
                "tenant-alpha",
                "mission-api",
                ResourcePoolType.ExternalApiQuota,
                1_000m,
                AllocationPriority.P1_High);

            Assert.Equal(AllocationDecisionStatus.RejectedExceedsBudget, req.Status);
            Assert.Contains("No active budget configured", req.DecisionReason);
        }

        [Fact]
        public async Task ALLOC04_NegativeAmount_ThrowsArgumentOutOfRangeException()
        {
            var store = new InMemoryBusinessResourceAllocationStore();
            var service = new BusinessResourceAllocationService(store);

            await service.ConfigureBudgetAsync("tenant-alpha", ResourcePoolType.ComputeTokens, 100_000m);

            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
                service.RequestAllocationAsync("tenant-alpha", "m-01", ResourcePoolType.ComputeTokens, -100m, AllocationPriority.P1_High));
        }
    }
}
