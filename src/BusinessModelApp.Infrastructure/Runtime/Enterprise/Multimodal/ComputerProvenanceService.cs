using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Multimodal;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Multimodal;

namespace BusinessModelApp.Infrastructure.Runtime.Enterprise.Multimodal
{
    public class ComputerProvenanceService : IComputerProvenanceService
    {
        private readonly ConcurrentDictionary<string, ComputerTraceRecord> _traces = new();
        private readonly ConcurrentDictionary<string, List<string>> _sessionTraces = new();

        public Task<ComputerTraceRecord> RecordTraceAsync(string tenantId, string sessionId, string proposalId, string attemptId, string externalEffect, string outcomeId)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentException("TenantId is required", nameof(tenantId));

            var record = new ComputerTraceRecord
            {
                TenantId = tenantId,
                ComputerSessionId = sessionId,
                ActionProposalId = proposalId,
                ExecutionAttemptId = attemptId,
                ExternalEffect = externalEffect ?? "None",
                OutcomeId = outcomeId ?? string.Empty,
                TimestampUtc = DateTime.UtcNow
            };

            record.TraceHash = record.ComputeTraceHash();
            _traces[record.TraceId] = record;

            _sessionTraces.AddOrUpdate(sessionId,
                _ => new List<string> { record.TraceId },
                (_, list) =>
                {
                    lock (list) { list.Add(record.TraceId); }
                    return list;
                });

            return Task.FromResult(record);
        }

        public Task<ComputerTraceRecord?> GetTraceAsync(string tenantId, string traceId)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentException("TenantId is required", nameof(tenantId));

            if (_traces.TryGetValue(traceId, out var trace))
            {
                if (trace.TenantId != tenantId)
                {
                    throw new UnauthorizedAccessException($"Tenant penetration defense: trace {traceId} belongs to another tenant");
                }
                return Task.FromResult<ComputerTraceRecord?>(trace);
            }

            return Task.FromResult<ComputerTraceRecord?>(null);
        }

        public Task<ReplaySessionDescriptor> CreateReplaySessionAsync(string tenantId, string originalSessionId)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentException("TenantId is required", nameof(tenantId));

            int count = 0;
            if (_sessionTraces.TryGetValue(originalSessionId, out var traces))
            {
                lock (traces) { count = traces.Count; }
            }

            var descriptor = new ReplaySessionDescriptor
            {
                OriginalSessionId = originalSessionId,
                ReplaySessionId = Guid.NewGuid().ToString("N"),
                TenantId = tenantId,
                SnapshotCount = count,
                DeterministicHash = ComputerEnvironmentSnapshot.ComputeSha256($"{tenantId}:{originalSessionId}:{count}:replay"),
                ReplayedAtUtc = DateTime.UtcNow,
                IsOriginalPreserved = true
            };

            return Task.FromResult(descriptor);
        }
    }
}
