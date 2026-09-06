using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime;

namespace BusinessModelApp.Infrastructure.Runtime
{
    public record RuntimeAuditRecord
    {
        public Guid AuditId { get; init; } = Guid.NewGuid();
        public Guid WorkspaceId { get; init; }
        public RuntimeRunId RunId { get; init; }
        public ExecutionAttemptId? AttemptId { get; init; }
        public string ActionType { get; init; } = string.Empty;
        public string PreviousState { get; init; } = string.Empty;
        public string NewState { get; init; } = string.Empty;
        public string PolicySnapshotId { get; init; } = string.Empty;
        public Guid CorrelationId { get; init; }
        public Guid CausationId { get; init; }
        public string ActorId { get; init; } = string.Empty;
        public string RecordHash { get; init; } = string.Empty;
        public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;

        public static RuntimeAuditRecord Create(
            Guid workspaceId,
            RuntimeRunId runId,
            ExecutionAttemptId? attemptId,
            string actionType,
            string previousState,
            string newState,
            string policySnapshotId,
            Guid correlationId,
            Guid causationId,
            string actorId)
        {
            var raw = $"{workspaceId}:{runId}:{attemptId}:{actionType}:{previousState}:{newState}:{policySnapshotId}:{correlationId}:{causationId}:{actorId}";
            var hash = RuntimeEventEnvelope.ComputeSha256(raw);

            return new RuntimeAuditRecord
            {
                WorkspaceId = workspaceId,
                RunId = runId,
                AttemptId = attemptId,
                ActionType = actionType,
                PreviousState = previousState,
                NewState = newState,
                PolicySnapshotId = policySnapshotId,
                CorrelationId = correlationId,
                CausationId = causationId,
                ActorId = actorId,
                RecordHash = hash,
                TimestampUtc = DateTime.UtcNow
            };
        }
    }

    public interface IRuntimeAuditStore
    {
        Task RecordAuditAsync(RuntimeAuditRecord record, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<RuntimeAuditRecord>> GetAuditHistoryAsync(RuntimeRunId runId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<RuntimeAuditRecord>> GetTenantAuditHistoryAsync(Guid workspaceId, int limit = 100, CancellationToken cancellationToken = default);
    }

    public class RuntimeAuditStore : IRuntimeAuditStore
    {
        private readonly ConcurrentDictionary<Guid, List<RuntimeAuditRecord>> _runAudits = new();
        private readonly ConcurrentDictionary<Guid, List<RuntimeAuditRecord>> _tenantAudits = new();
        private readonly object _lock = new();

        public Task RecordAuditAsync(RuntimeAuditRecord record, CancellationToken cancellationToken = default)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));

            lock (_lock)
            {
                var runList = _runAudits.GetOrAdd(record.RunId.Value, _ => new List<RuntimeAuditRecord>());
                runList.Add(record);

                var tenantList = _tenantAudits.GetOrAdd(record.WorkspaceId, _ => new List<RuntimeAuditRecord>());
                tenantList.Add(record);
            }

            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<RuntimeAuditRecord>> GetAuditHistoryAsync(RuntimeRunId runId, CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                if (!_runAudits.TryGetValue(runId.Value, out var list))
                    return Task.FromResult<IReadOnlyList<RuntimeAuditRecord>>(Array.Empty<RuntimeAuditRecord>());

                return Task.FromResult<IReadOnlyList<RuntimeAuditRecord>>(list.OrderBy(a => a.TimestampUtc).ToList());
            }
        }

        public Task<IReadOnlyList<RuntimeAuditRecord>> GetTenantAuditHistoryAsync(Guid workspaceId, int limit = 100, CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                if (!_tenantAudits.TryGetValue(workspaceId, out var list))
                    return Task.FromResult<IReadOnlyList<RuntimeAuditRecord>>(Array.Empty<RuntimeAuditRecord>());

                var result = list.OrderByDescending(a => a.TimestampUtc).Take(limit).ToList();
                return Task.FromResult<IReadOnlyList<RuntimeAuditRecord>>(result);
            }
        }
    }
}
