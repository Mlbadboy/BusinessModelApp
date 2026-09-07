using System;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Workers;
using BusinessModelApp.Core.Interfaces.Runtime.Workers;

namespace BusinessModelApp.Infrastructure.Runtime.Workers
{
    public class WorkerHealthManager : IWorkerHealthManager
    {
        private readonly IWorkerStore _store;

        public WorkerHealthManager(IWorkerStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public async Task<WorkerHealthSnapshot> GetHealthAsync(WorkerDefinitionId definitionId, WorkerModality modality, CancellationToken ct = default)
        {
            var snapshot = await _store.GetHealthSnapshotAsync(definitionId, ct);
            if (snapshot != null) return snapshot;

            var newSnapshot = new WorkerHealthSnapshot
            {
                WorkerDefinitionId = definitionId,
                Modality = modality,
                CircuitState = WorkerCircuitState.Healthy
            };
            await _store.SaveHealthSnapshotAsync(newSnapshot, ct);
            return newSnapshot;
        }

        public async Task RecordExecutionResultAsync(
            WorkerDefinitionId definitionId,
            WorkerModality modality,
            bool isSuccess,
            bool isCrash,
            bool isTimeout,
            bool isUnknownEffect,
            TimeSpan duration,
            CancellationToken ct = default)
        {
            var health = await GetHealthAsync(definitionId, modality, ct);

            // Once quarantined for security reasons, it cannot be automatically un-quarantined
            if (health.CircuitState == WorkerCircuitState.Quarantined)
                return;

            var metrics = health.Metrics;
            metrics.TotalExecutions++;

            if (isSuccess) metrics.SuccessCount++;
            if (isCrash) metrics.CrashCount++;
            if (isTimeout) metrics.TimeoutCount++;
            if (isUnknownEffect) metrics.UnknownEffectCount++;

            // Update circuit state based on failure rates
            if (metrics.FailureRate >= 0.50 && metrics.TotalExecutions >= 4)
            {
                health.CircuitState = WorkerCircuitState.CircuitOpen;
                health.LastStateChangedAt = DateTimeOffset.UtcNow;
            }
            else if (metrics.FailureRate >= 0.25 && metrics.TotalExecutions >= 4)
            {
                health.CircuitState = WorkerCircuitState.Degraded;
                health.LastStateChangedAt = DateTimeOffset.UtcNow;
            }
            else if (health.CircuitState == WorkerCircuitState.CircuitOpen && isSuccess)
            {
                health.CircuitState = WorkerCircuitState.Recovering;
                health.LastStateChangedAt = DateTimeOffset.UtcNow;
            }
            else if (metrics.FailureRate < 0.25)
            {
                health.CircuitState = WorkerCircuitState.Healthy;
            }

            await _store.SaveHealthSnapshotAsync(health, ct);
        }

        public async Task QuarantineWorkerAsync(WorkerDefinitionId definitionId, string reason, CancellationToken ct = default)
        {
            var health = await _store.GetHealthSnapshotAsync(definitionId, ct) ?? new WorkerHealthSnapshot
            {
                WorkerDefinitionId = definitionId,
                Modality = WorkerModality.Api
            };

            health.CircuitState = WorkerCircuitState.Quarantined;
            health.QuarantineReason = reason;
            health.LastStateChangedAt = DateTimeOffset.UtcNow;
            health.Metrics.SandboxViolations++;

            await _store.SaveHealthSnapshotAsync(health, ct);
        }
    }
}
