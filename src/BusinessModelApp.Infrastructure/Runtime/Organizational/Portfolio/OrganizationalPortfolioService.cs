using BusinessModelApp.Core.Domain.Runtime.Organizational.Allocation;
using BusinessModelApp.Core.Domain.Runtime.Organizational.Portfolio;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational.Portfolio;

/// <summary>
/// Unified facade coordinating organizational portfolio planning, dynamic rebalancing, and simulation.
/// Preserves:
/// - Invariant I33: ALLOCATION != WORK PLAN != PORTFOLIO != MISSION GRAPH != SCHEDULE != AUTHORITY != EXECUTION != OUTCOME.
/// - Invariant I33-S: OARA Allocation Ceiling Sovereignty (PortfolioAllocatedCapacity <= OARAAllocatedCapacity).
/// - Invariant I33-U: Portfolio Change != Mission Mutation (governed proposal, no direct Mission.Cancel).
/// - Invariant I33-W: Simulation Result Epistemic Isolation (TruthClassification = Simulation).
/// </summary>
public class OrganizationalPortfolioService : IOrganizationalPortfolioService
{
    private readonly IPortfolioRepository _repository;
    private readonly IPortfolioPlanner _planner;
    private readonly IPortfolioRebalanceEngine _rebalanceEngine;
    private readonly IPortfolioSimulator _simulator;
    private readonly IPortfolioProvenanceService _provenanceService;

    // Cache of recent proposals and deltas for provenance trace retrieval
    private readonly Dictionary<string, (PortfolioChangeProposal Proposal, MaterialityDelta Delta)> _proposalCache = new();
    private readonly object _lock = new();

    public OrganizationalPortfolioService(
        IPortfolioRepository repository,
        IPortfolioPlanner planner,
        IPortfolioRebalanceEngine rebalanceEngine,
        IPortfolioSimulator simulator,
        IPortfolioProvenanceService provenanceService)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _planner = planner ?? throw new ArgumentNullException(nameof(planner));
        _rebalanceEngine = rebalanceEngine ?? throw new ArgumentNullException(nameof(rebalanceEngine));
        _simulator = simulator ?? throw new ArgumentNullException(nameof(simulator));
        _provenanceService = provenanceService ?? throw new ArgumentNullException(nameof(provenanceService));
    }

    public async Task<OrganizationalPortfolio> GeneratePortfolioPlanAsync(
        string tenantId,
        IReadOnlyList<PortfolioWorkItem> candidateItems,
        AllocationPlan allocationPlan)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));
        if (allocationPlan == null) throw new ArgumentNullException(nameof(allocationPlan));

        // 1. Build capacity envelope strictly bounded by OARA allocation plan (I33-S)
        var envelope = new PortfolioCapacityEnvelope
        {
            TenantId = tenantId,
            AllocationSnapshotHash = allocationPlan.PlanId,
            EvaluatedUtc = DateTime.UtcNow
        };

        foreach (var record in allocationPlan.AdmittedAllocations)
        {
            foreach (var (type, amount) in record.AllocatedCapacities)
            {
                envelope.AvailableCapacities.TryGetValue(type, out var current);
                envelope.AvailableCapacities[type] = current + amount;
            }
        }

        // 2. Execute deterministic planning ladder (I33-T)
        var portfolio = _planner.PlanPortfolio(
            tenantId,
            candidateItems,
            envelope,
            allocationPlan.ActiveStrategicRegime);

        // Fetch version history to advance version number
        var history = await _repository.ListPortfolioHistoryAsync(tenantId);
        portfolio.VersionNumber = (history?.Count ?? 0) + 1;

        // 3. Persist to repository
        await _repository.SavePortfolioAsync(tenantId, portfolio);

        return portfolio;
    }

    public async Task<PortfolioChangeProposal?> RebalancePortfolioAsync(
        string tenantId,
        MaterialityDelta delta,
        IReadOnlyList<PortfolioWorkItem>? incomingCandidates = null)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));
        if (delta == null) throw new ArgumentNullException(nameof(delta));

        var currentPortfolio = await _repository.GetActivePortfolioAsync(tenantId);
        if (currentPortfolio == null)
        {
            return null;
        }

        var proposal = await _rebalanceEngine.EvaluateRebalanceAsync(
            tenantId,
            currentPortfolio,
            delta,
            incomingCandidates ?? Array.Empty<PortfolioWorkItem>());

        if (proposal != null)
        {
            lock (_lock)
            {
                _proposalCache[proposal.ProposalId] = (proposal, delta);
            }
        }

        return proposal;
    }

    public Task<WhyRebalanceTrace> GetRebalanceActionWhyTraceAsync(
        string tenantId,
        string proposalId,
        string workItemId)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));
        if (string.IsNullOrWhiteSpace(proposalId)) throw new ArgumentNullException(nameof(proposalId));
        if (string.IsNullOrWhiteSpace(workItemId)) throw new ArgumentNullException(nameof(workItemId));

        PortfolioChangeProposal? proposal = null;
        MaterialityDelta? delta = null;

        lock (_lock)
        {
            if (_proposalCache.TryGetValue(proposalId, out var cached))
            {
                proposal = cached.Proposal;
                delta = cached.Delta;
            }
        }

        if (proposal == null || delta == null)
        {
            throw new KeyNotFoundException($"Proposal '{proposalId}' not found in rebalance cache.");
        }

        var action = proposal.ItemActions.FirstOrDefault(a => a.WorkItemId == workItemId);
        if (action == null)
        {
            throw new KeyNotFoundException($"Work item '{workItemId}' not found in proposal '{proposalId}'.");
        }

        var trace = _provenanceService.ExplainRebalanceAction(proposal, action, delta);
        return Task.FromResult(trace);
    }

    public async Task<PortfolioSimulationResult> RunSimulationSandboxAsync(
        string tenantId,
        IReadOnlyList<PortfolioSimulationScenario> scenarios)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));

        var currentPortfolio = await _repository.GetActivePortfolioAsync(tenantId);
        if (currentPortfolio == null)
        {
            // Empty baseline sandbox
            currentPortfolio = new OrganizationalPortfolio
            {
                TenantId = tenantId,
                VersionNumber = 0
            };
        }

        // Epistemic isolation (I33-K, I33-W): Read-only sandbox execution
        return await _simulator.SimulateScenariosAsync(tenantId, currentPortfolio, scenarios);
    }

    public async Task<PortfolioChangeProposal> ProposeItemStopOrSupersedeAsync(
        string tenantId,
        string workItemId,
        string rationale,
        bool isSupersede = false)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));
        if (string.IsNullOrWhiteSpace(workItemId)) throw new ArgumentNullException(nameof(workItemId));

        var currentPortfolio = await _repository.GetActivePortfolioAsync(tenantId);
        if (currentPortfolio == null)
        {
            throw new InvalidOperationException($"No active portfolio found for tenant '{tenantId}'.");
        }

        var item = currentPortfolio.WorkItems.FirstOrDefault(i => i.WorkItemId == workItemId);
        if (item == null)
        {
            throw new KeyNotFoundException($"Work item '{workItemId}' not found in active portfolio.");
        }

        var actionType = isSupersede ? PortfolioItemAction.Supersede : PortfolioItemAction.Stop;

        var proposal = new PortfolioChangeProposal
        {
            ProposalId = Guid.NewGuid().ToString("N"),
            TenantId = tenantId,
            SourcePortfolioId = currentPortfolio.PortfolioId,
            ComputedMaterialityScore = 1.0, // Explicit human or governed mandate has 100% materiality
            StrategicJustification = rationale,
            ItemActions = new List<PortfolioItemRebalanceAction>
            {
                new PortfolioItemRebalanceAction
                {
                    WorkItemId = workItemId,
                    Title = item.Title,
                    Action = actionType,
                    TargetCapacities = new(),
                    Rationale = rationale,
                    StatusQuoImpact = "Frees allocated capacity upon governed sign-off."
                }
            }
        };

        proposal.ComputeHash();

        lock (_lock)
        {
            _proposalCache[proposal.ProposalId] = (proposal, new MaterialityDelta { RealityDelta = 1.0 });
        }

        return proposal;
    }
}
