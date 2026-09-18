using System;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Allocation;
using BusinessModelApp.Infrastructure.Runtime.Enterprise.Allocation;
using Xunit;

namespace BusinessModelApp.Tests.Domain
{
    /// <summary>
    /// Phase 4.13 Batch 4130: Autonomous Resource Allocation Domain Tests.
    /// </summary>
    public class Phase4Batch4130AllocationTests
    {
        [Fact]
        public async Task NormalPriorityAllocation_AndRelease_ManagesCapacityAccurately()
        {
            var store = new InMemoryBusinessResourceAllocationStore();
            var service = new BusinessResourceAllocationService(store);

            // Configure ComputeTokens pool: 1,000,000 tokens total, 20% reserve (800,000 usable for P2/P3)
            var budget = await service.ConfigureBudgetAsync("tenant-alpha", ResourcePoolType.ComputeTokens, 1_000_000m, 20.0m);
            Assert.Equal(1_000_000m, budget.TotalCapacity);
            Assert.Equal(800_000m, budget.UsableCapacityForNormalPriority);

            // Allocate 300,000 for normal mission
            var req = await service.RequestAllocationAsync(
                "tenant-alpha",
                "mission-01",
                ResourcePoolType.ComputeTokens,
                300_000m,
                AllocationPriority.P2_Normal,
                currentRunwayMonths: 14m);

            Assert.Equal(AllocationDecisionStatus.Approved, req.Status);
            Assert.Equal(700_000m, budget.AvailableCapacity);
            Assert.Equal(500_000m, budget.UsableCapacityForNormalPriority);

            // Release 300,000 after mission completion
            await service.ReleaseAllocationAsync(req.RequestId);
            Assert.Equal(AllocationDecisionStatus.Released, req.Status);
            Assert.Equal(1_000_000m, budget.AvailableCapacity);
        }

        [Fact]
        public async Task HighPriorityAllocation_AccessesProtectedReserveFloor()
        {
            var store = new InMemoryBusinessResourceAllocationStore();
            var service = new BusinessResourceAllocationService(store);

            // Configure 10 AgentMissionSlots, 20% reserve (2 slots reserved for P0/P1)
            var budget = await service.ConfigureBudgetAsync("tenant-alpha", ResourcePoolType.AgentMissionSlots, 10m, 20.0m);

            // Consume 8 slots with normal priority
            var normalReq = await service.RequestAllocationAsync(
                "tenant-alpha", "m-norm", ResourcePoolType.AgentMissionSlots, 8m, AllocationPriority.P2_Normal);
            Assert.Equal(AllocationDecisionStatus.Approved, normalReq.Status);

            // Attempt another normal priority slot (should fail due to 20% reserve floor)
            var blockedNormal = await service.RequestAllocationAsync(
                "tenant-alpha", "m-norm-2", ResourcePoolType.AgentMissionSlots, 1m, AllocationPriority.P2_Normal);
            Assert.Equal(AllocationDecisionStatus.RejectedExceedsBudget, blockedNormal.Status);
            Assert.Contains("Reserve floor", blockedNormal.DecisionReason);

            // P0 Critical allocation CAN access the reserved 2 slots
            var criticalReq = await service.RequestAllocationAsync(
                "tenant-alpha", "m-crit", ResourcePoolType.AgentMissionSlots, 2m, AllocationPriority.P0_Critical);
            Assert.Equal(AllocationDecisionStatus.Approved, criticalReq.Status);
        }
    }
}
