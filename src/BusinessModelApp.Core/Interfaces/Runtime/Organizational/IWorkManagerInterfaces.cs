using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Missions;
using BusinessModelApp.Core.Domain.Runtime.Organizational;

namespace BusinessModelApp.Core.Interfaces.Runtime.Organizational
{
    public interface IWorkManagerRunStore
    {
        Task SaveRunAsync(WorkManagerRun run, CancellationToken ct = default);
        Task<WorkManagerRun?> GetRunAsync(string tenantId, string runId, CancellationToken ct = default);
        Task<IReadOnlyList<WorkManagerRun>> ListRunsAsync(string tenantId, CancellationToken ct = default);

        Task SaveGovernanceQueueItemAsync(GovernanceQueueItem item, CancellationToken ct = default);
        Task<GovernanceQueueItem?> GetGovernanceQueueItemAsync(string tenantId, string queueItemId, CancellationToken ct = default);
        Task<IReadOnlyList<GovernanceQueueItem>> ListGovernanceQueueAsync(string tenantId, GovernanceQueueStatus? status = null, CancellationToken ct = default);

        Task SaveMissionProposalLinkAsync(string tenantId, string workId, MissionGraphProposal proposal, CancellationToken ct = default);
        Task<MissionGraphProposal?> GetMissionProposalLinkAsync(string tenantId, string workId, CancellationToken ct = default);
    }

    public interface IWorkPortfolioPrioritizer
    {
        Task<IReadOnlyList<WorkPortfolioRank>> RankPortfolioAsync(
            string tenantId,
            IReadOnlyList<WorkItem> activeItems,
            int maxItems = 50,
            CancellationToken ct = default);
    }

    public interface IWorkDecompositionEngine
    {
        Task<(bool Success, WorkPlan? Plan, MissionGraphProposal? Proposal, string? Error)> DecomposeWorkAsync(
            WorkItem item,
            CancellationToken ct = default);
    }

    public interface IGovernanceQueueManager
    {
        Task<GovernanceQueueItem> StageWorkForGovernanceAsync(
            WorkItem item,
            string? prg1ApprovalRequestId = null,
            CancellationToken ct = default);

        Task<(bool Success, string? Error)> RecordGovernanceDecisionAsync(
            string tenantId,
            string queueItemId,
            bool approved,
            string actor,
            string? note = null,
            CancellationToken ct = default);
    }

    public interface IAutonomousWorkManager
    {
        Task<WorkManagerCycleResult> ExecuteCycleAsync(
            string tenantId,
            ManagerTriggerType triggerType = ManagerTriggerType.ScheduledTick,
            string? triggerId = null,
            WorkManagerBudget? budget = null,
            CancellationToken ct = default);
    }
}
