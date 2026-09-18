using System;

namespace BusinessModelApp.Core.Domain.Runtime.Enterprise.Recovery
{
    public enum IncidentSeverity
    {
        Minor = 0,
        Moderate = 1,
        Major = 2,
        Critical = 3
    }

    public enum RecoveryStrategy
    {
        RetryWithBackoff = 0,
        CompensateAndRollback = 1,
        ReconcileExternalEffect = 2,
        TerminateAndAlert = 3
    }

    public enum IncidentStatus
    {
        Detected = 0,
        Investigating = 1,
        Reconciled = 2,
        Compensated = 3,
        Resolved = 4,
        Failed = 5
    }

    public sealed class BusinessIncident
    {
        public string IncidentId { get; init; } = Guid.NewGuid().ToString("N");
        public required string TenantId { get; init; }
        public required string SourceComponent { get; init; }
        public required string ErrorCode { get; init; }
        public IncidentSeverity Severity { get; init; }
        public required string RootCause { get; init; }
        public string UnknownEffectToken { get; init; } = string.Empty;
        public RecoveryStrategy RecommendedStrategy { get; private set; }
        public IncidentStatus Status { get; private set; } = IncidentStatus.Detected;
        public string ResolutionNotes { get; private set; } = string.Empty;
        public DateTime DetectedAtUtc { get; init; } = DateTime.UtcNow;
        public DateTime? ResolvedAtUtc { get; private set; }

        public void AssignStrategy(RecoveryStrategy strategy)
        {
            RecommendedStrategy = strategy;
            Status = IncidentStatus.Investigating;
        }

        public void MarkReconciled(string reconciliationDetails)
        {
            Status = IncidentStatus.Reconciled;
            ResolutionNotes = $"Reconciled: {reconciliationDetails}";
        }

        public void MarkCompensated(string compensationDetails)
        {
            Status = IncidentStatus.Compensated;
            ResolutionNotes = $"Compensated: {compensationDetails}";
        }

        public void Resolve(string finalNotes)
        {
            Status = IncidentStatus.Resolved;
            ResolutionNotes = string.IsNullOrWhiteSpace(ResolutionNotes) ? finalNotes : $"{ResolutionNotes} | {finalNotes}";
            ResolvedAtUtc = DateTime.UtcNow;
        }
    }
}
