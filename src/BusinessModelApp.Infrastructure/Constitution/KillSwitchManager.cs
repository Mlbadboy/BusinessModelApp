using System;
using System.Collections.Concurrent;
using BusinessModelApp.Core.Constitution;

namespace BusinessModelApp.Infrastructure.Constitution
{
    public class KillSwitchManager : IKillSwitchManager
    {
        private readonly ConcurrentDictionary<string, KillSwitchRecord> _activeKillSwitches = new(StringComparer.OrdinalIgnoreCase);

        public void Trip(KillSwitchScope scope, string targetId, string reason, string operatorId)
        {
            if (string.IsNullOrWhiteSpace(targetId)) throw new ArgumentNullException(nameof(targetId));

            var record = new KillSwitchRecord
            {
                Scope = scope,
                TargetId = targetId,
                IsTripped = true,
                Reason = reason,
                TrippedBy = operatorId,
                TrippedAtUtc = DateTime.UtcNow
            };

            _activeKillSwitches[record.KillSwitchKey] = record;
        }

        public void Reset(KillSwitchScope scope, string targetId)
        {
            var key = $"{scope}:{targetId}".ToUpperInvariant();
            _activeKillSwitches.TryRemove(key, out _);
        }

        public bool IsHalted(string tenantId, Guid? missionId, string? department, string? agentId, string? toolId)
        {
            // 1. Global Kill Switch Check
            if (_activeKillSwitches.ContainsKey($"{KillSwitchScope.Global}:*"))
            {
                return true;
            }

            // 2. Tenant Kill Switch Check
            if (!string.IsNullOrEmpty(tenantId) && _activeKillSwitches.ContainsKey($"{KillSwitchScope.Tenant}:{tenantId}"))
            {
                return true;
            }

            // 3. Mission Kill Switch Check
            if (missionId.HasValue && _activeKillSwitches.ContainsKey($"{KillSwitchScope.Mission}:{missionId.Value}"))
            {
                return true;
            }

            // 4. Department Kill Switch Check
            if (!string.IsNullOrEmpty(department) && _activeKillSwitches.ContainsKey($"{KillSwitchScope.Department}:{department}"))
            {
                return true;
            }

            // 5. Agent Kill Switch Check
            if (!string.IsNullOrEmpty(agentId) && _activeKillSwitches.ContainsKey($"{KillSwitchScope.Agent}:{agentId}"))
            {
                return true;
            }

            // 6. Tool Kill Switch Check
            if (!string.IsNullOrEmpty(toolId) && _activeKillSwitches.ContainsKey($"{KillSwitchScope.Tool}:{toolId}"))
            {
                return true;
            }

            return false;
        }
    }
}
