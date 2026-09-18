using System.Security.Cryptography;
using System.Text;

namespace BusinessModelApp.Core.Domain.Runtime.Enterprise.Brain;

/// <summary>
/// Constitutional Invariant I36 — Executive Cognitive State Sovereignty.
/// INTELLIGENCE != AWARENESS != RESPONSIBILITY != DECISION != AUTHORITY != EXECUTION.
/// </summary>
public static class AutonomousBusinessBrainInvariants
{
    public const string PrimaryInvariant = "INTELLIGENCE != AWARENESS != RESPONSIBILITY != DECISION != AUTHORITY != EXECUTION";

    // 26 Sub-laws I36-A through I36-Z
    public const string I36_A_IntelligenceNotAwareness = "I36-A: Raw LLM outputs, embeddings, or metrics do not equal executive state awareness without structured synthesis.";
    public const string I36_B_AwarenessNotResponsibility = "I36-B: Knowing a metric dropped does not assign an agent responsibility without governed allocation.";
    public const string I36_C_ResponsibilityNotDecision = "I36-C: Being responsible for a domain does not authorize making unilateral business policy decisions.";
    public const string I36_D_DecisionNotAuthority = "I36-D: Recommending or formulating an action is decision intelligence, not approval authority.";
    public const string I36_E_AuthorityNotExecution = "I36-E: Having human approval does not bypass the Batch 6 execution firewall or pre-execution verification.";
    public const string I36_F_EpistemicTransparency = "I36-F: Every cognitive signal carries an explicit epistemic status (Live, Verified, Inferred, Simulated, Stale, Unknown, NotConnected).";
    public const string I36_G_ExplicitUnknowns = "I36-G: Charlie must actively catalog its epistemic gaps and uncertainties rather than hallucinating continuity.";
    public const string I36_H_MultiTenantCognitiveIsolation = "I36-H: Executive cognitive states, attention priorities, and memory traces are strictly isolated by TenantId.";
    public const string I36_I_NonExecutionPrinciple = "I36-I: The Brain is purely an executive cognitive fabric; it cannot execute external mutations or issue execution permits.";
    public const string I36_J_CausalNonOverwriting = "I36-J: Causal attributions must be sourced from 3.8.1 Causal Intelligence without independent DAG mutation.";
    public const string I36_K_ForecastingIntegrity = "I36-K: Forecast projections must remain labeled as probabilistic distributions, never empirical facts.";
    public const string I36_L_ResourceDebtVisibility = "I36-L: The Brain reflects resource debt and capacity reservations strictly from 3.9.7 OARA.";
    public const string I36_M_MissionStatusFidelity = "I36-M: Active mission representations must reflect authoritative states from 3.9.2 Mission Orchestrator.";
    public const string I36_N_AuditProvenance = "I36-N: Every cognitive state snapshot carries a cryptographic SHA-256 SnapshotHash.";
    public const string I36_O_AntiHallucinationThreshold = "I36-O: Signals lacking empirical grounding must be classified as Unknown or Inferred with a confidence score.";
    public const string I36_P_CognitiveRecencyAndDecay = "I36-P: Cognitive state items decay in confidence over time if not refreshed by live observations.";
    public const string I36_Q_ContradictionResolution = "I36-Q: When signals from different engines conflict, the Brain surfaces a CognitiveContradiction rather than silently averaging.";
    public const string I36_R_ImmutableStateHistory = "I36-R: Brain snapshots are stored append-only; historical cognitive states cannot be altered or overwritten.";
    public const string I36_S_ExecutiveAttentionEconomy = "I36-S: The Brain filters out sub-material noise, reserving executive attention for material business deltas.";
    public const string I36_T_ZeroDirectSelfMutation = "I36-T: The Brain cannot alter its own cognitive synthesis rules or weights without governed adaptation (3.9.10 OLMA) and PRG-1 approval.";
    public const string I36_U_PRG1EscalationQueue = "I36-U: High-consequence decisions and critical risks are queued specifically for PRG-1 human governance.";
    public const string I36_V_SimulationBoundary = "I36-V: Simulated states from 3.9.9 are strictly segregated from live reality within the cognitive state.";
    public const string I36_W_EpistemicGapCataloging = "I36-W: Unknowns and unobserved areas are first-class citizen entities in the cognitive state.";
    public const string I36_X_FailClosedCognition = "I36-X: Telemetry failures degrade cognitive state to Partial or Degraded, never fabricating default nominal data.";
    public const string I36_Y_TenantPenetrationDefense = "I36-Y: Accessing cognitive states belonging to another tenant throws UnauthorizedAccessException.";
    public const string I36_Z_BrainNotAutonomousAgent = "I36-Z: The Brain coordinates cognitive synthesis; it does not replace specialized workers or human leadership.";
}

public enum CognitiveEpistemicStatus
{
    Live,
    Verified,
    Inferred,
    Simulated,
    Stale,
    Unknown,
    NotConnected
}

public enum CognitiveHealthStatus
{
    Nominal,
    Attentive,
    Degraded,
    Critical,
    PartialFailure
}

public enum CognitiveContradictionSeverity
{
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Detailed record of what changed across enterprise metrics, regimes, and environments.
/// </summary>
public sealed class EnterpriseDeltasRecord
{
    public string DeltaId { get; set; } = Guid.NewGuid().ToString("N");
    public string Domain { get; set; } = "Enterprise";
    public string MetricOrEntity { get; set; } = string.Empty;
    public double PreviousValue { get; set; }
    public double CurrentValue { get; set; }
    public double DeltaMagnitude => CurrentValue - PreviousValue;
    public double MaterialityScore { get; set; }
    public bool IsMaterial => MaterialityScore >= 0.15;
    public CognitiveEpistemicStatus EpistemicStatus { get; set; } = CognitiveEpistemicStatus.Live;
    public DateTime ObservedUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Attention priority sourced from 3.9.7 OARA capacity arbitration.
/// </summary>
public sealed class AttentionPriorityItem
{
    public string PriorityId { get; set; } = Guid.NewGuid().ToString("N");
    public string Area { get; set; } = string.Empty;
    public string Rationale { get; set; } = string.Empty;
    public double UrgencyScore { get; set; } // 0.0 to 1.0
    public double AllocatedCapacityPercentage { get; set; }
    public string StrategicRegime { get; set; } = "Balanced";
}

/// <summary>
/// Scarcity bottleneck reflecting constrained organizational resources.
/// </summary>
public sealed class ResourceConstraintItem
{
    public string ConstraintId { get; set; } = Guid.NewGuid().ToString("N");
    public string ResourceType { get; set; } = string.Empty; // Compute, Attention, Liquidity, Missions, Operations
    public double UtilizationRatio { get; set; } // 0.0 to 1.0+
    public bool IsBottleneck => UtilizationRatio >= 0.85;
    public string ImpactedDomain { get; set; } = string.Empty;
    public string RecommendedAlleviation { get; set; } = string.Empty;
}

/// <summary>
/// Active failures, latency spikes, or impediments observed across missions and operations.
/// </summary>
public sealed class ActiveImpedimentRecord
{
    public string ImpedimentId { get; set; } = Guid.NewGuid().ToString("N");
    public string SourceComponent { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = "Medium"; // Low, Medium, High, Critical
    public bool IsBlockingWork { get; set; }
    public DateTime DetectedUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Pending decision awaiting PRG-1 human governance or automated policy clearance.
/// </summary>
public sealed class DecisionQueueItem
{
    public string DecisionId { get; set; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = string.Empty;
    public string ProposedAction { get; set; } = string.Empty;
    public string ConsequenceTier { get; set; } = "Tier3_HardGovernance";
    public bool RequiresHumanApproval => ConsequenceTier == "Tier3_HardGovernance" || ConsequenceTier == "Tier4_MultiParty";
    public double ProjectedRoi { get; set; }
    public DateTime QueuedUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Critical alert escalated to human leadership under PRG-1 governance.
/// </summary>
public sealed class ExecutiveEscalationItem
{
    public string EscalationId { get; set; } = Guid.NewGuid().ToString("N");
    public string Category { get; set; } = "OperationalRisk";
    public string ExecutiveSummary { get; set; } = string.Empty;
    public string RecommendedAction { get; set; } = string.Empty;
    public bool IsUrgent { get; set; }
    public DateTime EscalatedUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// First-class citizen representing what Charlie does NOT know (I36-G, I36-W).
/// </summary>
public sealed class EpistemicGapItem
{
    public string GapId { get; set; } = Guid.NewGuid().ToString("N");
    public string Domain { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Reason { get; set; } = "NotConnected"; // NotConnected, TelemetryStale, HighUncertainty, ConflictingEvidence
    public CognitiveEpistemicStatus EpistemicStatus { get; set; } = CognitiveEpistemicStatus.Unknown;
    public double UncertaintyScore { get; set; } = 1.0; // 0.0 to 1.0
    public DateTime DetectedUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Detected divergence or contradiction between intelligence engines (I36-Q).
/// </summary>
public sealed class CognitiveContradictionItem
{
    public string ContradictionId { get; set; } = Guid.NewGuid().ToString("N");
    public string Topic { get; set; } = string.Empty;
    public string SourceEngineA { get; set; } = string.Empty;
    public string AssertionA { get; set; } = string.Empty;
    public string SourceEngineB { get; set; } = string.Empty;
    public string AssertionB { get; set; } = string.Empty;
    public CognitiveContradictionSeverity Severity { get; set; } = CognitiveContradictionSeverity.Medium;
    public string RecommendedResolution { get; set; } = "Human Review Required";
    public DateTime DetectedUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// The complete synthesized Executive Cognitive State representing what Charlie knows (and does not know).
/// Sourced from Reality, Watchtower, BI, Causal, Forecast, Radar, Scenario, Decision, Readiness, OARA, Portfolio, Memory, Learning.
/// </summary>
public sealed class ExecutiveCognitiveState
{
    public string SnapshotId { get; set; } = Guid.NewGuid().ToString("N");
    public string TenantId { get; set; } = string.Empty;
    public DateTime SnapshotUtc { get; set; } = DateTime.UtcNow;
    public CognitiveHealthStatus OverallHealth { get; set; } = CognitiveHealthStatus.Nominal;
    public string CurrentEnterpriseSummary { get; set; } = string.Empty;

    // The 12 Core Cognitive Inquiry Projections
    public List<EnterpriseDeltasRecord> RecentDeltas { get; set; } = new();
    public List<string> CausalExplanations { get; set; } = new();
    public List<string> LeadingForecasts { get; set; } = new();
    public List<AttentionPriorityItem> AttentionPriorities { get; set; } = new();
    public List<ResourceConstraintItem> ResourceBottlenecks { get; set; } = new();
    public List<string> ActiveMissions { get; set; } = new();
    public List<ActiveImpedimentRecord> ActiveImpedimentsAndFailures { get; set; } = new();
    public List<string> IdentifiedOpportunities { get; set; } = new();
    public List<DecisionQueueItem> PendingDecisions { get; set; } = new();
    public List<ExecutiveEscalationItem> ExecutiveEscalations { get; set; } = new();
    public List<EpistemicGapItem> EpistemicGaps { get; set; } = new();
    public List<CognitiveContradictionItem> CognitiveContradictions { get; set; } = new();

    // Cryptographic Snapshot Provenance (I36-N)
    public string SnapshotHash { get; set; } = string.Empty;

    public void ComputeSnapshotHash()
    {
        var raw = $"{TenantId}:{SnapshotId}:{SnapshotUtc:O}:{OverallHealth}:{RecentDeltas.Count}:{AttentionPriorities.Count}:{ResourceBottlenecks.Count}:{EpistemicGaps.Count}:{CognitiveContradictions.Count}";
        using var sha = SHA256.Create();
        SnapshotHash = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(raw)));
    }
}
