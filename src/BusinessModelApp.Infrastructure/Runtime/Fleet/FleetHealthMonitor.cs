using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime;
using BusinessModelApp.Core.Domain.Runtime.Fleet;
using BusinessModelApp.Core.Interfaces.Runtime.Fleet;

namespace BusinessModelApp.Infrastructure.Runtime.Fleet
{
    public class FleetHealthMonitor : IFleetHealthMonitor
    {
        private readonly IAgentFleetStore _fleetStore;
        private readonly ConcurrentDictionary<Guid, WorkerHeartbeat> _latestHeartbeats = new();

        public const int MaxAllowedFailuresBeforeQuarantine = 3;

        public FleetHealthMonitor(IAgentFleetStore fleetStore)
        {
            _fleetStore = fleetStore ?? throw new ArgumentNullException(nameof(fleetStore));
        }

        public Task RecordHeartbeatAsync(WorkerHeartbeat heartbeat, CancellationToken ct = default)
        {
            if (heartbeat == null) throw new ArgumentNullException(nameof(heartbeat));

            _latestHeartbeats[heartbeat.WorkerId.Value] = heartbeat;
            return Task.CompletedTask;
        }

        public async Task<IReadOnlyList<WorkerProcessRecord>> CheckHealthAsync(TimeSpan timeoutThreshold, CancellationToken ct = default)
        {
            var now = DateTimeOffset.UtcNow;
            var unhealthyWorkers = new List<WorkerProcessRecord>();

            var workers = await _fleetStore.GetAllWorkersAsync(ct);
            foreach (var worker in workers)
            {
                if (worker.HealthStatus == WorkerHealthStatus.Terminated)
                    continue;

                // Check heartbeat freshness
                if (_latestHeartbeats.TryGetValue(worker.WorkerId.Value, out var hb))
                {
                    if (now - hb.TimestampUtc > timeoutThreshold)
                    {
                        worker.CrashCount++;
                        worker.ConsecutiveFailures++;
                        worker.HealthStatus = WorkerHealthStatus.Degraded;

                        if (worker.ConsecutiveFailures >= MaxAllowedFailuresBeforeQuarantine)
                        {
                            await QuarantineWorkerAsync(worker.WorkerId, "Heartbeat timeout threshold exceeded repeatedly.", ct);
                        }

                        unhealthyWorkers.Add(worker);
                    }
                }
            }

            return unhealthyWorkers;
        }

        public async Task QuarantineWorkerAsync(WorkerProcessId workerId, string reason, CancellationToken ct = default)
        {
            var worker = await _fleetStore.GetWorkerAsync(workerId, ct);
            if (worker != null)
            {
                worker.HealthStatus = WorkerHealthStatus.Quarantined;
                worker.QuarantineReason = reason;
                worker.QuarantinedAt = DateTimeOffset.UtcNow;
            }
        }
    }
}
