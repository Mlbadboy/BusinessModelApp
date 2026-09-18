using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Organizational;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational
{
    public class AutonomousWorkManager : IAutonomousWorkManager
    {
        private readonly IOrganizationalWorkRepository _workRepository;
        private readonly IWorkManagerRunStore _runStore;
        private readonly IOrganizationalWorkAdmissionEngine _admissionEngine;
        private readonly IOrganizationalStateMachine _stateMachine;
        private readonly IWorkDependencyResolver _dependencyResolver;
        private readonly IWorkCommitmentMonitor _commitmentMonitor;
        private readonly IWorkPortfolioPrioritizer _prioritizer;
        private readonly IWorkDecompositionEngine _decompositionEngine;
        private readonly IGovernanceQueueManager _governanceQueueManager;

        public AutonomousWorkManager(
            IOrganizationalWorkRepository workRepository,
            IWorkManagerRunStore runStore,
            IOrganizationalWorkAdmissionEngine admissionEngine,
            IOrganizationalStateMachine stateMachine,
            IWorkDependencyResolver dependencyResolver,
            IWorkCommitmentMonitor commitmentMonitor,
            IWorkPortfolioPrioritizer prioritizer,
            IWorkDecompositionEngine decompositionEngine,
            IGovernanceQueueManager governanceQueueManager)
        {
            _workRepository = workRepository ?? throw new ArgumentNullException(nameof(workRepository));
            _runStore = runStore ?? throw new ArgumentNullException(nameof(runStore));
            _admissionEngine = admissionEngine ?? throw new ArgumentNullException(nameof(admissionEngine));
            _stateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
            _dependencyResolver = dependencyResolver ?? throw new ArgumentNullException(nameof(dependencyResolver));
            _commitmentMonitor = commitmentMonitor ?? throw new ArgumentNullException(nameof(commitmentMonitor));
            _prioritizer = prioritizer ?? throw new ArgumentNullException(nameof(prioritizer));
            _decompositionEngine = decompositionEngine ?? throw new ArgumentNullException(nameof(decompositionEngine));
            _governanceQueueManager = governanceQueueManager ?? throw new ArgumentNullException(nameof(governanceQueueManager));
        }

        public async Task<WorkManagerCycleResult> ExecuteCycleAsync(
            string tenantId,
            ManagerTriggerType triggerType = ManagerTriggerType.ScheduledTick,
            string? triggerId = null,
            WorkManagerBudget? budget = null,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentException("TenantId is required.", nameof(tenantId));
            budget ??= new WorkManagerBudget();
            var sw = Stopwatch.StartNew();

            var runId = $"RUN-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
            var effectiveTriggerId = triggerId ?? $"TRIG-{DateTime.UtcNow:yyyyMMddHHmmss}";

            var result = new WorkManagerCycleResult
            {
                RunId = runId,
                TenantId = tenantId,
                TimestampUtc = DateTime.UtcNow
            };

            // 1. Snapshot generation & Idempotency check
            var activeResponsibilities = await _workRepository.ListResponsibilitiesAsync(tenantId, ct);
            var activeItems = await _workRepository.ListWorkItemsAsync(tenantId, null, ct);
            var pendingProposals = await _workRepository.ListProposalsAsync(tenantId, ct);

            string inputSnapshot = $"{tenantId}:{activeResponsibilities.Count}:{activeItems.Count}:{pendingProposals.Count(p => p.AdmissionStatus == ProposalAdmissionStatus.Pending)}";
            string policySnapshot = "PSP-DEFAULT-v1:WPP-DEFAULT-v1";

            using var sha = SHA256.Create();
            string inputHash = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(inputSnapshot)));
            string policyHash = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(policySnapshot)));

            var managerRun = new WorkManagerRun
            {
                ManagerRunId = runId,
                TenantId = tenantId,
                TriggerType = triggerType,
                TriggerId = effectiveTriggerId,
                StartedUtc = DateTime.UtcNow,
                InputSnapshotHash = inputHash,
                PolicySnapshotHash = policyHash,
                Status = ManagerRunStatus.Running
            };

            // Idempotency: Check if an identical run was completed
            string idempotencyKey = managerRun.ComputeIdempotencyKey();
            var existingRuns = await _runStore.ListRunsAsync(tenantId, ct);
            var duplicateRun = existingRuns.FirstOrDefault(r =>
                r.Status == ManagerRunStatus.Completed &&
                (r.ComputeIdempotencyKey() == idempotencyKey || (!string.IsNullOrWhiteSpace(triggerId) && r.TriggerId == triggerId)) &&
                r.CompletedUtc.HasValue &&
                (DateTime.UtcNow - r.CompletedUtc.Value).TotalSeconds < 30); // 30s idempotency window

            if (duplicateRun != null)
            {
                // Return cached idempotent result
                sw.Stop();
                result.RunId = duplicateRun.ManagerRunId;
                result.Success = true;
                result.DurationMs = sw.ElapsedMilliseconds;
                result.ResultSummaryHash = duplicateRun.ResultHash;
                return result;
            }

            await _runStore.SaveRunAsync(managerRun, ct);

            try
            {
                // 2. Bounded Candidate Responsibilities Selection
                var candidateResponsibilities = activeResponsibilities
                    .Where(r => r.IsActive)
                    .Take(budget.MaxCandidateResponsibilities)
                    .ToList();
                result.ResponsibilitiesEvaluated = candidateResponsibilities.Count;

                // 3. Ingest and Admit Pending Proposals (bounded by MaxProposalsPerCycle)
                var unadmitted = pendingProposals
                    .Where(p => p.AdmissionStatus == ProposalAdmissionStatus.Pending)
                    .Take(budget.MaxProposalsPerCycle)
                    .ToList();

                foreach (var prop in unadmitted)
                {
                    var (admitted, _, _) = await _admissionEngine.EvaluateAndAdmitAsync(prop, ct);
                    if (admitted)
                    {
                        result.ProposalsEvaluated++;
                    }
                }

                // 4. Budget Runtime Check (Fail-Closed)
                if (sw.ElapsedMilliseconds >= budget.MaxManagerRuntimeMs)
                {
                    managerRun.Status = ManagerRunStatus.BudgetExceeded;
                    result.Success = false;
                    result.ErrorMessage = $"Cycle exceeded MaxManagerRuntimeMs ({budget.MaxManagerRuntimeMs}ms).";
                    await FinishRunAsync(managerRun, result, sw, ct);
                    return result;
                }

                // Refresh active work items
                var currentItems = await _workRepository.ListWorkItemsAsync(tenantId, null, ct);

                // 5. Rank Portfolio (Theory of Constraints + Starvation Protection)
                var rankedPortfolio = await _prioritizer.RankPortfolioAsync(
                    tenantId,
                    currentItems.Where(i => i.State != WorkState.Closed && i.State != WorkState.Cancelled).ToList(),
                    budget.MaxWorkItemsPerCycle,
                    ct);

                // 6. Decompose Qualified Items into WorkPlans & MissionGraphProposals
                int decompositionsDone = 0;
                foreach (var rankEntry in rankedPortfolio)
                {
                    if (decompositionsDone >= budget.MaxDecompositionsPerCycle)
                        break;

                    var item = currentItems.FirstOrDefault(i => i.WorkId == rankEntry.WorkId);
                    if (item == null) continue;

                    // Advance Detected to Qualified
                    if (item.State == WorkState.Detected)
                    {
                        var trans = _stateMachine.ValidateAndTransition(item, WorkState.Qualified);
                        if (trans.Success)
                        {
                            await _workRepository.SaveWorkItemAsync(item, ct);
                        }
                    }

                    // Advance Qualified to Planned via Decomposition
                    if (item.State == WorkState.Qualified)
                    {
                        var (decSuccess, _, proposal, _) = await _decompositionEngine.DecomposeWorkAsync(item, ct);
                        if (decSuccess)
                        {
                            decompositionsDone++;
                            result.WorkItemsPlanned++;
                            if (proposal != null)
                            {
                                result.MissionProposalsEmitted++;
                            }

                            _stateMachine.ValidateAndTransition(item, WorkState.Planned);
                            _stateMachine.ValidateAndTransition(item, WorkState.Assigned);
                            _stateMachine.ValidateAndTransition(item, WorkState.Preparing);
                            await _workRepository.SaveWorkItemAsync(item, ct);
                        }
                    }
                }

                // 7. Resolve Dependencies (Check for unblocked items)
                foreach (var item in currentItems.Where(i => i.State == WorkState.Blocked))
                {
                    var isStillBlocked = await _dependencyResolver.IsWorkBlockedAsync(tenantId, item.WorkId, ct);
                    if (!isStillBlocked)
                    {
                        var unblockResult = _stateMachine.ValidateAndTransition(item, WorkState.Preparing);
                        if (unblockResult.Success)
                        {
                            await _workRepository.SaveWorkItemAsync(item, ct);
                            result.DependenciesResolved++;
                        }
                    }
                }

                // 8. Audit Commitments & SLAs
                var evaluatedCommitments = await _commitmentMonitor.EvaluateCommitmentsAsync(tenantId, ct);
                result.CommitmentsAudited = evaluatedCommitments.Count;
                result.EscalationsTriggered = evaluatedCommitments.Count(c => c.Status == CommitmentStatus.Breached);

                // 9. Stage Preparing items to WaitingForGovernance, and GovernanceApproved to ExecutionAdmitted
                foreach (var item in currentItems.Where(i => i.State == WorkState.Preparing))
                {
                    // If no blocker, transition to WaitingForGovernance
                    var isBlocked = await _dependencyResolver.IsWorkBlockedAsync(tenantId, item.WorkId, ct);
                    if (!isBlocked)
                    {
                        var trans = _stateMachine.ValidateAndTransition(item, WorkState.WaitingForGovernance);
                        if (trans.Success)
                        {
                            await _workRepository.SaveWorkItemAsync(item, ct);
                            await _governanceQueueManager.StageWorkForGovernanceAsync(item, null, ct);
                            result.GovernanceItemsStaged++;
                        }
                    }
                }

                // Advance GovernanceApproved items to ExecutionAdmitted
                foreach (var item in currentItems.Where(i => i.State == WorkState.GovernanceApproved))
                {
                    var trans = _stateMachine.ValidateAndTransition(item, WorkState.ExecutionAdmitted);
                    if (trans.Success)
                    {
                        await _workRepository.SaveWorkItemAsync(item, ct);
                    }
                }

                managerRun.Status = ManagerRunStatus.Completed;
                result.Success = true;
            }
            catch (Exception ex)
            {
                managerRun.Status = ManagerRunStatus.Failed;
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            await FinishRunAsync(managerRun, result, sw, ct);
            return result;
        }

        private async Task FinishRunAsync(
            WorkManagerRun run,
            WorkManagerCycleResult result,
            Stopwatch sw,
            CancellationToken ct)
        {
            sw.Stop();
            result.DurationMs = sw.ElapsedMilliseconds;
            run.CompletedUtc = DateTime.UtcNow;

            var summary = $"Props:{result.ProposalsEvaluated};Plans:{result.WorkItemsPlanned};Deps:{result.DependenciesResolved};Gov:{result.GovernanceItemsStaged};Missions:{result.MissionProposalsEmitted}";
            run.ComputeResultHash(summary);
            result.ResultSummaryHash = run.ResultHash;

            await _runStore.SaveRunAsync(run, ct);
        }
    }
}
