using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime;
using BusinessModelApp.Core.Interfaces.Runtime;

namespace BusinessModelApp.Infrastructure.Runtime
{
    public class RuntimeAdmissionController : IRuntimeAdmissionController
    {
        private readonly ConcurrentDictionary<Guid, int> _activeRunsPerTenant = new();
        public const int MaxConcurrentRunsPerTenant = 10;
        public const decimal MaxTenantFinancialCeiling = 10_000.00m;

        public Task<AdmissionDecision> EvaluateAdmissionAsync(
            AdmissionRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            // 1. Tenant Valid
            if (request.WorkspaceId == Guid.Empty)
            {
                return Task.FromResult(AdmissionDecision.Denied("Admission rejected: Missing or empty WorkspaceId."));
            }

            // 2. Concurrency quota check
            int currentActive = _activeRunsPerTenant.GetOrAdd(request.WorkspaceId, 0);
            if (currentActive >= MaxConcurrentRunsPerTenant)
            {
                return Task.FromResult(AdmissionDecision.Denied(
                    $"Admission rejected: Tenant concurrency limit ({MaxConcurrentRunsPerTenant}) reached."));
            }

            // 3. Budget guard check
            if (request.RequestedBudget.MaxFinancialExposure > MaxTenantFinancialCeiling)
            {
                return Task.FromResult(AdmissionDecision.Denied(
                    $"Admission rejected: Requested financial exposure ({request.RequestedBudget.MaxFinancialExposure:C}) exceeds tenant ceiling ({MaxTenantFinancialCeiling:C})."));
            }

            // Admit run
            _activeRunsPerTenant.AddOrUpdate(request.WorkspaceId, 1, (_, c) => c + 1);
            return Task.FromResult(AdmissionDecision.Admitted(request.RequestedBudget, request.Priority));
        }

        public void RecordRunCompleted(RuntimeRunId runId)
        {
            // Decrement active runs if tracked
        }

        public void ReleaseTenantCapacity(Guid workspaceId)
        {
            _activeRunsPerTenant.AddOrUpdate(workspaceId, 0, (_, c) => Math.Max(0, c - 1));
        }

        public int GetActiveRunCount(Guid workspaceId)
        {
            return _activeRunsPerTenant.TryGetValue(workspaceId, out var count) ? count : 0;
        }
    }
}
