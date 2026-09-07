using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Reality;
using BusinessModelApp.Core.Interfaces.Runtime.Reality;

namespace BusinessModelApp.Infrastructure.Runtime.Reality
{
    /// <summary>
    /// Governs live operational work monitoring, mission DAG execution tracking,
    /// and immutable step-by-step work ledgers.
    /// </summary>
    public sealed class WorkControlCenter : IWorkControlCenter
    {
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, MissionWorkLedger>> _tenantMissionLedgers = new();
        private readonly object _syncLock = new();

        public Task<WorkProgress> GetWorkProgressAsync(string tenantId)
        {
            if (string.IsNullOrWhiteSpace(tenantId) || !_tenantMissionLedgers.TryGetValue(tenantId, out var dict))
            {
                return Task.FromResult(new WorkProgress(
                    TenantId: tenantId ?? string.Empty,
                    TotalMissions: 0,
                    ActiveMissions: 0,
                    WaitingApprovalMissions: 0,
                    WaitingExternalMissions: 0,
                    RunningNodes: 0,
                    CompletedNodes: 0,
                    BlockedNodes: 0,
                    UnknownEffectNodes: 0,
                    FailedNodes: 0,
                    CompensatedNodes: 0,
                    MeasuredAt: DateTimeOffset.UtcNow
                ));
            }

            var ledgers = dict.Values.ToList();
            var totalMissions = ledgers.Count;
            var activeMissions = ledgers.Count(l => l.CurrentStatus == "Active" || l.CurrentStatus == "Running");
            var waitingApprovalMissions = ledgers.Count(l => l.CurrentStatus == "WaitingApproval");
            var waitingExternalMissions = ledgers.Count(l => l.CurrentStatus == "WaitingExternal");

            var allEntries = ledgers.SelectMany(l => l.Entries).ToList();
            var runningNodes = allEntries.Count(e => e.Status == "Active" || e.Status == "Running");
            var completedNodes = allEntries.Count(e => e.Status == "Completed");
            var blockedNodes = allEntries.Count(e => e.Status == "Blocked");
            var unknownEffectNodes = allEntries.Count(e => e.Status == "UnknownEffect");
            var failedNodes = allEntries.Count(e => e.Status == "Failed");
            var compensatedNodes = allEntries.Count(e => e.Status == "Compensated");

            return Task.FromResult(new WorkProgress(
                TenantId: tenantId,
                TotalMissions: totalMissions,
                ActiveMissions: activeMissions,
                WaitingApprovalMissions: waitingApprovalMissions,
                WaitingExternalMissions: waitingExternalMissions,
                RunningNodes: runningNodes,
                CompletedNodes: completedNodes,
                BlockedNodes: blockedNodes,
                UnknownEffectNodes: unknownEffectNodes,
                FailedNodes: failedNodes,
                CompensatedNodes: compensatedNodes,
                MeasuredAt: DateTimeOffset.UtcNow
            ));
        }

        public Task<MissionWorkLedger?> GetMissionWorkLedgerAsync(string tenantId, string missionId)
        {
            if (_tenantMissionLedgers.TryGetValue(tenantId, out var dict) &&
                dict.TryGetValue(missionId, out var ledger))
            {
                return Task.FromResult<MissionWorkLedger?>(ledger);
            }

            return Task.FromResult<MissionWorkLedger?>(null);
        }

        public Task<IReadOnlyList<MissionWorkLedger>> GetAllMissionLedgersAsync(string tenantId)
        {
            if (_tenantMissionLedgers.TryGetValue(tenantId, out var dict))
            {
                return Task.FromResult<IReadOnlyList<MissionWorkLedger>>(dict.Values.OrderByDescending(l => l.CreatedAt).ToList());
            }

            return Task.FromResult<IReadOnlyList<MissionWorkLedger>>(Array.Empty<MissionWorkLedger>());
        }

        public Task RegisterMissionLedgerAsync(MissionWorkLedger ledger)
        {
            if (ledger == null) throw new ArgumentNullException(nameof(ledger));
            var dict = _tenantMissionLedgers.GetOrAdd(ledger.TenantId, _ => new ConcurrentDictionary<string, MissionWorkLedger>());
            dict[ledger.MissionId] = ledger;
            return Task.CompletedTask;
        }

        public Task RecordMissionStepAsync(string tenantId, string missionId, MissionWorkLedgerEntry entry)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentException("TenantId required.", nameof(tenantId));
            if (string.IsNullOrWhiteSpace(missionId)) throw new ArgumentException("MissionId required.", nameof(missionId));
            if (entry == null) throw new ArgumentNullException(nameof(entry));

            var dict = _tenantMissionLedgers.GetOrAdd(tenantId, _ => new ConcurrentDictionary<string, MissionWorkLedger>());

            lock (_syncLock)
            {
                if (dict.TryGetValue(missionId, out var existing))
                {
                    var updatedEntries = new List<MissionWorkLedgerEntry>(existing.Entries) { entry };
                    var completedCount = updatedEntries.Count(e => e.Status == "Completed");
                    var updatedStatus = entry.Status == "WaitingApproval" ? "WaitingApproval" : existing.CurrentStatus;

                    dict[missionId] = existing with
                    {
                        CurrentStatus = updatedStatus,
                        CompletedNodes = completedCount,
                        Entries = updatedEntries
                    };
                }
                else
                {
                    dict[missionId] = new MissionWorkLedger(
                        MissionId: missionId,
                        MissionName: $"Mission {missionId}",
                        TenantId: tenantId,
                        Objective: "Autonomous Execution",
                        CreatedAt: DateTimeOffset.UtcNow,
                        CurrentStatus: entry.Status,
                        TotalNodes: 1,
                        CompletedNodes: entry.Status == "Completed" ? 1 : 0,
                        Entries: new List<MissionWorkLedgerEntry> { entry }
                    );
                }
            }

            return Task.CompletedTask;
        }
    }
}
