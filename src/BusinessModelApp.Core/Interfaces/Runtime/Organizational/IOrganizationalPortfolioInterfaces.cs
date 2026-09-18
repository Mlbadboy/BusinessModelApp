using BusinessModelApp.Core.Domain.Runtime.Organizational.Allocation;
using BusinessModelApp.Core.Domain.Runtime.Organizational.Portfolio;

namespace BusinessModelApp.Core.Interfaces.Runtime.Organizational;

/// <summary>
/// Versioned storage and snapshot ledger for organizational portfolios.
/// Invariant I33-L: Cross-tenant portfolio isolation.
/// Invariant I33-N: Versioned snapshots with deterministic SHA-256 hashes.
/// </summary>
public interface IPortfolioRepository
{
    Task SavePortfolioAsync(string tenantId, OrganizationalPortfolio portfolio);
    Task<OrganizationalPortfolio?> GetActivePortfolioAsync(string tenantId);
    Task<OrganizationalPortfolio?> GetPortfolioVersionAsync(string tenantId, int versionNumber);
    Task<IReadOnlyList<OrganizationalPortfolio>> ListPortfolioHistoryAsync(string tenantId);
}

/// <summary>
/// Validates prerequisite graph and detects circular dependencies.
/// Invariant I33-F: Missing dependency marks item BlockedByDependency; cannot force execution.
/// Invariant I33-V: Planning dependency graph answers readiness before work begins; cannot mutate/execute a Mission Graph.
/// </summary>
public interface IDependencyGraphResolver
{
    Task<(DependencyValidationState State, string? Reason, IReadOnlyList<string> OrderedWorkItemIds)> ValidateAndSortDependenciesAsync(
        string tenantId,
        IReadOnlyList<PortfolioWorkItem> items);
}

/// <summary>
/// Deterministic capacity-aware sequencer matching work candidates to OARA allocation envelope.
/// Invariant I33-S: PortfolioAllocatedCapacity <= OARAAllocatedCapacity. 3.9.8 cannot manufacture allocation.
/// Invariant I33-T: Deterministic Portfolio Optimization. Identical inputs yield bit-for-bit identical PortfolioDecisionHash.
/// </summary>
public interface IPortfolioPlanner
{
    OrganizationalPortfolio PlanPortfolio(
        string tenantId,
        IReadOnlyList<PortfolioWorkItem> candidateItems,
        PortfolioCapacityEnvelope capacityEnvelope,
        string activeStrategicRegime = "GrowthAndResilience",
        string constraintSnapshotHash = "ConstraintPass");
}

/// <summary>
/// Evaluates reality changes and generates governed rebalancing proposals.
/// Invariant I33-Q: Materiality metric gates proposals; regular ticks with no delta produce zero churn.
/// Invariant I33-U: Proposed intents, not direct Mission.Cancel() calls.
/// </summary>
public interface IPortfolioRebalanceEngine
{
    Task<PortfolioChangeProposal?> EvaluateRebalanceAsync(
        string tenantId,
        OrganizationalPortfolio currentPortfolio,
        MaterialityDelta delta,
        IReadOnlyList<PortfolioWorkItem> incomingCandidates);
}

/// <summary>
/// Epistemically stamped read-only simulation sandbox for What-If scenario analysis.
/// Invariant I33-K: Simulation cannot mutate production portfolio.
/// Invariant I33-W: TruthClassification = Simulation. Read-only snapshots, zero write capability.
/// </summary>
public interface IPortfolioSimulator
{
    Task<PortfolioSimulationResult> SimulateScenariosAsync(
        string tenantId,
        OrganizationalPortfolio portfolioSnapshot,
        IReadOnlyList<PortfolioSimulationScenario> scenarios);
}

/// <summary>
/// Provenance engine providing "Why Rebalance?" and "Why Stop?" explanations.
/// Invariant I33-O: Every rebalance must be explainable.
/// </summary>
public interface IPortfolioProvenanceService
{
    WhyRebalanceTrace ExplainRebalanceAction(
        PortfolioChangeProposal proposal,
        PortfolioItemRebalanceAction action,
        MaterialityDelta delta);
}

/// <summary>
/// Unified facade coordinating organizational portfolio planning, rebalancing, and simulation.
/// </summary>
public interface IOrganizationalPortfolioService
{
    Task<OrganizationalPortfolio> GeneratePortfolioPlanAsync(
        string tenantId,
        IReadOnlyList<PortfolioWorkItem> candidateItems,
        AllocationPlan allocationPlan);

    Task<PortfolioChangeProposal?> RebalancePortfolioAsync(
        string tenantId,
        MaterialityDelta delta,
        IReadOnlyList<PortfolioWorkItem>? incomingCandidates = null);

    Task<WhyRebalanceTrace> GetRebalanceActionWhyTraceAsync(
        string tenantId,
        string proposalId,
        string workItemId);

    Task<PortfolioSimulationResult> RunSimulationSandboxAsync(
        string tenantId,
        IReadOnlyList<PortfolioSimulationScenario> scenarios);

    Task<PortfolioChangeProposal> ProposeItemStopOrSupersedeAsync(
        string tenantId,
        string workItemId,
        string rationale,
        bool isSupersede = false);
}
