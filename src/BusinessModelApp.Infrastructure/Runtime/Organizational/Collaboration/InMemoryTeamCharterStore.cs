using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Organizational.Collaboration;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational.Collaboration
{
    public class InMemoryTeamCharterStore : ITeamCharterStore
    {
        private readonly ConcurrentDictionary<string, TeamCharter> _charters = new();
        private readonly ConcurrentDictionary<string, List<CollaborationMessage>> _messages = new();
        private readonly ConcurrentDictionary<string, List<DisputeRecord>> _disputes = new();
        private readonly ConcurrentDictionary<string, TeamPerformanceRecord> _performance = new();

        private static string Key(string tenantId, string id) => $"{tenantId}::{id}";

        public Task SaveCharterAsync(TeamCharter charter, CancellationToken ct = default)
        {
            if (charter == null) throw new ArgumentNullException(nameof(charter));
            if (string.IsNullOrWhiteSpace(charter.TenantId)) throw new ArgumentException("TenantId is required.");
            if (string.IsNullOrWhiteSpace(charter.CharterId)) throw new ArgumentException("CharterId is required.");

            _charters[Key(charter.TenantId, charter.CharterId)] = charter;
            return Task.CompletedTask;
        }

        public Task<TeamCharter?> GetCharterAsync(string tenantId, string charterId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(charterId))
                return Task.FromResult<TeamCharter?>(null);

            _charters.TryGetValue(Key(tenantId, charterId), out var charter);
            return Task.FromResult(charter);
        }

        public Task<IReadOnlyList<TeamCharter>> ListChartersForWorkAsync(string tenantId, string workId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(workId))
                return Task.FromResult<IReadOnlyList<TeamCharter>>(Array.Empty<TeamCharter>());

            var results = _charters.Values
                .Where(c => c.TenantId == tenantId && c.WorkId == workId)
                .OrderByDescending(c => c.FormedUtc)
                .ToList();

            return Task.FromResult<IReadOnlyList<TeamCharter>>(results);
        }

        public Task<IReadOnlyList<TeamCharter>> ListActiveChartersAsync(string tenantId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                return Task.FromResult<IReadOnlyList<TeamCharter>>(Array.Empty<TeamCharter>());

            var results = _charters.Values
                .Where(c => c.TenantId == tenantId &&
                            (c.Status == TeamLifecycleStatus.Operating ||
                             c.Status == TeamLifecycleStatus.Assembling ||
                             c.Status == TeamLifecycleStatus.Forming ||
                             c.Status == TeamLifecycleStatus.Disputed))
                .OrderByDescending(c => c.FormedUtc)
                .ToList();

            return Task.FromResult<IReadOnlyList<TeamCharter>>(results);
        }

        public Task SaveMessageAsync(CollaborationMessage message, CancellationToken ct = default)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            if (string.IsNullOrWhiteSpace(message.TenantId)) throw new ArgumentException("TenantId is required.");
            if (string.IsNullOrWhiteSpace(message.CharterId)) throw new ArgumentException("CharterId is required.");

            var key = Key(message.TenantId, message.CharterId);
            _messages.AddOrUpdate(
                key,
                _ => new List<CollaborationMessage> { message },
                (_, list) =>
                {
                    lock (list)
                    {
                        list.Add(message);
                    }
                    return list;
                });

            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<CollaborationMessage>> ListMessagesAsync(string tenantId, string charterId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(charterId))
                return Task.FromResult<IReadOnlyList<CollaborationMessage>>(Array.Empty<CollaborationMessage>());

            var key = Key(tenantId, charterId);
            if (_messages.TryGetValue(key, out var list))
            {
                lock (list)
                {
                    return Task.FromResult<IReadOnlyList<CollaborationMessage>>(list.OrderBy(m => m.TurnNumber).ToList());
                }
            }

            return Task.FromResult<IReadOnlyList<CollaborationMessage>>(Array.Empty<CollaborationMessage>());
        }

        public Task SaveDisputeAsync(DisputeRecord dispute, CancellationToken ct = default)
        {
            if (dispute == null) throw new ArgumentNullException(nameof(dispute));
            if (string.IsNullOrWhiteSpace(dispute.TenantId)) throw new ArgumentException("TenantId is required.");
            if (string.IsNullOrWhiteSpace(dispute.CharterId)) throw new ArgumentException("CharterId is required.");

            var key = Key(dispute.TenantId, dispute.CharterId);
            _disputes.AddOrUpdate(
                key,
                _ => new List<DisputeRecord> { dispute },
                (_, list) =>
                {
                    lock (list)
                    {
                        list.Add(dispute);
                    }
                    return list;
                });

            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<DisputeRecord>> ListDisputesAsync(string tenantId, string charterId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(charterId))
                return Task.FromResult<IReadOnlyList<DisputeRecord>>(Array.Empty<DisputeRecord>());

            var key = Key(tenantId, charterId);
            if (_disputes.TryGetValue(key, out var list))
            {
                lock (list)
                {
                    return Task.FromResult<IReadOnlyList<DisputeRecord>>(list.OrderByDescending(d => d.ResolvedUtc).ToList());
                }
            }

            return Task.FromResult<IReadOnlyList<DisputeRecord>>(Array.Empty<DisputeRecord>());
        }

        public Task SavePerformanceRecordAsync(TeamPerformanceRecord record, CancellationToken ct = default)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));
            _performance[Key(record.TenantId, record.CharterId)] = record;
            return Task.CompletedTask;
        }

        public Task<TeamPerformanceRecord?> GetPerformanceRecordAsync(string tenantId, string charterId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(charterId))
                return Task.FromResult<TeamPerformanceRecord?>(null);

            _performance.TryGetValue(Key(tenantId, charterId), out var record);
            return Task.FromResult(record);
        }
    }
}
