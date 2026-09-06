using System;

namespace BusinessModelApp.Core.Domain.Runtime
{
    public record AdmissionRequest
    {
        public RuntimeRunId RunId { get; init; }
        public Guid WorkspaceId { get; init; }
        public ResponsibilityId? ResponsibilityId { get; init; }
        public MissionGraphId MissionGraphId { get; init; }
        public int Priority { get; init; } = 5; // 1 (Highest) to 10 (Lowest)
        public RunResourceBudget RequestedBudget { get; init; } = RunResourceBudget.Default;
        public string RequesterId { get; init; } = string.Empty;

        public AdmissionRequest(RuntimeRunId runId, Guid workspaceId, MissionGraphId missionGraphId)
        {
            if (workspaceId == Guid.Empty)
                throw new ArgumentException("WorkspaceId cannot be empty.", nameof(workspaceId));

            RunId = runId;
            WorkspaceId = workspaceId;
            MissionGraphId = missionGraphId;
        }
    }

    public record AdmissionDecision
    {
        public bool IsAdmitted { get; init; }
        public string? RejectionReason { get; init; }
        public RunResourceBudget AllocatedBudget { get; init; } = RunResourceBudget.Default;
        public int AssignedPriority { get; init; } = 5;
        public DateTime AdmittedAtUtc { get; init; } = DateTime.UtcNow;

        public static AdmissionDecision Admitted(RunResourceBudget budget, int priority = 5) => new()
        {
            IsAdmitted = true,
            AllocatedBudget = budget,
            AssignedPriority = priority
        };

        public static AdmissionDecision Denied(string reason) => new()
        {
            IsAdmitted = false,
            RejectionReason = reason
        };
    }
}
