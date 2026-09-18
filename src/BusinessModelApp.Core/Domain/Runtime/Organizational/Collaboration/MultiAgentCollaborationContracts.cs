using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace BusinessModelApp.Core.Domain.Runtime.Organizational.Collaboration
{
    /// <summary>
    /// Constitutional Invariant I29: Multi-Agent Collaboration & Team Formation Sovereignty
    /// COLLABORATION ≠ SWARM AUTONOMY ≠ AUTHORITY POOLING ≠ PEER DELEGATION ≠ TRUTH ≠ POLICY MUTATION ≠ GOVERNANCE ≠ EXECUTION PERMIT
    /// </summary>
    public static class MultiAgentCollaborationSovereignty
    {
        public const string InvariantName = "I29";
        public const string InvariantStatement =
            "COLLABORATION ≠ SWARM AUTONOMY ≠ AUTHORITY POOLING ≠ PEER DELEGATION ≠ TRUTH ≠ POLICY MUTATION ≠ GOVERNANCE ≠ EXECUTION PERMIT";

        public const string I29_A_NoUncontrolledSwarms =
            "I29-A: Team formation is strictly bounded by explicit Team Charters, objective scopes, role definitions, and resource budgets. Open-ended or self-replicating swarms are structurally prohibited.";

        public const string I29_B_NoAuthorityPooling =
            "I29-B: Individual agent authority cannot be combined, pooled, or escalated to authorize actions that no individual agent (or the team charter) has authority to perform.";

        public const string I29_C_NoPeerSpawningOrDelegation =
            "I29-C: Agents cannot unilaterally spawn subordinate agents, instantiate unvetted workers, or dynamically create child authority trees without governed charter compilation.";

        public const string I29_D_StructuredAuditableCommunication =
            "I29-D: Cross-agent communication must use typed, auditable, immutable message contracts with provenance hashes. Unlogged private side-channels and infinite debate loops are prohibited.";

        public const string I29_E_DeterministicDisputeArbitration =
            "I29-E: Disagreements among collaborative agents are resolved via deterministic arbitration: Evidence > Policy/Constraint Dominance > Reputation/Specialization > PRG-1 Escalation.";

        public const string I29_F_EphemeralLifecycleAndDissolution =
            "I29-F: Teams are ephemeral, goal-oriented entities. Upon completion, failure, timeout, or cancellation, the team dissolves and releases all locks, resources, and leases.";

        public const string I29_G_BoundedTeamSizeAndInteractionDepth =
            "I29-G: Hard ceilings: Max team size <= 5 agents, max negotiation turns <= 10 turns, bounded concurrent teams per tenant.";

        public const string I29_H_MultiTenantPartitioning =
            "I29-H: Collaboration messages, team charters, dispute arbitration, and shared work items are strictly partitioned by TenantId.";

        public const string I29_I_ConsensusNotTruth =
            "I29-I: Multi-agent consensus (majority vote or unanimous agreement) never converts a hypothesis or ungrounded assertion into empirical truth.";

        public const string I29_J_FirewallIsolation =
            "I29-J: Collaborative teams cannot issue ExecutionPermits or call external worker connectors directly. All consequential actions must proceed through Batch 6 Firewall.";

        public const string I29_K_CollaborationMetrology =
            "I29-K: Team collaboration outcomes feed back into Empirical Reputation and Team Composition Metrology without altering truth or bypassing governance.";

        public const string I29_L_DeterministicReplayability =
            "I29-L: All team formation steps, negotiation messages, and dispute resolutions must be cryptographically hashed and deterministically replayable.";

        public const string I29_M_RoleSpecializationBoundaries =
            "I29-M: Agents operate strictly within declared capability domains and cannot unilaterally assume arbitrary functional responsibilities.";

        public const string I29_N_TeamFormationSovereignty =
            "I29-N: Only authorized Work, Responsibility, or Mission Orchestrator triggers team formation; agents cannot spontaneously self-organize without charter.";
    }

    public enum TeamLifecycleStatus
    {
        Forming,
        Assembling,
        Operating,
        Disputed,
        Dissolving,
        Dissolved,
        TimedOut
    }

    public enum CollaborationMessageType
    {
        InformationSharing,
        ProposalOffer,
        ProposalCounter,
        WorkHandoff,
        DisputeRaised,
        DisputeConcession,
        StatusUpdate
    }

    public enum DisputeType
    {
        ConflictingHypothesis,
        ResourceContention,
        PriorityClash,
        RoleBoundaryOverlap,
        PolicyInterpretation
    }

    public enum ArbitrationOutcome
    {
        EvidencePrevails,
        PolicyPrevails,
        ReputationPrevails,
        SplitCompromise,
        EscalatedToGovernance
    }

    public class TeamMemberRole
    {
        public string AgentInstanceId { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public string SpecializationDomain { get; set; } = string.Empty;
        public int MaxRiskTier { get; set; } = 1;
        public List<string> AssignedCapabilities { get; set; } = new();
        public string Status { get; set; } = "Active";
    }

    public class TeamCharter
    {
        public string CharterId { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public string WorkId { get; set; } = string.Empty;
        public string? WorkPlanId { get; set; }
        public string? MissionId { get; set; }
        public string Objective { get; set; } = string.Empty;
        public DateTime FormedUtc { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresUtc { get; set; } = DateTime.UtcNow.AddMinutes(60);
        public TeamLifecycleStatus Status { get; set; } = TeamLifecycleStatus.Forming;
        public List<TeamMemberRole> Members { get; set; } = new();
        public decimal ResourceBudget { get; set; } = 0m;
        public int GovernanceTier { get; set; } = 1;
        public string CharterHash { get; set; } = string.Empty;

        public void ComputeCharterHash()
        {
            var raw = $"{TenantId}:{WorkId}:{Objective}:{Members.Count}:{ExpiresUtc:O}:{GovernanceTier}:{ResourceBudget}";
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
            CharterHash = Convert.ToHexString(bytes);
        }
    }

    public class CollaborationMessage
    {
        public string MessageId { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public string CharterId { get; set; } = string.Empty;
        public string SenderAgentId { get; set; } = string.Empty;
        public string RecipientAgentId { get; set; } = "Broadcast";
        public CollaborationMessageType MessageType { get; set; } = CollaborationMessageType.InformationSharing;
        public string Content { get; set; } = string.Empty;
        public List<string> EvidenceReferences { get; set; } = new();
        public EpistemicStatus EpistemicClassification { get; set; } = EpistemicStatus.Hypothesis;
        public int TurnNumber { get; set; } = 1;
        public string CorrelationId { get; set; } = string.Empty;
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
        public string MessageHash { get; set; } = string.Empty;

        public void ComputeMessageHash()
        {
            var raw = $"{TenantId}:{CharterId}:{SenderAgentId}:{RecipientAgentId}:{MessageType}:{TurnNumber}:{Content}:{string.Join(",", EvidenceReferences)}";
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
            MessageHash = Convert.ToHexString(bytes);
        }
    }

    public class DisputeRecord
    {
        public string DisputeId { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public string CharterId { get; set; } = string.Empty;
        public DisputeType DisputeType { get; set; } = DisputeType.ConflictingHypothesis;
        public string Topic { get; set; } = string.Empty;
        public string InitiatorAgentId { get; set; } = string.Empty;
        public List<string> ContendingAgentIds { get; set; } = new();
        public Dictionary<string, string> ConflictingClaims { get; set; } = new();
        public ArbitrationOutcome ResolutionOutcome { get; set; } = ArbitrationOutcome.EvidencePrevails;
        public string ResolutionEvidence { get; set; } = string.Empty;
        public bool EscalatedToGovernance { get; set; } = false;
        public DateTime ResolvedUtc { get; set; } = DateTime.UtcNow;
        public string DisputeHash { get; set; } = string.Empty;

        public void ComputeDisputeHash()
        {
            var raw = $"{TenantId}:{CharterId}:{DisputeType}:{Topic}:{InitiatorAgentId}:{ResolutionOutcome}:{EscalatedToGovernance}:{ResolvedUtc:O}";
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
            DisputeHash = Convert.ToHexString(bytes);
        }
    }

    public class TeamFormationPolicy
    {
        public int MaxTeamSize { get; set; } = 5;
        public int MaxNegotiationRounds { get; set; } = 10;
        public int MaxActiveTeamsPerTenant { get; set; } = 20;
        public TimeSpan DefaultCharterTtl { get; set; } = TimeSpan.FromMinutes(60);
        public bool RequireEvidenceForHandoff { get; set; } = true;

        public static TeamFormationPolicy ResolveEffective(
            TeamFormationPolicy systemPolicy,
            TeamFormationPolicy? tenantOverride)
        {
            if (tenantOverride == null) return systemPolicy;
            return new TeamFormationPolicy
            {
                MaxTeamSize = Math.Min(systemPolicy.MaxTeamSize, tenantOverride.MaxTeamSize),
                MaxNegotiationRounds = Math.Min(systemPolicy.MaxNegotiationRounds, tenantOverride.MaxNegotiationRounds),
                MaxActiveTeamsPerTenant = Math.Min(systemPolicy.MaxActiveTeamsPerTenant, tenantOverride.MaxActiveTeamsPerTenant),
                DefaultCharterTtl = tenantOverride.DefaultCharterTtl < systemPolicy.DefaultCharterTtl ? tenantOverride.DefaultCharterTtl : systemPolicy.DefaultCharterTtl,
                RequireEvidenceForHandoff = systemPolicy.RequireEvidenceForHandoff || tenantOverride.RequireEvidenceForHandoff
            };
        }
    }

    public class TeamPerformanceRecord
    {
        public string CharterId { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public bool ObjectiveAchieved { get; set; } = false;
        public TimeSpan Duration { get; set; } = TimeSpan.Zero;
        public int RoundsUsed { get; set; } = 0;
        public int DisputesEncountered { get; set; } = 0;
        public int DisputesResolved { get; set; } = 0;
        public double CollaborationEfficiencyScore { get; set; } = 1.0;
    }
}
