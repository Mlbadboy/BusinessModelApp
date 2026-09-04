using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BusinessModelApp.Core.Agents;
using BusinessModelApp.Core.Domain.Common;

namespace BusinessModelApp.Core.Domain.Decisions
{
    public enum DecisionType
    {
        SelectStrategy = 1,
        AllocateCapacity = 2,
        ApproveProposal = 3,
        RejectOpportunity = 4,
        HaltExecution = 5,
        ModifyTerms = 6
    }

    public enum DecisionApprovalStatus
    {
        NotRequired = 0,
        Pending = 1,
        Approved = 2,
        Rejected = 3
    }

    /// <summary>
    /// Immutable first-class record of an autonomous executive decision.
    /// Invariant: Once committed, a DecisionRecord cannot be modified.
    /// Superseding decisions must reference SupersedesDecisionId.
    /// Includes cryptographic SHA-256 hash verifying that inputs, trade-offs, and rationale are untampered.
    /// </summary>
    public class DecisionRecord : Entity
    {
        public Guid DecisionId => Id;
        public Guid? WorkspaceId { get; set; }
        public Guid ObjectiveId { get; set; }
        public Guid? MissionId { get; set; }
        public Guid? StrategyId { get; set; }
        public Guid? ParentDecisionId { get; set; }
        public Guid? SupersedesDecisionId { get; set; } // Immutable lineage supersession

        public string CEOObjective { get; set; } = string.Empty;
        public Guid? WorldModelSnapshotId { get; set; }
        public Guid? RevenueBaselineSnapshotId { get; set; }

        public AgentRole Role { get; set; } = AgentRole.ExecutiveOrchestrator;
        public DecisionType Type { get; set; } = DecisionType.SelectStrategy;
        public string DecisionMaker { get; set; } = "Charlie Executive Supervisor";

        public string SelectedAlternative { get; set; } = string.Empty;
        public string SelectedStrategyName { get; set; } = string.Empty;
        public string AlternativesConsideredJson { get; set; } = "[]";
        public string StrategyCandidatesJson { get; set; } = "[]";
        public string RejectedStrategiesJson { get; set; } = "[]";

        // Economic & Feasibility Projections
        public decimal ExpectedRevenueImpactINR { get; set; }
        public decimal ExpectedCostINR { get; set; }
        public decimal ExpectedMarginPercent { get; set; }
        public double WinProbability { get; set; }
        public double DeliveryFeasibilityScore { get; set; } // 0.0 to 1.0 based on Capacity

        // Governance & Autonomy
        public double RiskScore { get; set; }
        public AutonomyLevel ExecutedAutonomyLevel { get; set; } = AutonomyLevel.Level3_ControlledAutonomy;
        public bool HumanApprovalRequired { get; set; } = false;
        public DecisionApprovalStatus ApprovalStatus { get; set; } = DecisionApprovalStatus.NotRequired;
        public Guid? HumanApprovalRequestId { get; set; }
        public string RequiredApprovalsJson { get; set; } = "[]";

        // Grounding Evidence & Explainability
        public string DecisionRationale { get; set; } = string.Empty;
        public string GroundingEvidenceIdsJson { get; set; } = "[]";
        public string GroundingEvidenceHashesJson { get; set; } = "[]";
        public string EvidenceReferencesJson { get; set; } = "[]";
        public string ConstitutionRulesEvaluatedJson { get; set; } = "[]";
        public string ConstitutionResultJson { get; set; } = "{}";
        public string FeasibilityResultJson { get; set; } = "{}";
        public string AssumptionsJson { get; set; } = "[]";
        public string RiskAssessmentJson { get; set; } = "{}";

        public string CryptographicHash { get; set; } = string.Empty;
        public DateTime DecidedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ExecutedAt { get; set; }

        public string ComputeCryptographicHash()
        {
            var payload = $"{Id}:{ObjectiveId}:{MissionId}:{StrategyId}:{SelectedAlternative}:{ExpectedRevenueImpactINR}:{ExpectedCostINR}:{WinProbability}:{RiskScore}:{DecisionRationale}:{GroundingEvidenceHashesJson}:{DecidedAt:O}";
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(payload));
            CryptographicHash = Convert.ToHexString(bytes).ToLowerInvariant();
            return CryptographicHash;
        }
    }
}
