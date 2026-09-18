using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Allocation;

namespace BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Allocation
{
    public interface IBusinessResourceAllocationStore
    {
        Task SaveBudgetAsync(TenantResourceBudget budget, CancellationToken cancellationToken = default);
        Task<TenantResourceBudget?> GetBudgetAsync(string tenantId, ResourcePoolType poolType, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<TenantResourceBudget>> ListBudgetsForTenantAsync(string tenantId, CancellationToken cancellationToken = default);

        Task SaveRequestAsync(ResourceAllocationRequest request, CancellationToken cancellationToken = default);
        Task<ResourceAllocationRequest?> GetRequestAsync(string requestId, CancellationToken cancellationToken = default);
    }

    public interface IBusinessResourceAllocationService
    {
        Task<TenantResourceBudget> ConfigureBudgetAsync(
            string tenantId,
            ResourcePoolType poolType,
            decimal totalCapacity,
            decimal reserveFloorPct = 20.0m,
            CancellationToken cancellationToken = default);

        Task<ResourceAllocationRequest> RequestAllocationAsync(
            string tenantId,
            string missionId,
            ResourcePoolType poolType,
            decimal requestedAmount,
            AllocationPriority priority,
            decimal currentRunwayMonths = 12m,
            CancellationToken cancellationToken = default);

        Task ReleaseAllocationAsync(
            string requestId,
            CancellationToken cancellationToken = default);
    }
}
