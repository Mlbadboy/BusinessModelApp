using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Organizational;

namespace BusinessModelApp.Core.Interfaces.Runtime.Organizational
{
    public interface IOrganizationalWorkRepository
    {
        // Responsibility operations
        Task<OrganizationalResponsibility?> GetResponsibilityAsync(string tenantId, string responsibilityId, CancellationToken ct = default);
        Task<IReadOnlyList<OrganizationalResponsibility>> ListResponsibilitiesAsync(string tenantId, CancellationToken ct = default);
        Task SaveResponsibilityAsync(OrganizationalResponsibility responsibility, CancellationToken ct = default);

        // Proposal operations
        Task<WorkProposal?> GetProposalAsync(string tenantId, string proposalId, CancellationToken ct = default);
        Task<IReadOnlyList<WorkProposal>> ListProposalsAsync(string tenantId, CancellationToken ct = default);
        Task SaveProposalAsync(WorkProposal proposal, CancellationToken ct = default);

        // WorkItem operations
        Task<WorkItem?> GetWorkItemAsync(string tenantId, string workId, CancellationToken ct = default);
        Task<IReadOnlyList<WorkItem>> ListWorkItemsAsync(string tenantId, WorkState? stateFilter = null, CancellationToken ct = default);
        Task SaveWorkItemAsync(WorkItem item, CancellationToken ct = default);

        // WorkPlan operations
        Task<WorkPlan?> GetWorkPlanAsync(string tenantId, string planId, CancellationToken ct = default);
        Task SaveWorkPlanAsync(string tenantId, WorkPlan plan, CancellationToken ct = default);

        // Dependency operations
        Task<IReadOnlyList<WorkDependency>> GetDependenciesForWorkAsync(string tenantId, string workId, CancellationToken ct = default);
        Task SaveDependencyAsync(string tenantId, WorkDependency dependency, CancellationToken ct = default);

        // Lineage operations
        Task SaveLineageRecordAsync(string tenantId, WorkMissionLineageRecord record, CancellationToken ct = default);
        Task<WorkMissionLineageRecord?> GetLineageRecordAsync(string tenantId, string workId, CancellationToken ct = default);
    }

    public interface IOrganizationalWorkAdmissionEngine
    {
        Task<(bool Admitted, string? RejectionReason, WorkItem? Item)> EvaluateAndAdmitAsync(
            WorkProposal proposal,
            CancellationToken ct = default);
    }

    public interface IOrganizationalStateMachine
    {
        (bool Success, string? ErrorMessage, WorkState NewState) ValidateAndTransition(
            WorkItem item,
            WorkState targetState,
            string? governanceApprovalActor = null,
            string? verificationEvidenceHash = null);
    }

    public interface IWorkDependencyResolver
    {
        bool HasCircularDependency(string tenantId, string dependentWorkId, string requiredWorkId);
        Task<bool> IsWorkBlockedAsync(string tenantId, string workId, CancellationToken ct = default);
        Task<IReadOnlyList<string>> GetBlockingPrerequisitesAsync(string tenantId, string workId, CancellationToken ct = default);
    }

    public interface IWorkCommitmentMonitor
    {
        Task<IReadOnlyList<WorkCommitment>> EvaluateCommitmentsAsync(string tenantId, CancellationToken ct = default);
    }

    public interface IWorkOutcomeVerifier
    {
        (bool Verified, double CalculatedSuccessScore, string VerificationHash, string? ErrorMessage) VerifyOutcome(
            WorkItem item,
            string claimedSummary,
            Dictionary<string, double> expectedMetrics,
            Dictionary<string, double> actualMetrics,
            string evidenceSha256,
            string verifierActor);
    }
}
