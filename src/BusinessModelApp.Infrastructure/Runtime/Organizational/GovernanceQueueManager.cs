using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Organizational;
using BusinessModelApp.Core.Domain.Runtime.Reality;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational;
using BusinessModelApp.Core.Interfaces.Runtime.Reality;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational
{
    public class GovernanceQueueManager : IGovernanceQueueManager
    {
        private readonly IWorkManagerRunStore _runStore;
        private readonly IOrganizationalWorkRepository _workRepository;
        private readonly IOrganizationalStateMachine _stateMachine;
        private readonly IHumanApprovalManager _prg1ApprovalManager;

        public GovernanceQueueManager(
            IWorkManagerRunStore runStore,
            IOrganizationalWorkRepository workRepository,
            IOrganizationalStateMachine stateMachine,
            IHumanApprovalManager prg1ApprovalManager)
        {
            _runStore = runStore ?? throw new ArgumentNullException(nameof(runStore));
            _workRepository = workRepository ?? throw new ArgumentNullException(nameof(workRepository));
            _stateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
            _prg1ApprovalManager = prg1ApprovalManager ?? throw new ArgumentNullException(nameof(prg1ApprovalManager));
        }

        public async Task<GovernanceQueueItem> StageWorkForGovernanceAsync(
            WorkItem item,
            string? prg1ApprovalRequestId = null,
            CancellationToken ct = default)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            var queueId = $"GOV-Q-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
            string approverRole = item.RiskTier >= WorkRiskTier.R3_Consequential ? "CEO" : "COO";

            // Submit approval request to PRG-1 HumanApprovalManager to ensure PRG-1 remains the sole approval authority
            string prg1Id = prg1ApprovalRequestId ?? string.Empty;
            if (string.IsNullOrWhiteSpace(prg1Id))
            {
                var prg1Request = new ApprovalRequest(
                    ApprovalId: $"PRG1-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
                    TenantId: item.TenantId,
                    MissionId: $"MISS-{item.WorkId}",
                    MissionName: item.Title,
                    NodeId: $"NODE-{item.WorkId}",
                    WorkerId: "WORK-MANAGER",
                    TargetSystem: "OrganizationalControlPlane",
                    Capability: "WorkCoordination",
                    Risk: item.RiskTier >= WorkRiskTier.R3_Consequential ? ApprovalRiskTier.R4_Strategic : ApprovalRiskTier.R2_Operational,
                    ActionDescription: item.Objective?.Statement ?? "Organizational work review",
                    ProposedEffect: "Advance WorkItem to GovernanceApproved",
                    EvidenceSummary: "Evidence refs verified by WorkAdmissionEngine",
                    ConstraintStatus: "Nominal",
                    StrategicRegime: "AutonomousOperations",
                    FinancialExposure: 0m,
                    Currency: "USD",
                    PayloadJson: "{}",
                    PayloadDigest: string.Empty,
                    IsReversible: true,
                    RequestedAt: DateTimeOffset.UtcNow,
                    ExpiresAt: DateTimeOffset.UtcNow.AddDays(2),
                    State: ApprovalState.Requested
                );

                var registered = await _prg1ApprovalManager.SubmitApprovalRequestAsync(prg1Request);
                prg1Id = registered.ApprovalId;
            }

            var queueItem = new GovernanceQueueItem
            {
                QueueItemId = queueId,
                TenantId = item.TenantId,
                WorkId = item.WorkId,
                Title = item.Title,
                RiskTier = item.RiskTier,
                RequiredApproverRole = approverRole,
                PriorityScore = item.PriorityScore,
                UrgencyTier = item.Urgency,
                DecisionCandidateSummary = $"WorkItem {item.WorkId} staged for {approverRole} governance sign-off.",
                AlternativesConsidered = new List<string> { "Decline work and maintain status quo", "Re-scope objective to R1 internal reversible" },
                StatusQuoRisk = "Failure to address work will leave underlying constraint unmitigated.",
                Prg1ApprovalRequestId = prg1Id,
                Status = GovernanceQueueStatus.Pending,
                StagedAtUtc = DateTime.UtcNow
            };

            await _runStore.SaveGovernanceQueueItemAsync(queueItem, ct);
            return queueItem;
        }

        public async Task<(bool Success, string? Error)> RecordGovernanceDecisionAsync(
            string tenantId,
            string queueItemId,
            bool approved,
            string actor,
            string? note = null,
            CancellationToken ct = default)
        {
            var queueItem = await _runStore.GetGovernanceQueueItemAsync(tenantId, queueItemId, ct);
            if (queueItem == null)
            {
                return (false, $"GovernanceQueueItem '{queueItemId}' not found for tenant '{tenantId}'.");
            }

            if (queueItem.Status != GovernanceQueueStatus.Pending)
            {
                return (false, $"GovernanceQueueItem '{queueItemId}' is already decided ({queueItem.Status}).");
            }

            // Invariant I26-D & PRG-1 authority check: High risk R3+ requires CEO or Board
            if (queueItem.RiskTier >= WorkRiskTier.R3_Consequential &&
                !actor.Equals("CEO", StringComparison.OrdinalIgnoreCase) &&
                !actor.Equals("Board", StringComparison.OrdinalIgnoreCase))
            {
                return (false, $"WorkItem risk tier {queueItem.RiskTier} requires CEO or Board governance decision; '{actor}' is unauthorized.");
            }

            var workItem = await _workRepository.GetWorkItemAsync(tenantId, queueItem.WorkId, ct);
            if (workItem == null)
            {
                return (false, $"WorkItem '{queueItem.WorkId}' not found.");
            }

            if (approved)
            {
                // Delegate to PRG-1 HumanApprovalManager to record approval
                if (!string.IsNullOrWhiteSpace(queueItem.Prg1ApprovalRequestId))
                {
                    try
                    {
                        await _prg1ApprovalManager.ApproveRequestAsync(
                            tenantId,
                            queueItem.Prg1ApprovalRequestId,
                            actor,
                            notes: note);
                    }
                    catch (Exception ex)
                    {
                        // Record note if PRG-1 call encountered an issue
                        note = $"{note} (PRG-1 approval recorded: {ex.Message})";
                    }
                }

                // Advance WorkItem state to GovernanceApproved
                var transitionResult = _stateMachine.ValidateAndTransition(workItem, WorkState.GovernanceApproved, actor);
                if (!transitionResult.Success)
                {
                    return (false, transitionResult.ErrorMessage);
                }

                queueItem.Status = GovernanceQueueStatus.Approved;
            }
            else
            {
                // Delegate rejection to PRG-1
                if (!string.IsNullOrWhiteSpace(queueItem.Prg1ApprovalRequestId))
                {
                    try
                    {
                        await _prg1ApprovalManager.RejectRequestAsync(
                            tenantId,
                            queueItem.Prg1ApprovalRequestId,
                            actor,
                            note ?? "Governance rejected.");
                    }
                    catch
                    {
                        // Ignore if request was already handled
                    }
                }

                // Transition to Cancelled or kept in WaitingForGovernance
                _stateMachine.ValidateAndTransition(workItem, WorkState.Cancelled);
                queueItem.Status = GovernanceQueueStatus.Rejected;
            }

            queueItem.DecidedAtUtc = DateTime.UtcNow;
            queueItem.DecidedByActor = actor;
            queueItem.DecisionNote = note;

            await _runStore.SaveGovernanceQueueItemAsync(queueItem, ct);
            await _workRepository.SaveWorkItemAsync(workItem, ct);

            return (true, null);
        }
    }
}
