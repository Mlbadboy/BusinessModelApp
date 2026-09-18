using BusinessModelApp.Core.Domain.Runtime.Organizational.Allocation;

namespace BusinessModelApp.Core.Interfaces.Runtime.Organizational;

/// <summary>
/// Catalog and capacity registry for scarce organizational resources.
/// </summary>
public interface IResourceRegistry
{
    Task RegisterResourceAsync(string tenantId, OrganizationalResource resource);
    Task<OrganizationalResource?> GetResourceAsync(string tenantId, string resourceId);
    Task<IReadOnlyList<OrganizationalResource>> ListResourcesAsync(string tenantId);
    Task<bool> UpdateResourceCapacityAsync(string tenantId, string resourceId, double deltaAvailable, double deltaReserved);
}

/// <summary>
/// Resolves capacity availability through epistemic filters.
/// Invariant I32-U: Unmeasured capacity strictly defaults to Unknown.
/// </summary>
public interface IResourceAvailabilityResolver
{
    Task<(double UsableCapacity, ResourceCapacityReality Reality)> ResolveUsableCapacityAsync(string tenantId, OrganizationalResourceType type);
}

/// <summary>
/// Engine for managing cognitive organizational attention budgets.
/// Invariant I32-A: Attention != Priority.
/// </summary>
public interface IAttentionBudgetEngine
{
    Task<AttentionBudget> GetAttentionBudgetAsync(string tenantId);
    Task<bool> CommitAttentionUnitsAsync(string tenantId, string domain, double units);
    Task ReleaseAttentionUnitsAsync(string tenantId, string domain, double units);
}

/// <summary>
/// Evaluator ensuring that hard business constraints cannot be bypassed.
/// Invariant I32-G: Constraints Cannot Be Optimized Away.
/// Invariant I32-R: Deterministic Arbitration Non-Compensation.
/// </summary>
public interface IAllocationConstraintEvaluator
{
    (bool IsAdmissible, string? RejectionReason) EvaluateHardConstraints(
        string tenantId,
        AllocationDemand demand,
        IReadOnlyList<OrganizationalResource> availableResources);
}

/// <summary>
/// 14-Stage Lexicographic Decision Ladder Arbitrator.
/// Invariant I32-E: Scarcity Sovereignty.
/// Invariant I32-F: No Resource Monopolization.
/// Invariant I32-R: Non-Compensation.
/// </summary>
public interface IAllocationArbitrator
{
    AllocationPlan ArbitrateDemands(
        string tenantId,
        IReadOnlyList<AllocationDemand> demands,
        IReadOnlyList<OrganizationalResource> resources,
        string activeStrategicRegime = "GrowthAndResilience");
}

/// <summary>
/// Thread-safe multi-tenant ledger for allocation records and advance reservations.
/// Invariant I32-D: Resource Reservation != Resource Consumption.
/// Invariant I32-N: Allocation Expiry.
/// Invariant I32-T: Reservation Authority Uniqueness.
/// </summary>
public interface IAllocationLedger
{
    Task SaveRecordAsync(string tenantId, AllocationRecord record);
    Task<AllocationRecord?> GetRecordAsync(string tenantId, string allocationId);
    Task<IReadOnlyList<AllocationRecord>> ListRecordsAsync(string tenantId);
    Task<int> ExpireStaleReservationsAsync(string tenantId);
}

/// <summary>
/// Persistent tracker for Resource Allocation Debt.
/// </summary>
public interface IResourceDebtTracker
{
    Task<ResourceDebtRecord> RecordShortfallDebtAsync(string tenantId, ResourceDebtType type, double amount, string description);
    Task<IReadOnlyList<ResourceDebtRecord>> ListDebtsAsync(string tenantId);
    Task<bool> RemediateDebtAsync(string tenantId, string debtId);
}

/// <summary>
/// Provenance service explaining "Why Allocated?" and "Why Not More?".
/// </summary>
public interface IAllocationProvenanceService
{
    WhyAllocationTrace ExplainAllocation(
        AllocationRecord record,
        AllocationCandidate candidate,
        IReadOnlyList<AllocationCandidate> competingCandidates);
}

/// <summary>
/// Unified facade coordinating Organizational Attention & Resource Allocation.
/// </summary>
public interface IOrganizationalAllocationService
{
    Task<AllocationPlan> GenerateAllocationPlanAsync(string tenantId, IReadOnlyList<AllocationDemand> demands);
    Task<WhyAllocationTrace> GetWhyAllocationTraceAsync(string tenantId, string allocationId);
    Task<bool> ReleaseAllocationReservationAsync(string tenantId, string allocationId);
    Task<IReadOnlyList<ResourceDebtRecord>> GetTenantResourceDebtsAsync(string tenantId);
}
