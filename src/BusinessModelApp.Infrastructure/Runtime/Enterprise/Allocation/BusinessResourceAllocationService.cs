using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Allocation;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Allocation;

namespace BusinessModelApp.Infrastructure.Runtime.Enterprise.Allocation
{
    public sealed class InMemoryBusinessResourceAllocationStore : IBusinessResourceAllocationStore
    {
        private readonly ConcurrentDictionary<string, TenantResourceBudget> _budgets = new();
        private readonly ConcurrentDictionary<string, ResourceAllocationRequest> _requests = new();

        private static string BudgetKey(string tenantId, ResourcePoolType type) => $"{tenantId}::{type}";

        public Task SaveBudgetAsync(TenantResourceBudget budget, CancellationToken cancellationToken = default)
        {
            _budgets[BudgetKey(budget.TenantId, budget.PoolType)] = budget;
            return Task.CompletedTask;
        }

        public Task<TenantResourceBudget?> GetBudgetAsync(string tenantId, ResourcePoolType poolType, CancellationToken cancellationToken = default)
        {
            _budgets.TryGetValue(BudgetKey(tenantId, poolType), out var budget);
            return Task.FromResult(budget);
        }

        public Task<IReadOnlyList<TenantResourceBudget>> ListBudgetsForTenantAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            var list = _budgets.Values.Where(b => b.TenantId == tenantId).ToList();
            return Task.FromResult<IReadOnlyList<TenantResourceBudget>>(list);
        }

        public Task SaveRequestAsync(ResourceAllocationRequest request, CancellationToken cancellationToken = default)
        {
            _requests[request.RequestId] = request;
            return Task.CompletedTask;
        }

        public Task<ResourceAllocationRequest?> GetRequestAsync(string requestId, CancellationToken cancellationToken = default)
        {
            _requests.TryGetValue(requestId, out var req);
            return Task.FromResult(req);
        }
    }

    public sealed class BusinessResourceAllocationService : IBusinessResourceAllocationService
    {
        private readonly IBusinessResourceAllocationStore _store;

        public BusinessResourceAllocationService(IBusinessResourceAllocationStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public async Task<TenantResourceBudget> ConfigureBudgetAsync(
            string tenantId,
            ResourcePoolType poolType,
            decimal totalCapacity,
            decimal reserveFloorPct = 20.0m,
            CancellationToken cancellationToken = default)
        {
            var budget = new TenantResourceBudget
            {
                TenantId = tenantId,
                PoolType = poolType,
                TotalCapacity = totalCapacity,
                ReserveFloorPct = Math.Clamp(reserveFloorPct, 0m, 50m)
            };

            await _store.SaveBudgetAsync(budget, cancellationToken);
            return budget;
        }

        public async Task<ResourceAllocationRequest> RequestAllocationAsync(
            string tenantId,
            string missionId,
            ResourcePoolType poolType,
            decimal requestedAmount,
            AllocationPriority priority,
            decimal currentRunwayMonths = 12m,
            CancellationToken cancellationToken = default)
        {
            var request = new ResourceAllocationRequest
            {
                TenantId = tenantId,
                MissionId = missionId,
                PoolType = poolType,
                RequestedAmount = requestedAmount,
                Priority = priority
            };

            // Constitutional Runway Rule: If runway < 3 months, P2/P3 non-critical cash or compute expansion is throttled
            if (currentRunwayMonths < 3.0m && priority <= AllocationPriority.P2_Normal)
            {
                request.Reject(AllocationDecisionStatus.RejectedRunwayRisk, $"Runway is critically low ({currentRunwayMonths:F1} months < 3.0 months). Non-critical resource allocations throttled.");
                await _store.SaveRequestAsync(request, cancellationToken);
                return request;
            }

            var budget = await _store.GetBudgetAsync(tenantId, poolType, cancellationToken);
            if (budget == null)
            {
                request.Reject(AllocationDecisionStatus.RejectedExceedsBudget, $"No active budget configured for {poolType} under tenant '{tenantId}'.");
                await _store.SaveRequestAsync(request, cancellationToken);
                return request;
            }

            if (budget.TryAllocate(requestedAmount, priority, out string failureReason))
            {
                request.Approve();
                await _store.SaveBudgetAsync(budget, cancellationToken);
            }
            else
            {
                request.Reject(AllocationDecisionStatus.RejectedExceedsBudget, failureReason);
            }

            await _store.SaveRequestAsync(request, cancellationToken);
            return request;
        }

        public async Task ReleaseAllocationAsync(
            string requestId,
            CancellationToken cancellationToken = default)
        {
            var req = await _store.GetRequestAsync(requestId, cancellationToken);
            if (req == null) throw new KeyNotFoundException($"Allocation request '{requestId}' not found.");

            if (req.Status == AllocationDecisionStatus.Approved)
            {
                var budget = await _store.GetBudgetAsync(req.TenantId, req.PoolType, cancellationToken);
                if (budget != null)
                {
                    budget.Release(req.RequestedAmount);
                    await _store.SaveBudgetAsync(budget, cancellationToken);
                }

                req.MarkReleased();
                await _store.SaveRequestAsync(req, cancellationToken);
            }
        }
    }
}
