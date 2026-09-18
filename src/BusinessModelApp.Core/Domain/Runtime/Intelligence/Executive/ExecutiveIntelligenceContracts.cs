using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace BusinessModelApp.Core.Domain.Runtime.Intelligence.Executive
{
    // =========================================================================
    // THE GOLDEN EXECUTIVE INTELLIGENCE SOVEREIGNTY RULE (INVARIANT I24)
    // =========================================================================
    // BRIEFING != DECISION != APPROVAL != EXECUTION
    // Executive Insight != Executive Recommendation != Decision != Approval != Execution
    //
    // Charlie can tell leadership:
    // "Revenue conversion has deteriorated 8.4%, evidence confidence is high,
    //  the primary causal hypothesis is X, Scenario B has the strongest risk-adjusted
    //  outcome, and this decision requires human review."
    //
    // Charlie CANNOT say:
    // "I approved Scenario B and executed it."
    //
    // CHARLIE MAY BRIEF, PRIORITIZE, EXPLAIN, AND ESCALATE.
    // CHARLIE CANNOT APPROVE. CHARLIE CANNOT EXECUTE. CHARLIE CANNOT SELF-AUTHORIZE.
    // =========================================================================

    public enum ExecutiveAudience
    {
        CEO,    // Strategic impact, top risks, top opportunities, capital implications, decision portfolio
        CFO,    // Cash, margin, revenue, working capital, downside scenarios, capital allocation
        COO,    // Capacity, operations, service levels, bottlenecks, worker performance, execution dependencies
        CRO,    // Pipeline, conversion, pricing, retention, customer risk, territory
        Board   // Strategic trajectory, material risks, governance items, capital requirements
    }

    public enum ExecutivePriority
    {
        CriticalAttention,  // Highest priority for immediate executive awareness (NOT emergency execution authority)
        MaterialReview,     // Significant material impact requiring leadership evaluation
        StrategicWatch,     // Emerging pattern or opportunity to track
        Informational       // Standard operational awareness
    }

    public enum ExecutiveEpistemicKind
    {
        Fact,               // Supported directly by certified reality observation/telemetry
        Inference,          // Derived via deterministic analytical rules from facts
        Hypothesis,         // Causal hypothesis (identifiability bounded)
        Forecast,           // Predictive time-series projection
        Simulation,         // Scenario counterfactual outcome distribution
        Opportunity,        // Radar-detected upside potential
        Recommendation,     // Decision engine proposed candidate
        Decision,           // Human/PRG-1 approved decision
        Policy,             // Governance/materiality rule constraint
        Unknown             // Insufficient telemetry or ungrounded signal
    }

    public enum ExecutiveClaimValidity
    {
        Valid,
        ConditionallyValid,
        InsufficientEvidence,
        StaleEvidence,
        ConflictedEvidence,
        OverstatedClaim,
        Unknown
    }

    public enum ExecutiveBriefStatus
    {
        Draft,
        Assembling,
        Validating,
        EvidenceChecked,
        GovernanceChecked,
        Ready,
        Published,          // Intelligence communication; PUBLISHED != APPROVED
        Acknowledged,       // Leadership has viewed/acknowledged the briefing
        Superseded,         // Newer briefing generated for same horizon/audience
        Expired,            // Temporal horizon elapsed or evidence stale
        Rejected,           // Failed deterministic validation
        Invalidated,        // Underlying scenario or forecast invalidated
        Conflicted          // Severe unresolvable upstream signal contradictions
    }

    // =========================================================================
    // MATERIALITY POLICY & DETERMINISTIC SCORING (I24-F)
    // =========================================================================

    public sealed record ExecutiveMaterialityPolicy
    {
        public string PolicyId { get; init; } = "EMP-DEFAULT-v1";
        public string Version { get; init; } = "1.0.0";
        public string TenantId { get; init; } = string.Empty;

        // Deterministic weights summing to 1.0
        public double FinancialImpactWeight { get; init; } = 0.30;
        public double OperationalImpactWeight { get; init; } = 0.20;
        public double StrategicImpactWeight { get; init; } = 0.20;
        public double RiskExposureWeight { get; init; } = 0.15;
        public double TimeSensitivityWeight { get; init; } = 0.15;

        // Materiality Thresholds
        public double CriticalAttentionThreshold { get; init; } = 80.0;
        public double MaterialReviewThreshold { get; init; } = 60.0;
        public double StrategicWatchThreshold { get; init; } = 40.0;

        // Freshness & Confidence constraints
        public TimeSpan MaxEvidenceAge { get; init; } = TimeSpan.FromHours(24);
        public double MinConfidenceThreshold { get; init; } = 50.0;

        public DateTime EffectiveFromUtc { get; init; } = DateTime.UtcNow;
        public DateTime? EffectiveUntilUtc { get; init; }
        public string IntegrityHash { get; set; } = string.Empty;

        public string ComputeIntegrityHash()
        {
            var raw = $"{PolicyId}:{Version}:{TenantId}:{FinancialImpactWeight:F3}:{OperationalImpactWeight:F3}:{StrategicImpactWeight:F3}:{RiskExposureWeight:F3}:{TimeSensitivityWeight:F3}:{CriticalAttentionThreshold:F1}:{MaterialReviewThreshold:F1}";
            using var sha = SHA256.Create();
            IntegrityHash = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(raw)));
            return IntegrityHash;
        }
    }

    public sealed record ExecutiveMaterialityScore
    {
        public double FinancialImpact { get; init; }
        public double OperationalImpact { get; init; }
        public double StrategicImpact { get; init; }
        public double RiskExposure { get; init; }
        public double TimeSensitivity { get; init; }

        public double MaterialityScore { get; init; }
        public double PriorityScore { get; init; }
        public double UrgencyScore { get; init; }
        public double ExposureScore { get; init; }
        public ExecutivePriority AssignedPriority { get; init; }
    }

    // =========================================================================
    // EXECUTIVE CLAIM & CLAIM GRAPH (I24-A, I24-B, I24-C, I24-N)
    // =========================================================================

    public sealed record ExecutiveClaim
    {
        public string ClaimId { get; init; } = Guid.NewGuid().ToString("N");
        public string ClaimText { get; init; } = string.Empty;
        public ExecutiveEpistemicKind EpistemicKind { get; init; } = ExecutiveEpistemicKind.Unknown;
        public double Confidence { get; init; }
        public IReadOnlyList<string> EvidenceIds { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> SourceHashes { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> UpstreamArtifactHashes { get; init; } = Array.Empty<string>();
        public string PolicyVersion { get; init; } = "1.0.0";
        public DateTime GeneratedAtUtc { get; init; } = DateTime.UtcNow;
        public ExecutiveClaimValidity Validity { get; set; } = ExecutiveClaimValidity.Valid;
        public string ValidityReason { get; set; } = string.Empty;
        public string IntegrityHash { get; set; } = string.Empty;

        public string ComputeIntegrityHash()
        {
            var raw = $"{ClaimId}:{ClaimText}:{EpistemicKind}:{Confidence:F2}:{string.Join(",", EvidenceIds)}:{string.Join(",", SourceHashes)}";
            using var sha = SHA256.Create();
            IntegrityHash = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(raw)));
            return IntegrityHash;
        }
    }

    public sealed record ExecutiveClaimGraph
    {
        public string GraphId { get; init; } = Guid.NewGuid().ToString("N");
        public string BriefId { get; init; } = string.Empty;
        public IReadOnlyList<ExecutiveClaim> Claims { get; init; } = Array.Empty<ExecutiveClaim>();
        public IReadOnlyDictionary<string, List<string>> LineageEdges { get; init; } = new Dictionary<string, List<string>>();
        public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
    }

    // =========================================================================
    // EXECUTIVE CONTRADICTION & UNCERTAINTY (I24-G, I24-H)
    // =========================================================================

    public sealed record ExecutiveContradictionRecord
    {
        public string ContradictionId { get; init; } = Guid.NewGuid().ToString("N");
        public string Domain { get; init; } = string.Empty;
        public string ClaimAId { get; init; } = string.Empty;
        public string ClaimAText { get; init; } = string.Empty;
        public string ClaimBId { get; init; } = string.Empty;
        public string ClaimBText { get; init; } = string.Empty;
        public string DivergenceDescription { get; init; } = string.Empty;
        public double Severity { get; init; }
        public DateTime DetectedAtUtc { get; init; } = DateTime.UtcNow;
    }

    // =========================================================================
    // EXECUTIVE BRIEF SECTIONS & SO-WHAT COMPONENT
    // =========================================================================

    public sealed record ExecutiveMetricSummary
    {
        public string MetricKey { get; init; } = string.Empty;
        public string MetricName { get; init; } = string.Empty;
        public string DisplayValue { get; init; } = "UNKNOWN";
        public string TrendDirection { get; init; } = "Neutral"; // "Up", "Down", "Stable", "Unknown"
        public double? DeltaPercentage { get; init; }
        public ExecutiveEpistemicKind EpistemicKind { get; init; } = ExecutiveEpistemicKind.Fact;
        public string ProvenanceRef { get; init; } = string.Empty;
    }

    public sealed record ExecutiveInsight
    {
        public string InsightId { get; init; } = Guid.NewGuid().ToString("N");
        public string Title { get; init; } = string.Empty;
        public string Domain { get; init; } = string.Empty;

        // The "So What?" Architecture:
        public string WhatChanged { get; init; } = string.Empty;
        public string WhyItHappened { get; init; } = string.Empty;
        public string SoWhatWhyCare { get; init; } = string.Empty;
        public string WhatHappensNext { get; init; } = string.Empty;
        public IReadOnlyList<string> AlternativesAvailable { get; init; } = Array.Empty<string>();
        public string TradeoffSacrifice { get; init; } = string.Empty;
        public string CharlieRecommendation { get; init; } = string.Empty;
        public string GovernanceRequirement { get; init; } = "REQUIRES HUMAN REVIEW";

        public ExecutivePriority Priority { get; init; } = ExecutivePriority.MaterialReview;
        public double Confidence { get; init; }
        public ExecutiveEpistemicKind EpistemicKind { get; init; } = ExecutiveEpistemicKind.Inference;
        public IReadOnlyList<string> LinkedClaimIds { get; init; } = Array.Empty<string>();
    }

    public sealed record ExecutiveGovernanceItem
    {
        public string ItemId { get; init; } = Guid.NewGuid().ToString("N");
        public string Title { get; init; } = string.Empty;
        public string Category { get; init; } = "Decision"; // Decision, Risk, Staleness, Policy, Constraint
        public string RecommendedAction { get; init; } = string.Empty;
        public string RequiredReviewerRole { get; init; } = "CEO";
        public DateTime? DeadlineUtc { get; init; }
        public string CostOfInaction { get; init; } = string.Empty;
        public string Status { get; init; } = "REQUIRES HUMAN REVIEW";
        public string LinkedCandidateId { get; init; } = string.Empty;
    }

    public sealed record ExecutiveAttentionItem
    {
        public string AttentionId { get; init; } = Guid.NewGuid().ToString("N");
        public string Headline { get; init; } = string.Empty;
        public ExecutivePriority Priority { get; init; } = ExecutivePriority.CriticalAttention;
        public string WhySurfaced { get; init; } = string.Empty;
        public string EvidenceSummary { get; init; } = string.Empty;
        public double Confidence { get; init; }
        public string HumanOwner { get; init; } = "CEO";
        public DateTime? ReviewDeadlineUtc { get; init; }
    }

    public sealed record ExecutiveBriefSection
    {
        public string SectionId { get; init; } = Guid.NewGuid().ToString("N");
        public string Title { get; init; } = string.Empty;
        public int Order { get; init; }
        public string ContentMarkdown { get; init; } = string.Empty;
        public IReadOnlyList<ExecutiveMetricSummary> Metrics { get; init; } = Array.Empty<ExecutiveMetricSummary>();
        public IReadOnlyList<ExecutiveInsight> Insights { get; init; } = Array.Empty<ExecutiveInsight>();
        public IReadOnlyList<ExecutiveClaim> Claims { get; init; } = Array.Empty<ExecutiveClaim>();
    }

    // =========================================================================
    // IMMUTABLE EXECUTIVE SNAPSHOT & PROVENANCE (I24-M, I24-O)
    // =========================================================================

    public sealed record ExecutiveInputSnapshot
    {
        public string SnapshotId { get; init; } = Guid.NewGuid().ToString("N");
        public string TenantId { get; init; } = string.Empty;
        public DateTime CapturedAtUtc { get; init; } = DateTime.UtcNow;
        public string RealitySnapshotHash { get; init; } = string.Empty;
        public string BiKernelSnapshotHash { get; init; } = string.Empty;
        public string CausalSnapshotHash { get; init; } = string.Empty;
        public string ForecastSnapshotHash { get; init; } = string.Empty;
        public string RadarSnapshotHash { get; init; } = string.Empty;
        public string ScenarioSnapshotHash { get; init; } = string.Empty;
        public string DecisionSnapshotHash { get; init; } = string.Empty;
        public string MaterialityPolicyHash { get; init; } = string.Empty;
        public string IntegrityHash { get; set; } = string.Empty;

        public string ComputeIntegrityHash()
        {
            var raw = $"{SnapshotId}:{TenantId}:{RealitySnapshotHash}:{BiKernelSnapshotHash}:{CausalSnapshotHash}:{ForecastSnapshotHash}:{RadarSnapshotHash}:{ScenarioSnapshotHash}:{DecisionSnapshotHash}:{MaterialityPolicyHash}";
            using var sha = SHA256.Create();
            IntegrityHash = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(raw)));
            return IntegrityHash;
        }
    }

    public sealed record ExecutiveBriefProvenance
    {
        public string ProvenanceId { get; init; } = Guid.NewGuid().ToString("N");
        public string BriefId { get; init; } = string.Empty;
        public string TenantId { get; init; } = string.Empty;
        public string SnapshotId { get; init; } = string.Empty;
        public string SnapshotIntegrityHash { get; init; } = string.Empty;
        public string BriefIntegrityHash { get; init; } = string.Empty;
        public DateTime GeneratedAtUtc { get; init; } = DateTime.UtcNow;

        public bool VerifyLineage(string currentBriefHash, string currentSnapshotHash)
        {
            return !string.IsNullOrWhiteSpace(BriefId) &&
                   !string.IsNullOrWhiteSpace(TenantId) &&
                   BriefIntegrityHash == currentBriefHash &&
                   SnapshotIntegrityHash == currentSnapshotHash;
        }
    }

    // =========================================================================
    // THE EXECUTIVE BRIEF (I24 ROOT ARTIFACT)
    // =========================================================================

    public sealed record ExecutiveBrief
    {
        public string BriefId { get; init; } = Guid.NewGuid().ToString("N");
        public string TenantId { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public ExecutiveAudience Audience { get; init; } = ExecutiveAudience.CEO;
        public ExecutiveBriefStatus Status { get; set; } = ExecutiveBriefStatus.Draft;
        public DateTime GeneratedAtUtc { get; init; } = DateTime.UtcNow;
        public DateTime EvidenceCutoffUtc { get; init; } = DateTime.UtcNow;
        public DateTime ValidityHorizonUtc { get; init; } = DateTime.UtcNow.AddHours(24);

        // Clamped confidence: min of all upstream dependencies
        public double ClampedConfidence { get; init; } = 100.0;
        public string StrategicRegime { get; init; } = "Normal";

        // Structured Brief Sections
        public IReadOnlyList<ExecutiveBriefSection> Sections { get; init; } = Array.Empty<ExecutiveBriefSection>();
        public IReadOnlyList<ExecutiveAttentionItem> AttentionQueue { get; init; } = Array.Empty<ExecutiveAttentionItem>();
        public IReadOnlyList<ExecutiveGovernanceItem> GovernanceQueue { get; init; } = Array.Empty<ExecutiveGovernanceItem>();
        public IReadOnlyList<ExecutiveContradictionRecord> Contradictions { get; init; } = Array.Empty<ExecutiveContradictionRecord>();
        public IReadOnlyList<ExecutiveClaim> ClaimGraph { get; init; } = Array.Empty<ExecutiveClaim>();

        // Constitutional boundary statement (Immutable)
        public string ConstitutionalBoundary { get; init; } =
            "STATUS: PENDING HUMAN REVIEW — CHARLIE CANNOT APPROVE OR EXECUTE. " +
            "Executive briefings provide decision-support intelligence. All consequential actions require Human / PRG-1 authority.";

        public string SnapshotId { get; init; } = string.Empty;
        public string PolicySnapshotHash { get; init; } = string.Empty;
        public string IntegrityHash { get; set; } = string.Empty;

        public string ComputeIntegrityHash()
        {
            var raw = $"{BriefId}:{TenantId}:{Audience}:{GeneratedAtUtc:O}:{EvidenceCutoffUtc:O}:{ValidityHorizonUtc:O}:{ClampedConfidence:F2}:{Sections.Count}:{AttentionQueue.Count}:{GovernanceQueue.Count}:{SnapshotId}:{PolicySnapshotHash}";
            using var sha = SHA256.Create();
            IntegrityHash = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(raw)));
            return IntegrityHash;
        }
    }
}
