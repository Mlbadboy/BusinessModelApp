using System.Security.Cryptography;
using System.Text;

namespace BusinessModelApp.Core.Domain.Runtime.Organizational.Allocation;

/// <summary>
/// Invariant I32: Constitutional Invariant of Organizational Attention & Resource Allocation (OARA).
/// ATTENTION != PRIORITY != RESOURCE != RESERVATION != ALLOCATION != AUTHORITY != EXECUTION
///
/// Sovereignty Boundary:
/// OARA cannot create:
/// - Strategic Authority (Batch 3.5 owns Strategic Regime)
/// - Reservation Authority (Batch 3.5 reservation mechanism owns locks)
/// - Human Approval (PRG-1 owns human governance)
/// - Execution Authority (Batch 6 firewall owns execution permits)
///
/// Subordinate Invariants:
/// - I32-A: Attention != Priority. Cognitive salience does not establish priority.
/// - I32-B: Priority != Allocation. Priority does not grant unconstrained allocation.
/// - I32-C: Allocation != Authority. Reservations do not grant execution authority.
/// - I32-D: Resource Reservation != Resource Consumption. Reservations are planning locks, not consumption.
/// - I32-E: Scarcity Sovereignty. Deterministic lexicographic arbitration governs competing demands.
/// - I32-F: No Resource Monopolization. Starvation protection and aging prevent indefinite lockup.
/// - I32-G: Constraints Cannot Be Optimized Away. 3.5 cash/regulatory constraints remain inviolable.
/// - I32-H: Budget Ceiling Is Hard. Optimization cannot increase budgets, wallets, or compute quotas.
/// - I32-I: Forecast != Guaranteed Capacity. Predicted demand cannot lock unlimited future resources.
/// - I32-J: Simulation != Reservation. Simulations carry zero real-world locks or side effects.
/// - I32-K: Allocation != ExecutionPermit. Allocation != CapabilityLease != ExecutionPermit.
/// - I32-L: Cross-Tenant Allocation Isolation. Strict multi-tenant capacity partitioning.
/// - I32-M: Allocation Atomicity. Multi-resource allocations are all-or-nothing unless governed partial approved.
/// - I32-N: Allocation Expiry. Unused reservations expire deterministically via TTL.
/// - I32-O: Allocation Revocation Is Not Forced Preemption. UnknownEffect preserved on in-flight operations.
/// - I32-P: Human Governance Sovereignty. OARA prepares AllocationProposals; human approval cannot be manufactured.
/// - I32-Q: No Self-Expansion. OARA cannot allocate resources or attention to itself.
/// - I32-R: Deterministic Arbitration Non-Compensation. Hard constraints cannot be offset by downstream ROI/urgency.
/// - I32-S: Strategic Regime Sovereignty. OARA consumes 3.5 strategic regime authority; cannot mutate it.
/// - I32-T: Reservation Authority Uniqueness. Reuses 3.5 reservation mechanism; zero competing reservation locks.
/// - I32-U: Epistemic Capacity Sovereignty. Unmeasured capacity strictly defaults to Unknown.
/// - I32-V: Allocation Approval != Human Approval. ArbitrationAdmitted != HumanApproved != ExecutionPermit.
/// - I32-W: Simulation Capacity Cannot Become Real Capacity. Simulation != Reality != Reservation != Authority.
/// </summary>
public static class InvariantI32
{
    public const string InvariantName = "I32_OrganizationalAllocationSovereignty";
    public const string CoreDoctrine = "ATTENTION != PRIORITY != RESOURCE != RESERVATION != ALLOCATION != AUTHORITY != EXECUTION";

    public const string I32_A = "I32-A: Attention != Priority. Cognitive salience does not autonomously establish organizational priority.";
    public const string I32_B = "I32-B: Priority != Allocation. High strategic priority does not grant unconstrained resource entitlement.";
    public const string I32_C = "I32-C: Allocation != Authority. Receiving a resource allocation cannot create execution authority.";
    public const string I32_D = "I32-D: Resource Reservation != Resource Consumption. Advance reservations are planning locks, not consumption.";
    public const string I32_E = "I32-E: Scarcity Sovereignty. Deterministic lexicographic arbitration governs; LLMs cannot dictate allocations.";
    public const string I32_F = "I32-F: No Resource Monopolization. Starvation protection and aging prevent indefinite capacity lockup.";
    public const string I32_G = "I32-G: Constraints Cannot Be Optimized Away. Inviolable business constraints cannot be bypassed.";
    public const string I32_H = "I32-H: Budget Ceiling Is Hard. Optimization cannot increase budgets, wallets, or compute quotas.";
    public const string I32_I = "I32-I: Forecast != Guaranteed Capacity. Predicted demand cannot lock unlimited future resources.";
    public const string I32_J = "I32-J: Simulation != Reservation. Counterfactual simulations carry zero real-world locks or side effects.";
    public const string I32_K = "I32-K: Allocation != ExecutionPermit. Allocation != CapabilityLease != ExecutionPermit.";
    public const string I32_L = "I32-L: Cross-Tenant Allocation Isolation. Tenant capacities and allocations are strictly partitioned.";
    public const string I32_M = "I32-M: Allocation Atomicity. Multi-resource allocations are all-or-nothing unless governed partial states approved.";
    public const string I32_N = "I32-N: Allocation Expiry. Unused reservations expire deterministically via TTL to prevent phantom capacity.";
    public const string I32_O = "I32-O: Allocation Revocation Is Not Forced Preemption. In-flight consequential operations cannot be terminated.";
    public const string I32_P = "I32-P: Human Governance Sovereignty. OARA prepares AllocationProposals; human approval cannot be manufactured.";
    public const string I32_Q = "I32-Q: No Self-Expansion. OARA cannot allocate resources or attention to itself.";
    public const string I32_R = "I32-R: Deterministic Arbitration Non-Compensation. Hard constraint breaches cannot be offset by downstream ROI or urgency.";
    public const string I32_S = "I32-S: Strategic Regime Sovereignty. OARA consumes 3.5 strategic regime authority; cannot mutate or override it.";
    public const string I32_T = "I32-T: Reservation Authority Uniqueness. OARA reuses the authoritative 3.5 reservation mechanism; zero competing locks.";
    public const string I32_U = "I32-U: Epistemic Capacity Sovereignty. Resource capacity must carry epistemic classification; unmeasured defaults to Unknown.";
    public const string I32_V = "I32-V: Allocation Approval != Human Approval. ArbitrationAdmitted != HumanApproved != ExecutionPermit.";
    public const string I32_W = "I32-W: Simulation Capacity Cannot Become Real Capacity. Simulation != Reality != Reservation != Authority.";
}

/// <summary>
/// Epistemic classification of resource capacity reality.
/// Invariant I32-U: Unmeasured capacity strictly defaults to Unknown.
/// </summary>
public enum ResourceCapacityReality
{
    Verified = 0,    // Measured directly from live ERP/telemetry
    Inferred = 1,    // Derived from active mission and worker queue telemetry
    Simulated = 2,   // Produced by counterfactual scenario modeling
    Stale = 3,       // Telemetry older than validity window
    Unknown = 4,     // PRG-1 Reality Discipline: Unmeasured capacity
    NotConnected = 5 // No underlying instrumentation present
}

/// <summary>
/// First-class scarce organizational resource types.
/// </summary>
public enum OrganizationalResourceType
{
    AgentBandwidth = 0,     // Specialist worker availability slots
    HumanAttention = 1,     // Executive/managerial decision queue bandwidth
    Compute = 2,            // GPU/CPU/API rate limit headroom
    MissionSlots = 3,       // Max concurrent mission orchestrations
    OperationsCapacity = 4, // Production/fulfillment/logistics bandwidth
    LiquidityBuffer = 5,    // Working capital and cash runway reserve
    Budget = 6,             // Discretionary and operational spending ceiling
    Time = 7                // Critical calendar horizon & deadline windows
}

/// <summary>
/// Governed lifecycle of an allocation.
/// Invariant I32-V: ArbitrationAdmitted != HumanApproved != ExecutionPermit.
/// </summary>
public enum AllocationLifecycleState
{
    Proposed = 0,
    Evaluating = 1,
    ArbitrationAdmitted = 2, // Admitted by OARA arbitration; NOT HumanApproved
    Reserved = 3,            // Governed lock established
    Consuming = 4,           // Active work in progress
    Released = 5,            // Completed and capacity freed
    Expired = 6,             // TTL elapsed without consumption
    Rejected = 7             // Denied due to constraints or lack of capacity
}

/// <summary>
/// Posture resulting from allocation arbitration.
/// </summary>
public enum AllocationDecisionPosture
{
    Allocate = 0,              // Full capacity granted
    AllocatePartial = 1,       // Governed partial grant with explicit trade-off justification
    Defer = 2,                 // Delayed until next scheduling epoch
    WaitForCapacity = 3,       // Queued pending release of running missions
    RequestHumanDecision = 4,  // Escalated to human executive for discretionary judgment
    Reject = 5                 // Permanently denied due to constraint breach or insufficient value
}

/// <summary>
/// Type of Resource Allocation Debt.
/// </summary>
public enum ResourceDebtType
{
    AgentBandwidthDebt = 0,
    ComputeDebt = 1,
    AttentionDebt = 2,
    OperationsDebt = 3,
    LiquidityBufferDebt = 4
}

/// <summary>
/// First-class model of a scarce organizational resource.
/// </summary>
public class OrganizationalResource
{
    public string ResourceId { get; set; } = Guid.NewGuid().ToString("N");
    public string TenantId { get; set; } = string.Empty;
    public OrganizationalResourceType ResourceType { get; set; }
    public string Name { get; set; } = string.Empty;
    public double TotalCapacity { get; set; }
    public double AvailableCapacity { get; set; }
    public double ReservedCapacity { get; set; }
    public double ConsumedCapacity { get; set; }
    public ResourceCapacityReality EpistemicReality { get; set; } = ResourceCapacityReality.Unknown;
    public double MaxAllocationPerDemand { get; set; } = double.MaxValue;
    public double HardFloorCapacity { get; set; } = 0.0;
    public string Unit { get; set; } = string.Empty;
    public TimeSpan WindowDuration { get; set; } = TimeSpan.FromHours(24);
    public string SourceEvidenceRef { get; set; } = string.Empty;
    public DateTime LastEvaluatedUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Demand placed on scarce organizational capacity by an initiative, responsibility, or mission proposal.
/// </summary>
public class AllocationDemand
{
    public string DemandId { get; set; } = Guid.NewGuid().ToString("N");
    public string TenantId { get; set; } = string.Empty;
    public string TargetResponsibilityId { get; set; } = string.Empty;
    public string TargetProposalId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public Dictionary<OrganizationalResourceType, double> RequestedCapacities { get; set; } = new();
    public int PriorityTier { get; set; } = 1; // 1 = Standard, 2 = Elevated, 3 = High, 4 = Critical
    public double ExpectedOutcomeImpact { get; set; } = 0.5; // 0.0 to 1.0
    public double ExpectedRoiPerUnit { get; set; } = 1.0;
    public double RiskScore { get; set; } = 0.2; // 0.0 to 1.0
    public double ReversibilityScore { get; set; } = 0.8; // 1.0 = fully reversible
    public double TimeToDecayHours { get; set; } = 72.0;
    public bool AllowsPartialAllocation { get; set; } = false;
    public double MinimumViableFraction { get; set; } = 0.5;
    public Dictionary<OrganizationalResourceType, double> MinimumViableCapacities { get; set; } = new();
    public bool IsReadinessDebtRemediation { get; set; } = false;
    public string EvidenceRef { get; set; } = string.Empty;
    public DateTime SubmittedUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Evaluation candidate during arbitration.
/// </summary>
public class AllocationCandidate
{
    public AllocationDemand Demand { get; set; } = new();
    public bool PassesHardConstraints { get; set; } = true;
    public string ConstraintBreachReason { get; set; } = string.Empty;
    public bool PassesSafetyAndCompliance { get; set; } = true;
    public double StrategicRegimeScore { get; set; } = 1.0;
    public double AgingBonus { get; set; } = 0.0;
    public double FinalArbitrationScore { get; set; } = 0.0;
    public AllocationDecisionPosture RecommendedPosture { get; set; } = AllocationDecisionPosture.Defer;
    public Dictionary<OrganizationalResourceType, double> GrantedCapacities { get; set; } = new();
}

/// <summary>
/// Immutable ledger record representing an admitted allocation and reservation.
/// </summary>
public class AllocationRecord
{
    public string AllocationId { get; set; } = Guid.NewGuid().ToString("N");
    public string TenantId { get; set; } = string.Empty;
    public string DemandId { get; set; } = string.Empty;
    public string TargetResponsibilityId { get; set; } = string.Empty;
    public string TargetProposalId { get; set; } = string.Empty;
    public Dictionary<OrganizationalResourceType, double> AllocatedCapacities { get; set; } = new();
    public AllocationLifecycleState State { get; set; } = AllocationLifecycleState.Proposed;
    public AllocationDecisionPosture Posture { get; set; } = AllocationDecisionPosture.Allocate;
    public DateTime ValidFromUtc { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAtUtc { get; set; } = DateTime.UtcNow.AddHours(24);
    public string SnapshotHash { get; set; } = string.Empty;
    public string ReasonSummary { get; set; } = string.Empty;

    public void ComputeSnapshotHash(
        string resourceSnapshot,
        string demandSnapshot,
        string constraintSnapshot,
        string regimeSnapshot,
        string policySnapshot,
        string readinessSnapshot)
    {
        string raw = $"{TenantId}:{resourceSnapshot}:{demandSnapshot}:{constraintSnapshot}:{regimeSnapshot}:{policySnapshot}:{readinessSnapshot}:{State}:{Posture}:{ExpiresAtUtc:O}";
        using var sha = SHA256.Create();
        SnapshotHash = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(raw)));
    }
}

/// <summary>
/// Complete allocation plan generated by OARA for an epoch.
/// </summary>
public class AllocationPlan
{
    public string PlanId { get; set; } = Guid.NewGuid().ToString("N");
    public string TenantId { get; set; } = string.Empty;
    public DateTime GeneratedUtc { get; set; } = DateTime.UtcNow;
    public List<AllocationRecord> AdmittedAllocations { get; set; } = new();
    public List<AllocationCandidate> EvaluatedCandidates { get; set; } = new();
    public List<ResourceDebtRecord> AccumulatedDebts { get; set; } = new();
    public string ActiveStrategicRegime { get; set; } = "GrowthAndResilience";
}

/// <summary>
/// Persistent Resource Allocation Debt record.
/// Legitimate Demands > Available Capacity -> Resource Allocation Debt.
/// </summary>
public class ResourceDebtRecord
{
    public string DebtId { get; set; } = Guid.NewGuid().ToString("N");
    public string TenantId { get; set; } = string.Empty;
    public ResourceDebtType DebtType { get; set; }
    public double ShortfallAmount { get; set; }
    public string Description { get; set; } = string.Empty;
    public int AgeInDays { get; set; } = 0;
    public double CompoundingFactor { get; set; } = 1.0;
    public double DebtAgeMultiplier => CompoundingFactor;
    public double EffectiveSeverity => Math.Round(ShortfallAmount * CompoundingFactor, 2);
    public DateTime FirstDeferredUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastEvaluatedUtc { get; set; } = DateTime.UtcNow;
    public bool IsSatisfied { get; set; } = false;
}

/// <summary>
/// Deep provenance trace explaining "Why Allocated?" and "Why Not More?".
/// </summary>
public class WhyAllocationTrace
{
    public string AllocationId { get; set; } = string.Empty;
    public AllocationDecisionPosture Posture { get; set; }
    public string WhyAllocated { get; set; } = string.Empty;
    public string WhyNotMore { get; set; } = string.Empty;
    public string LimitingConstraint { get; set; } = string.Empty;
    public List<string> UpstreamLadderPasses { get; set; } = new();
    public List<string> CompetingAlternativesConsidered { get; set; } = new();
    public Dictionary<OrganizationalResourceType, double> CapacityShortfallsEncountered { get; set; } = new();
    public string SnapshotHash { get; set; } = string.Empty;
    public DateTime EvaluatedUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Explicit attention budget model across cognitive organizational domains.
/// </summary>
public class AttentionBudget
{
    public string TenantId { get; set; } = string.Empty;
    public double TotalAttentionUnits { get; set; } = 100.0;
    public double CommittedAttentionUnits { get; set; } = 0.0;
    public double ConsumedAttentionUnits => CommittedAttentionUnits;
    public double AvailableAttentionUnits => Math.Max(0.0, TotalAttentionUnits - CommittedAttentionUnits);
    public Dictionary<string, double> DomainCommitments { get; set; } = new();
    public Dictionary<string, double> DomainAllocations => DomainCommitments;
}
