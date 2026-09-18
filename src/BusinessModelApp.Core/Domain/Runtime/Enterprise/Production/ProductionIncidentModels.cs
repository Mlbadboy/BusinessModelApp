using System;

namespace BusinessModelApp.Core.Domain.Runtime.Enterprise.Production
{
    public enum ProductionIncidentSeverity
    {
        Info = 0,
        Warning = 1,
        Error = 2,
        Critical = 3,
        Emergency = 4
    }

    public sealed class ProductionKillSwitchState
    {
        public string TenantId { get; init; } = string.Empty;
        public bool IsArmed { get; private set; } = true;
        public bool IsTriggered { get; private set; }
        public string TriggeredByAuthority { get; private set; } = string.Empty;
        public string TriggerReason { get; private set; } = string.Empty;
        public DateTime? TriggeredAtUtc { get; private set; }

        public void Trigger(string authority, string reason)
        {
            if (string.IsNullOrWhiteSpace(authority)) throw new ArgumentException("Trigger authority required.", nameof(authority));
            IsTriggered = true;
            TriggeredByAuthority = authority.Trim();
            TriggerReason = reason.Trim();
            TriggeredAtUtc = DateTime.UtcNow;
        }

        public void Reset(string authority)
        {
            if (string.IsNullOrWhiteSpace(authority)) throw new ArgumentException("Reset authority required.", nameof(authority));
            IsTriggered = false;
            TriggeredByAuthority = string.Empty;
            TriggerReason = string.Empty;
            TriggeredAtUtc = null;
        }
    }

    public sealed class ProductionIncident
    {
        public string IncidentId { get; init; } = Guid.NewGuid().ToString("N");
        public required string TenantId { get; init; }
        public required string ComponentName { get; init; }
        public ProductionIncidentSeverity Severity { get; init; } = ProductionIncidentSeverity.Warning;
        public string Description { get; init; } = string.Empty;
        public string ErrorDetails { get; init; } = string.Empty;
        public bool IsResolved { get; private set; }
        public string ResolutionNotes { get; private set; } = string.Empty;
        public DateTime ReportedAtUtc { get; init; } = DateTime.UtcNow;
        public DateTime? ResolvedAtUtc { get; private set; }

        public void Resolve(string notes)
        {
            IsResolved = true;
            ResolutionNotes = notes.Trim();
            ResolvedAtUtc = DateTime.UtcNow;
        }
    }
}
