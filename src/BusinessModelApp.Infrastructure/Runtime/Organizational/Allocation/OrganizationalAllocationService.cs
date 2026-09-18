using BusinessModelApp.Core.Domain.Runtime.Organizational.Allocation;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational.Allocation;

/// <summary>
/// Unified facade coordinating Organizational Attention & Resource Allocation (OARA).
/// Preserves:
/// - Invariant I32: Attention != Priority != Resource != Reservation != Allocation != Authority != Execution.
/// - Invariant I32-C: Allocation != Authorization.
/// - Invariant I32-K: Allocation != Execution Permit.
/// - Invariant I32-V: ArbitrationAdmitted != HumanApproved != ExecutionPermit.
/// </summary>
public class OrganizationalAllocationService : IOrganizationalAllocationService
{
    private readonly IResourceRegistry _resourceRegistry;
    private readonly IAllocationArbitrator _arbitrator;
    private readonly IAllocationLedger _ledger;
    private readonly IResourceDebtTracker _debtTracker;
    private readonly IAllocationProvenanceService _provenanceService;
    private readonly IAttentionBudgetEngine _attentionEngine;

    // Cache of recent evaluated candidates by demandId for why-provenance generation
    private readonly Dictionary<string, (AllocationCandidate Candidate, List<AllocationCandidate> EpochCandidates)> _candidateCache = new();
    private readonly object _lock = new();

    public OrganizationalAllocationService(
        IResourceRegistry resourceRegistry,
        IAllocationArbitrator arbitrator,
        IAllocationLedger ledger,
        IResourceDebtTracker debtTracker,
        IAllocationProvenanceService provenanceService,
        IAttentionBudgetEngine attentionEngine)
    {
        _resourceRegistry = resourceRegistry ?? throw new ArgumentNullException(nameof(resourceRegistry));
        _arbitrator = arbitrator ?? throw new ArgumentNullException(nameof(arbitrator));
        _ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));
        _debtTracker = debtTracker ?? throw new ArgumentNullException(nameof(debtTracker));
        _provenanceService = provenanceService ?? throw new ArgumentNullException(nameof(provenanceService));
        _attentionEngine = attentionEngine ?? throw new ArgumentNullException(nameof(attentionEngine));
    }

    public async Task<AllocationPlan> GenerateAllocationPlanAsync(string tenantId, IReadOnlyList<AllocationDemand> demands)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));

        // 1. Expire any stale reservations first (I32-N)
        await _ledger.ExpireStaleReservationsAsync(tenantId);

        // 2. Fetch all registered resources for tenant
        var resources = await _resourceRegistry.ListResourcesAsync(tenantId);

        // 3. Run 14-stage Lexicographic Decision Ladder Arbitration (I32-R, I32-E)
        var plan = _arbitrator.ArbitrateDemands(tenantId, demands, resources);

        // 4. Cache candidates for provenance explanations and commit admitted records to ledger
        lock (_lock)
        {
            foreach (var candidate in plan.EvaluatedCandidates)
            {
                _candidateCache[candidate.Demand.DemandId] = (candidate, plan.EvaluatedCandidates);
            }
        }

        foreach (var record in plan.AdmittedAllocations)
        {
            await _ledger.SaveRecordAsync(tenantId, record);

            // Update registered resource allocations
            foreach (var (type, amount) in record.AllocatedCapacities)
            {
                var matchingResource = resources.FirstOrDefault(r => r.ResourceType == type);
                if (matchingResource != null)
                {
                    await _resourceRegistry.UpdateResourceCapacityAsync(tenantId, matchingResource.ResourceId, -amount, amount);
                }

                // If HumanAttention was allocated, commit attention budget (I32-A)
                if (type == OrganizationalResourceType.HumanAttention)
                {
                    await _attentionEngine.CommitAttentionUnitsAsync(tenantId, "General", amount);
                }
            }
        }

        // 5. Track accumulated resource debts
        foreach (var debt in plan.AccumulatedDebts)
        {
            await _debtTracker.RecordShortfallDebtAsync(tenantId, debt.DebtType, debt.ShortfallAmount, debt.Description);
        }

        return plan;
    }

    public async Task<WhyAllocationTrace> GetWhyAllocationTraceAsync(string tenantId, string allocationId)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));
        if (string.IsNullOrWhiteSpace(allocationId)) throw new ArgumentNullException(nameof(allocationId));

        var record = await _ledger.GetRecordAsync(tenantId, allocationId);
        if (record == null)
        {
            throw new KeyNotFoundException($"Allocation record '{allocationId}' not found for tenant '{tenantId}'.");
        }

        AllocationCandidate? candidate = null;
        List<AllocationCandidate>? epochCandidates = null;

        lock (_lock)
        {
            if (_candidateCache.TryGetValue(record.DemandId, out var cached))
            {
                candidate = cached.Candidate;
                epochCandidates = cached.EpochCandidates;
            }
        }

        if (candidate == null)
        {
            // Synthesize a candidate from the record if evicted from memory cache
            candidate = new AllocationCandidate
            {
                Demand = new AllocationDemand
                {
                    DemandId = record.DemandId,
                    TenantId = record.TenantId,
                    Title = $"Demand for {record.TargetProposalId}",
                    TargetProposalId = record.TargetProposalId,
                    TargetResponsibilityId = record.TargetResponsibilityId,
                    RequestedCapacities = new Dictionary<OrganizationalResourceType, double>(record.AllocatedCapacities)
                },
                GrantedCapacities = new Dictionary<OrganizationalResourceType, double>(record.AllocatedCapacities),
                RecommendedPosture = record.Posture,
                PassesHardConstraints = true
            };
            epochCandidates = new List<AllocationCandidate> { candidate };
        }

        return _provenanceService.ExplainAllocation(record, candidate, epochCandidates ?? new List<AllocationCandidate>());
    }

    public async Task<bool> ReleaseAllocationReservationAsync(string tenantId, string allocationId)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));
        if (string.IsNullOrWhiteSpace(allocationId)) throw new ArgumentNullException(nameof(allocationId));

        var record = await _ledger.GetRecordAsync(tenantId, allocationId);
        if (record == null) return false;

        if (record.State == AllocationLifecycleState.Released || record.State == AllocationLifecycleState.Expired)
        {
            return false;
        }

        record.State = AllocationLifecycleState.Released;
        await _ledger.SaveRecordAsync(tenantId, record);

        // Restore capacity back to resource registry
        var resources = await _resourceRegistry.ListResourcesAsync(tenantId);
        foreach (var (type, amount) in record.AllocatedCapacities)
        {
            var matchingResource = resources.FirstOrDefault(r => r.ResourceType == type);
            if (matchingResource != null)
            {
                await _resourceRegistry.UpdateResourceCapacityAsync(tenantId, matchingResource.ResourceId, amount, -amount);
            }

            if (type == OrganizationalResourceType.HumanAttention)
            {
                await _attentionEngine.ReleaseAttentionUnitsAsync(tenantId, "General", amount);
            }
        }

        return true;
    }

    public async Task<IReadOnlyList<ResourceDebtRecord>> GetTenantResourceDebtsAsync(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));
        return await _debtTracker.ListDebtsAsync(tenantId);
    }
}
