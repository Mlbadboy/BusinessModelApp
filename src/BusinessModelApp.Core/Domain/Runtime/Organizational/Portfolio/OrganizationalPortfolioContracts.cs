using System.Security.Cryptography;
using System.Text;
using BusinessModelApp.Core.Domain.Runtime.Organizational.Allocation;

namespace BusinessModelApp.Core.Domain.Runtime.Organizational.Portfolio;

/// <summary>
/// Invariant I33: Constitutional Invariant of Autonomous Resource-Aware Work Planning & Portfolio Control.
/// ALLOCATION != WORK PLAN != PORTFOLIO != MISSION GRAPH != SCHEDULE != AUTHORITY != EXECUTION != OUTCOME
///
/// Sovereignty Boundaries:
/// - OARA (3.9.7) owns ALLOCATION ARBITRATION.
/// - 3.9.8 owns PORTFOLIO / WORK PLANNING.
/// - 3.9.0 owns WORK ADMISSION.
/// - 3.9.1 owns WORK MANAGEMENT.
/// - 3.9.2 owns MISSION COORDINATION.
/// - Runtime owns EXECUTION SCHEDULING.
/// - Batch 6 owns CONSEQUENTIAL EXECUTION AUTHORITY.
///
/// Subordinate Invariants:
/// - I33-A: Portfolio != Allocation. Having an allocated envelope does not create a portfolio.
/// - I33-B: Work Plan != Mission Graph. A plan establishes sequence and trade-offs, not executable DAGs.
/// - I33-C: Portfolio Priority != Execution Authority. High portfolio rank never creates unilateral execution permits.
/// - I33-D: Allocation != Guaranteed Resource Consumption. Work plans respect actual OARA reservation locks.
/// - I33-E: Plan != Commitment. A plan is a structured proposal subject to PRG-1 human review.
/// - I33-F: Dependency != Unilateral Block. Missing dependencies mark items BlockedByDependency; planner cannot force execution.
/// - I33-G: Portfolio Optimization Cannot Violate 3.5 Constraints. No rebalance or planning algorithm may exceed liquidity floors or budget ceilings.
/// - I33-H: Portfolio Cannot Increase OARA Allocation Ceiling. Planner can allocate <= OARA envelope, never > envelope.
/// - I33-I: Portfolio Cannot Manufacture ExecutionPermit. Zero code paths from portfolio planning to Batch 6 permits.
/// - I33-J: Rebalancing Cannot Force-Preempt UnknownEffect. In-flight work with pending/unknown status cannot be aborted without safe drain.
/// - I33-K: Simulation Cannot Mutate Production Portfolio. What-If simulations carry zero production state locks.
/// - I33-L: Cross-Tenant Portfolio Isolation. Strict partitioning by TenantId.
/// - I33-M: Human Governance Sovereignty. Structural rebalances generate PortfolioChangeProposal for PRG-1 review.
/// - I33-N: Versioned Snapshots. Every portfolio rebalance generates a deterministic versioned SHA-256 hash.
/// - I33-O: Explainable Rebalancing. Every rebalancing proposal provides explicit "Why Change?" and "Why Stop?" provenance.
/// - I33-P: No Self-Prioritization. Portfolio planner cannot allocate work or priorities to itself.
/// - I33-Q: Anti-Churn & Bounded Replanning. Materiality metric gates proposals; regular ticks with no delta produce zero churn.
/// - I33-R: Fail-Closed Planning. Planning anomalies or constraint ambiguities immediately fail closed.
/// - I33-S: OARA Allocation Ceiling Sovereignty. 3.9.8 MUST NOT increase or manufacture an OARA allocation. PortfolioAllocatedCapacity <= OARAAllocatedCapacity.
/// - I33-T: Deterministic Portfolio Optimization. Same inputs yield bit-for-bit identical PortfolioDecisionHash.
/// - I33-U: Portfolio Change != Mission Mutation. Continue/Increase/Reduce/Pause/Stop/Start are proposed portfolio intents, not direct Mission.Cancel() calls.
/// - I33-V: Planning Dependency Sovereignty. Portfolio dependency graph answers readiness before work begins; cannot mutate/execute a Mission Graph.
/// - I33-W: Simulation Result Epistemic Isolation. PortfolioSimulationResult has TruthClassification = Simulation. Read-only snapshots, zero write capability.
/// </summary>
public static class InvariantI33
{
    public const string InvariantName = "I33_OrganizationalPortfolioAndWorkPlanningSovereignty";
    public const string CoreDoctrine = "ALLOCATION != WORK PLAN != PORTFOLIO != MISSION GRAPH != SCHEDULE != AUTHORITY != EXECUTION != OUTCOME";

    public const string I33_A = "I33-A: Portfolio != Allocation. An allocated envelope does not constitute an active portfolio.";
    public const string I33_B = "I33-B: Work Plan != Mission Graph. A plan establishes sequence and trade-offs, not executable DAGs.";
    public const string I33_C = "I33-C: Portfolio Priority != Execution Authority. High portfolio rank never creates unilateral execution permits.";
    public const string I33_D = "I33-D: Allocation != Guaranteed Resource Consumption. Work plans respect actual OARA reservation locks.";
    public const string I33_E = "I33-E: Plan != Commitment. A plan is a structured proposal subject to PRG-1 human review.";
    public const string I33_F = "I33-F: Dependency != Unilateral Block. Missing dependencies mark items BlockedByDependency; planner cannot force execution.";
    public const string I33_G = "I33-G: Constraints Cannot Be Optimized Away. No rebalance or planning algorithm may exceed liquidity floors or budget ceilings.";
    public const string I33_H = "I33-H: Portfolio Cannot Increase OARA Allocation Ceiling. Planner can allocate <= OARA envelope, never > envelope.";
    public const string I33_I = "I33-I: Portfolio Cannot Manufacture ExecutionPermit. Zero code paths from portfolio planning to Batch 6 permits.";
    public const string I33_J = "I33-J: Rebalancing Cannot Force-Preempt UnknownEffect. In-flight work with pending/unknown status cannot be aborted without safe drain.";
    public const string I33_K = "I33-K: Simulation Cannot Mutate Production Portfolio. What-If simulations carry zero production state locks.";
    public const string I33_L = "I33-L: Cross-Tenant Portfolio Isolation. Strict partitioning by TenantId.";
    public const string I33_M = "I33-M: Human Governance Sovereignty. Structural rebalances generate PortfolioChangeProposal for PRG-1 review.";
    public const string I33_N = "I33-N: Versioned Snapshots. Every portfolio rebalance generates a deterministic versioned SHA-256 hash.";
    public const string I33_O = "I33-O: Explainable Rebalancing. Every rebalancing proposal provides explicit 'Why Change?' and 'Why Stop?' provenance.";
    public const string I33_P = "I33-P: No Self-Prioritization. Portfolio planner cannot allocate work or priorities to itself.";
    public const string I33_Q = "I33-Q: Anti-Churn & Bounded Replanning. Materiality metric gates proposals; regular ticks with no delta produce zero churn.";
    public const string I33_R = "I33-R: Fail-Closed Planning. Planning anomalies or constraint ambiguities immediately fail closed.";
    public const string I33_S = "I33-S: OARA Allocation Ceiling Sovereignty. 3.9.8 MUST NOT increase or manufacture an OARA allocation. PortfolioAllocatedCapacity <= OARAAllocatedCapacity.";
    public const string I33_T = "I33-T: Deterministic Portfolio Optimization. Same inputs yield bit-for-bit identical PortfolioDecisionHash.";
    public const string I33_U = "I33-U: Portfolio Change != Mission Mutation. Continue/Increase/Reduce/Pause/Stop/Start are proposed portfolio intents, not direct Mission.Cancel() calls.";
    public const string I33_V = "I33-V: Planning Dependency Sovereignty. Portfolio dependency graph answers readiness before work begins; cannot mutate/execute a Mission Graph.";
    public const string I33_W = "I33-W: Simulation Result Epistemic Isolation. PortfolioSimulationResult has TruthClassification = Simulation. Read-only snapshots, zero write capability.";
}

public enum PortfolioWorkCategory
{
    Strategic = 0,   // Growth, capability acquisition, transformation
    Operational = 1, // Core delivery, fulfillment, customer success, SLA
    Obligatory = 2   // Statutory compliance, security patching, debt remediation
}

public enum PortfolioLifecycleState
{
    Draft = 0,
    Evaluating = 1,
    PortfolioAdmitted = 2,
    ResourceAligned = 3,
    DependenciesValidated = 4,
    PlanReady = 5,
    GovernanceReview = 6,
    WorkPlaneAdmissionRequested = 7, // Strictly distinct from ExecutionPermit (I33-E)
    Deferred = 8,
    Waiting = 9,
    Blocked = 10,
    Rejected = 11,
    Superseded = 12,
    Cancelled = 13
}

public enum PortfolioItemAction
{
    Continue = 0,
    Increase = 1,
    Reduce = 2,
    Pause = 3,
    Stop = 4,
    Supersede = 5,
    Start = 6
}

public enum DependencyValidationState
{
    Satisfied = 0,
    Missing = 1,
    InProgress = 2,
    Failed = 3,
    CycleDetected = 4
}

/// <summary>
/// An atomic candidate or active work item in the organizational portfolio.
/// </summary>
public sealed class PortfolioWorkItem
{
    public string WorkItemId { get; set; } = Guid.NewGuid().ToString("N");
    public string TenantId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public PortfolioWorkCategory Category { get; set; } = PortfolioWorkCategory.Operational;
    public PortfolioLifecycleState State { get; set; } = PortfolioLifecycleState.Draft;
    public string TargetResponsibilityId { get; set; } = string.Empty;
    public string TargetProposalId { get; set; } = string.Empty;

    // Multi-dimensional metrics
    public double StrategicAlignmentScore { get; set; } = 0.5; // 0.0 to 1.0
    public double ExpectedValue { get; set; } = 1.0;
    public double RiskScore { get; set; } = 0.2; // 0.0 to 1.0
    public double ReversibilityScore { get; set; } = 0.8; // 1.0 = fully reversible
    public double DeadlinePressure { get; set; } = 0.5; // 0.0 to 1.0
    public double OpportunityCost { get; set; } = 0.0;
    public bool IsReadinessDebtRemediation { get; set; } = false;

    // Capacities (Requested vs OARA Allocated)
    public Dictionary<OrganizationalResourceType, double> ResourceDemands { get; set; } = new();
    public Dictionary<OrganizationalResourceType, double> AllocatedCapacities { get; set; } = new();

    // Dependencies
    public List<string> PrerequisiteWorkItemIds { get; set; } = new();
    public DependencyValidationState DependencyState { get; set; } = DependencyValidationState.Satisfied;

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? TargetCompletionUtc { get; set; }
}

/// <summary>
/// Envelope of available capacity granted by OARA for this portfolio epoch.
/// Invariant I33-S: PortfolioAllocatedCapacity <= OARAAllocatedCapacity.
/// </summary>
public sealed class PortfolioCapacityEnvelope
{
    public string EnvelopeId { get; set; } = Guid.NewGuid().ToString("N");
    public string TenantId { get; set; } = string.Empty;
    public string AllocationSnapshotHash { get; set; } = string.Empty;
    public Dictionary<OrganizationalResourceType, double> AvailableCapacities { get; set; } = new();
    public Dictionary<OrganizationalResourceType, double> CommittedCapacities { get; set; } = new();
    public DateTime EvaluatedUtc { get; set; } = DateTime.UtcNow;

    public bool HasSufficientCapacity(Dictionary<OrganizationalResourceType, double> demands)
    {
        foreach (var (type, requested) in demands)
        {
            AvailableCapacities.TryGetValue(type, out var available);
            CommittedCapacities.TryGetValue(type, out var committed);
            if ((committed + requested) > available) return false;
        }
        return true;
    }

    public void CommitCapacity(Dictionary<OrganizationalResourceType, double> amounts)
    {
        foreach (var (type, amount) in amounts)
        {
            CommittedCapacities.TryGetValue(type, out var current);
            CommittedCapacities[type] = current + amount;
        }
    }
}

/// <summary>
/// Versioned snapshot of the organizational portfolio.
/// </summary>
public sealed class OrganizationalPortfolio
{
    public string PortfolioId { get; set; } = Guid.NewGuid().ToString("N");
    public string TenantId { get; set; } = string.Empty;
    public int VersionNumber { get; set; } = 1;
    public DateTime GeneratedUtc { get; set; } = DateTime.UtcNow;

    public string AllocationSnapshotHash { get; set; } = string.Empty;
    public string ConstraintSnapshotHash { get; set; } = string.Empty;
    public string StrategicRegimeSnapshotHash { get; set; } = string.Empty;
    public string PortfolioDecisionHash { get; set; } = string.Empty;

    public List<PortfolioWorkItem> WorkItems { get; set; } = new();
    public PortfolioCapacityEnvelope CapacityEnvelope { get; set; } = new();
    public PortfolioLifecycleState State { get; set; } = PortfolioLifecycleState.PlanReady;

    public void ComputeDecisionHash()
    {
        var sb = new StringBuilder();
        sb.Append($"{TenantId}:{VersionNumber}:{AllocationSnapshotHash}:{ConstraintSnapshotHash}:{StrategicRegimeSnapshotHash}:");
        foreach (var item in WorkItems.OrderBy(i => i.WorkItemId))
        {
            sb.Append($"{item.WorkItemId}:{item.State}:{item.Category}:");
            foreach (var (type, val) in item.AllocatedCapacities.OrderBy(kv => kv.Key))
            {
                sb.Append($"{type}={val};");
            }
        }
        using var sha = SHA256.Create();
        PortfolioDecisionHash = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString())));
    }
}

/// <summary>
/// Mathematical materiality metric used to gate rebalancing proposals (I33-Q).
/// </summary>
public sealed class MaterialityDelta
{
    public double RealityDelta { get; set; } = 0.0;
    public double ForecastDelta { get; set; } = 0.0;
    public double ConstraintDelta { get; set; } = 0.0;
    public double StrategicRegimeDelta { get; set; } = 0.0;
    public double AllocationDelta { get; set; } = 0.0;
    public double DependencyDelta { get; set; } = 0.0;
    public double OutcomeDelta { get; set; } = 0.0;

    public double ComputeScore()
    {
        // Deterministic weighted formula
        return Math.Round(
            (0.20 * RealityDelta) +
            (0.15 * ForecastDelta) +
            (0.20 * ConstraintDelta) +
            (0.15 * StrategicRegimeDelta) +
            (0.15 * AllocationDelta) +
            (0.10 * DependencyDelta) +
            (0.05 * OutcomeDelta),
            4);
    }
}

/// <summary>
/// Action proposal for an individual portfolio item during rebalance.
/// Invariant I33-U: Proposed intent, not direct runtime mission mutation.
/// </summary>
public sealed class PortfolioItemRebalanceAction
{
    public string WorkItemId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public PortfolioItemAction Action { get; set; }
    public Dictionary<OrganizationalResourceType, double> TargetCapacities { get; set; } = new();
    public string Rationale { get; set; } = string.Empty;
    public string StatusQuoImpact { get; set; } = string.Empty;
}

/// <summary>
/// Complete governed rebalancing proposal (I33-M, I33-Q, I33-U).
/// </summary>
public sealed class PortfolioChangeProposal
{
    public string ProposalId { get; set; } = Guid.NewGuid().ToString("N");
    public string TenantId { get; set; } = string.Empty;
    public string SourcePortfolioId { get; set; } = string.Empty;
    public double ComputedMaterialityScore { get; set; }
    public double MaterialityThreshold { get; set; } = 0.15;
    public bool IsMaterial => ComputedMaterialityScore >= MaterialityThreshold;
    public List<PortfolioItemRebalanceAction> ItemActions { get; set; } = new();
    public string StrategicJustification { get; set; } = string.Empty;
    public string ProposalHash { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public void ComputeHash()
    {
        var sb = new StringBuilder($"{ProposalId}:{TenantId}:{SourcePortfolioId}:{ComputedMaterialityScore:F4}:");
        foreach (var action in ItemActions.OrderBy(a => a.WorkItemId))
        {
            sb.Append($"{action.WorkItemId}:{action.Action}:");
        }
        using var sha = SHA256.Create();
        ProposalHash = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString())));
    }
}

/// <summary>
/// Deep explainability trace for portfolio rebalancing decisions ("Why Change?", "Why Stop?").
/// </summary>
public sealed class WhyRebalanceTrace
{
    public string ProposalId { get; set; } = string.Empty;
    public string WorkItemId { get; set; } = string.Empty;
    public PortfolioItemAction Action { get; set; }
    public string WhyActionTaken { get; set; } = string.Empty;
    public string TriggeringMaterialityFactors { get; set; } = string.Empty;
    public string OpportunityCostComparison { get; set; } = string.Empty;
    public string CapacityImpactSummary { get; set; } = string.Empty;
    public DateTime EvaluatedUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Read-only simulation scenario definition (I33-W).
/// </summary>
public sealed class PortfolioSimulationScenario
{
    public string ScenarioId { get; set; } = Guid.NewGuid().ToString("N");
    public string TenantId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Dictionary<PortfolioWorkCategory, double> CategoryAllocationWeights { get; set; } = new();
    public Dictionary<OrganizationalResourceType, double> SimulatedCapacityShifts { get; set; } = new();
    public string SimulatedRegime { get; set; } = "Growth";
}

/// <summary>
/// Scenario outcome produced in simulation sandbox (I33-W).
/// </summary>
public sealed class SimulatedScenarioOutcome
{
    public string ScenarioName { get; set; } = string.Empty;
    public double ProjectedPortfolioValue { get; set; }
    public double ProjectedRiskExposure { get; set; }
    public double ProjectedBufferPreservation { get; set; }
    public int CompletedInitiativesEstimate { get; set; }
    public int DeferredInitiativesCount { get; set; }
}

/// <summary>
/// Immutable, epistemically isolated simulation result (I33-W).
/// TruthClassification is strictly "Simulation".
/// </summary>
public sealed class PortfolioSimulationResult
{
    public string SimulationRunId { get; init; } = Guid.NewGuid().ToString("N");
    public string TruthClassification => "Simulation"; // Explicit epistemic stamp (I33-W)
    public string InputSnapshotHash { get; init; } = string.Empty;
    public string OutputSnapshotHash { get; init; } = string.Empty;
    public string ProviderId { get; init; } = "DeterministicScenarioEngine";
    public IReadOnlyList<SimulatedScenarioOutcome> ScenarioOutcomes { get; init; } = Array.Empty<SimulatedScenarioOutcome>();
    public double EstimatedUncertainty { get; init; } = 0.25;
    public DateTime SimulatedUtc { get; init; } = DateTime.UtcNow;
}
