using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime;
using BusinessModelApp.Core.Interfaces.Missions;

namespace BusinessModelApp.Infrastructure.Runtime.Missions
{
    public class MissionGraphAuditLedger : IMissionGraphAuditLedger
    {
        private readonly ConcurrentDictionary<Guid, List<MissionGraphAuditEntry>> _ledger = new();

        public Task RecordEventAsync(MissionGraphAuditEntry entry, CancellationToken ct = default)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));

            _ledger.AddOrUpdate(
                entry.GraphId.Value,
                new List<MissionGraphAuditEntry> { entry },
                (_, list) =>
                {
                    lock (list)
                    {
                        list.Add(entry);
                    }
                    return list;
                });

            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<MissionGraphAuditEntry>> GetEntriesAsync(MissionGraphId graphId, CancellationToken ct = default)
        {
            if (_ledger.TryGetValue(graphId.Value, out var list))
            {
                lock (list)
                {
                    return Task.FromResult<IReadOnlyList<MissionGraphAuditEntry>>(list.ToList());
                }
            }

            return Task.FromResult<IReadOnlyList<MissionGraphAuditEntry>>(Array.Empty<MissionGraphAuditEntry>());
        }
    }
}
