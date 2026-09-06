using System;
using System.Collections.Generic;
using BusinessModelApp.Core.Domain.Runtime;

namespace BusinessModelApp.Core.Domain.Responsibilities
{
    public record ResponsibilityMissionProposal
    {
        public Guid ProposalId { get; init; } = Guid.NewGuid();
        public ResponsibilityId ResponsibilityId { get; init; }
        public Guid WorkspaceId { get; init; }
        public string MissionType { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public string Rationale { get; init; } = string.Empty;
        public List<Guid> TriggerEventIds { get; init; } = new();
        public List<string> EvidenceSummaries { get; init; } = new();
        public ResponsibilityPriority Priority { get; init; } = ResponsibilityPriority.P2_Medium;
        public decimal SeverityScore { get; init; } = 1.0m;
        public AutonomyTier EffectiveAutonomyTier { get; init; } = AutonomyTier.L1_Advise;
        public List<string> RequiredCapabilities { get; init; } = new();
        public bool RequiresHumanApproval { get; init; } = true;
        public BrainRequestId? BrainRequestId { get; init; }
        public DateTime ProposedAtUtc { get; init; } = DateTime.UtcNow;
        public DateTime? DeadlineUtc { get; init; }
        public bool IsNoMissionOutcome { get; init; } = false;
        public string DispositionNotes { get; init; } = string.Empty;
    }
}
