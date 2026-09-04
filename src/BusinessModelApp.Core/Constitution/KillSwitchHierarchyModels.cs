using System;

namespace BusinessModelApp.Core.Constitution
{
    public enum KillSwitchScope
    {
        Global = 1,
        Tenant = 2,
        Mission = 3,
        Department = 4,
        Agent = 5,
        Tool = 6
    }

    public class KillSwitchRecord
    {
        public string KillSwitchKey => $"{Scope}:{TargetId}".ToUpperInvariant();
        public KillSwitchScope Scope { get; set; }
        public string TargetId { get; set; } = string.Empty;
        public bool IsTripped { get; set; } = true;
        public string TrippedBy { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public DateTime TrippedAtUtc { get; set; } = DateTime.UtcNow;
    }

    public interface IKillSwitchManager
    {
        void Trip(KillSwitchScope scope, string targetId, string reason, string operatorId);
        void Reset(KillSwitchScope scope, string targetId);
        bool IsHalted(string tenantId, Guid? missionId, string? department, string? agentId, string? toolId);
    }
}
