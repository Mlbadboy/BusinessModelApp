using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Organizational;

namespace BusinessModelApp.Core.Interfaces.Runtime.Organizational
{
    public interface IOrganizationalMemoryStore
    {
        Task SavePrecedentAsync(OrganizationalPrecedent precedent, CancellationToken ct = default);
        Task<OrganizationalPrecedent?> GetPrecedentAsync(string tenantId, string precedentId, CancellationToken ct = default);
        Task<IReadOnlyList<OrganizationalPrecedent>> ListPrecedentsAsync(string tenantId, CancellationToken ct = default);

        Task SaveAntiPatternAsync(OrganizationalAntiPattern antiPattern, CancellationToken ct = default);
        Task<OrganizationalAntiPattern?> GetAntiPatternAsync(string tenantId, string antiPatternId, CancellationToken ct = default);
        Task<IReadOnlyList<OrganizationalAntiPattern>> ListAntiPatternsAsync(string tenantId, string? domain = null, CancellationToken ct = default);

        Task SaveTrajectoryAsync(OrganizationalTrajectory trajectory, CancellationToken ct = default);
        Task<OrganizationalTrajectory?> GetTrajectoryAsync(string tenantId, string trajectoryId, CancellationToken ct = default);
        Task<OrganizationalTrajectory?> GetTrajectoryForWorkAsync(string tenantId, string workId, CancellationToken ct = default);
        Task<IReadOnlyList<OrganizationalTrajectory>> ListTrajectoriesAsync(string tenantId, CancellationToken ct = default);

        Task SaveSnapshotAsync(OrganizationalContextSnapshot snapshot, CancellationToken ct = default);
        Task<OrganizationalContextSnapshot?> GetSnapshotAsync(string tenantId, string snapshotId, CancellationToken ct = default);

        Task SaveProvenanceLineageAsync(MemoryProvenanceLineage lineage, CancellationToken ct = default);
        Task<MemoryProvenanceLineage?> GetProvenanceLineageAsync(string tenantId, string memoryId, CancellationToken ct = default);
    }

    public interface IMemoryWriteGate
    {
        Task<(bool Permitted, string? Reason)> ValidateWriteAsync(
            string tenantId,
            string callerRole,
            string sourceRecordType,
            string evidenceRef,
            CancellationToken ct = default);
    }

    public interface IOrganizationalContextAssembler
    {
        Task<OrganizationalContextSnapshot> AssembleContextAsync(
            string tenantId,
            string workId,
            ContextAssemblyPolicy? policyOverride = null,
            CancellationToken ct = default);
    }

    public interface IWorkTrajectoryRecorder
    {
        Task<OrganizationalTrajectory> StartTrajectoryAsync(
            string tenantId,
            string responsibilityId,
            string proposalId,
            string workId,
            CancellationToken ct = default);

        Task RecordMilestoneAsync(
            string tenantId,
            string workId,
            Action<OrganizationalTrajectory> updateAction,
            CancellationToken ct = default);
    }

    public interface IMemoryFreshnessEvaluator
    {
        Task<FreshnessEvaluationResult> EvaluateFreshnessAsync(
            string tenantId,
            string precedentId,
            TimeSpan age,
            string? currentRegime = null,
            CancellationToken ct = default);
    }

    public interface IOrganizationalMemoryService
    {
        Task<OrganizationalContextSnapshot> GetContextForWorkAsync(
            string tenantId,
            string workId,
            ContextAssemblyPolicy? policy = null,
            CancellationToken ct = default);

        Task<OrganizationalTrajectory?> GetTrajectoryForWorkAsync(
            string tenantId,
            string workId,
            CancellationToken ct = default);

        Task<MemoryProvenanceLineage?> GetMemoryProvenanceAsync(
            string tenantId,
            string memoryId,
            CancellationToken ct = default);

        Task<IReadOnlyList<OrganizationalAntiPattern>> GetAntiPatternsForDomainAsync(
            string tenantId,
            string domain,
            CancellationToken ct = default);

        Task<FreshnessEvaluationResult> EvaluateMemoryFreshnessAsync(
            string tenantId,
            string precedentId,
            TimeSpan age,
            string? currentRegime = null,
            CancellationToken ct = default);
    }
}
