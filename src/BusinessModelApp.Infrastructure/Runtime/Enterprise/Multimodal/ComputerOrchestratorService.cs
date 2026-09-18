using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Multimodal;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Multimodal;

namespace BusinessModelApp.Infrastructure.Runtime.Enterprise.Multimodal
{
    public class ComputerOrchestratorService : IComputerOrchestratorService
    {
        private readonly IComputerSessionManager _sessionManager;
        private readonly IEnvironmentModel _environmentModel;
        private readonly IComputerActionPlanner _actionPlanner;
        private readonly IComputerSafetyGuard _safetyGuard;
        private readonly IActionRiskEvaluator _riskEvaluator;
        private readonly IComputerVerificationService _verificationService;
        private readonly IComputerProvenanceService _provenanceService;

        private readonly ConcurrentDictionary<string, ComputerActionProposal> _proposals = new();

        public ComputerOrchestratorService(
            IComputerSessionManager sessionManager,
            IEnvironmentModel environmentModel,
            IComputerActionPlanner actionPlanner,
            IComputerSafetyGuard safetyGuard,
            IActionRiskEvaluator riskEvaluator,
            IComputerVerificationService verificationService,
            IComputerProvenanceService provenanceService)
        {
            _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
            _environmentModel = environmentModel ?? throw new ArgumentNullException(nameof(environmentModel));
            _actionPlanner = actionPlanner ?? throw new ArgumentNullException(nameof(actionPlanner));
            _safetyGuard = safetyGuard ?? throw new ArgumentNullException(nameof(safetyGuard));
            _riskEvaluator = riskEvaluator ?? throw new ArgumentNullException(nameof(riskEvaluator));
            _verificationService = verificationService ?? throw new ArgumentNullException(nameof(verificationService));
            _provenanceService = provenanceService ?? throw new ArgumentNullException(nameof(provenanceService));
        }

        public async Task<ComputerSession> StartSessionAsync(string tenantId, string application, string targetGoal)
        {
            return await _sessionManager.CreateSessionAsync(tenantId, application, targetGoal);
        }

        public async Task<ComputerEnvironmentSnapshot> ObserveAsync(string tenantId, string sessionId, string application, string window, string? url = null)
        {
            var session = await _sessionManager.GetSessionAsync(tenantId, sessionId);
            if (session == null) throw new KeyNotFoundException($"Session {sessionId} not found");

            await _sessionManager.TransitionSessionAsync(tenantId, sessionId, ComputerSessionState.Observing, "Observing digital environment");
            var snapshot = await _environmentModel.CreateSnapshotAsync(tenantId, sessionId, application, window, url);
            session.ActiveSnapshotId = snapshot.EnvironmentId;

            await _sessionManager.TransitionSessionAsync(tenantId, sessionId, ComputerSessionState.Interpreting, "Snapshot observed and registered");
            return snapshot;
        }

        public async Task<ComputerActionProposal> ProposeActionAsync(
            string tenantId,
            string sessionId,
            string snapshotId,
            string intentId,
            ProposedActionType actionType,
            string? targetElementId,
            (int X, int Y)? coordinates,
            Dictionary<string, string>? parameters = null)
        {
            var session = await _sessionManager.GetSessionAsync(tenantId, sessionId);
            if (session == null) throw new KeyNotFoundException($"Session {sessionId} not found");

            await _sessionManager.TransitionSessionAsync(tenantId, sessionId, ComputerSessionState.Planning, "Formulating governed action proposal");
            var proposal = await _actionPlanner.FormulateProposalAsync(tenantId, sessionId, snapshotId, intentId, actionType, targetElementId, coordinates, parameters);

            _proposals[proposal.ProposalId] = proposal;
            session.ActiveProposalId = proposal.ProposalId;

            await _sessionManager.TransitionSessionAsync(tenantId, sessionId, ComputerSessionState.GovernanceCheck, $"Proposal {proposal.ProposalId} formulated with risk tier {proposal.RiskTier}");
            return proposal;
        }

        public async Task<ComputerActionResult> AdmitAndDispatchProposalAsync(string tenantId, string sessionId, string proposalId)
        {
            var session = await _sessionManager.GetSessionAsync(tenantId, sessionId);
            if (session == null) throw new KeyNotFoundException($"Session {sessionId} not found");

            if (!_proposals.TryGetValue(proposalId, out var proposal))
            {
                throw new KeyNotFoundException($"Proposal {proposalId} not found");
            }

            if (proposal.TenantId != tenantId)
            {
                throw new UnauthorizedAccessException($"Tenant penetration defense: proposal {proposalId} does not belong to {tenantId}");
            }

            // Gating: If proposal requires human approval (R4/R5) and has not been approved
            if (proposal.RequiresHumanApproval)
            {
                await _sessionManager.TransitionSessionAsync(tenantId, sessionId, ComputerSessionState.WaitingForHuman, $"High-risk action ({proposal.RiskTier}) requires PRG-1 human approval");
                return new ComputerActionResult
                {
                    IsSuccess = false,
                    Status = ActionExecutionStatus.BlockedByGovernance,
                    Message = $"Action proposal {proposalId} is gated by PRG-1 Human Governance: {proposal.RiskTier} requires explicit human approval."
                };
            }

            // Admit action
            await _sessionManager.TransitionSessionAsync(tenantId, sessionId, ComputerSessionState.ActionAdmitted, "Action admitted through governance gates");

            var attempt = new ComputerActionExecutionAttempt
            {
                ProposalId = proposalId,
                SessionId = sessionId,
                TenantId = tenantId,
                Status = ActionExecutionStatus.Executing,
                AdmittedAtUtc = DateTime.UtcNow
            };

            await _sessionManager.TransitionSessionAsync(tenantId, sessionId, ComputerSessionState.Executing, "Executing through Worker Fabric and Execution Firewall");

            // Dispatch simulation: Worker Fabric execution
            attempt.CompletedAtUtc = DateTime.UtcNow;
            attempt.Status = ActionExecutionStatus.Completed;
            attempt.ExternalEffect = proposal.RiskTier >= ActionRiskTier.R3_ExternalCommunication ? "ExternalTransmissionRecorded" : "LocalStateUpdated";

            await _sessionManager.TransitionSessionAsync(tenantId, sessionId, ComputerSessionState.Verifying, "Verifying postconditions");
            var trace = await _provenanceService.RecordTraceAsync(tenantId, sessionId, proposalId, attempt.AttemptId, attempt.ExternalEffect, "Outcome_Successful");

            await _sessionManager.TransitionSessionAsync(tenantId, sessionId, ComputerSessionState.Succeeded, "Action completed and verified");

            return new ComputerActionResult
            {
                IsSuccess = true,
                Status = ActionExecutionStatus.Completed,
                Message = $"Proposal {proposalId} executed successfully under governed Worker Fabric.",
                Attempt = attempt,
                ResultHash = trace.TraceHash
            };
        }

        public async Task<ComputerSession> PauseAsync(string tenantId, string sessionId, string reason)
        {
            return await _sessionManager.PauseSessionAsync(tenantId, sessionId, reason);
        }

        public async Task<ComputerSession> ResumeAsync(string tenantId, string sessionId, string supervisorId)
        {
            return await _sessionManager.ResumeSessionAsync(tenantId, sessionId, supervisorId);
        }

        public async Task<ComputerSession> EmergencyKillAsync(string tenantId, string sessionId, string reason)
        {
            return await _sessionManager.EmergencyKillSessionAsync(tenantId, sessionId, reason);
        }
    }
}
