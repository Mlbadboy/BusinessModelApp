using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Organizational;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational
{
    public class OrganizationalWorkOrchestrator
    {
        private readonly IOrganizationalWorkRepository _repository;
        private readonly IOrganizationalWorkAdmissionEngine _admissionEngine;
        private readonly IOrganizationalStateMachine _stateMachine;
        private readonly IWorkDependencyResolver _dependencyResolver;
        private readonly IWorkCommitmentMonitor _commitmentMonitor;
        private readonly IWorkOutcomeVerifier _outcomeVerifier;

        public OrganizationalWorkOrchestrator(
            IOrganizationalWorkRepository repository,
            IOrganizationalWorkAdmissionEngine admissionEngine,
            IOrganizationalStateMachine stateMachine,
            IWorkDependencyResolver dependencyResolver,
            IWorkCommitmentMonitor commitmentMonitor,
            IWorkOutcomeVerifier outcomeVerifier)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _admissionEngine = admissionEngine ?? throw new ArgumentNullException(nameof(admissionEngine));
            _stateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
            _dependencyResolver = dependencyResolver ?? throw new ArgumentNullException(nameof(dependencyResolver));
            _commitmentMonitor = commitmentMonitor ?? throw new ArgumentNullException(nameof(commitmentMonitor));
            _outcomeVerifier = outcomeVerifier ?? throw new ArgumentNullException(nameof(outcomeVerifier));
        }

        public async Task<WorkProposal> SubmitProposalAsync(WorkProposal proposal, CancellationToken ct = default)
        {
            if (proposal == null) throw new ArgumentNullException(nameof(proposal));
            if (string.IsNullOrWhiteSpace(proposal.ProposalId))
            {
                proposal.ProposalId = $"PROP-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
            }
            proposal.ComputeProvenanceHash();
            await _repository.SaveProposalAsync(proposal, ct);
            return proposal;
        }

        public async Task<(bool Admitted, string? RejectionReason, WorkItem? Item)> AdmitProposalAsync(
            string tenantId,
            string proposalId,
            CancellationToken ct = default)
        {
            var proposal = await _repository.GetProposalAsync(tenantId, proposalId, ct);
            if (proposal == null)
            {
                return (false, $"Proposal '{proposalId}' not found for tenant '{tenantId}'.", null);
            }

            return await _admissionEngine.EvaluateAndAdmitAsync(proposal, ct);
        }

        public async Task<(bool Success, string? ErrorMessage, WorkItem? Item)> TransitionWorkStateAsync(
            string tenantId,
            string workId,
            WorkState targetState,
            string? governanceApprovalActor = null,
            string? verificationEvidenceHash = null,
            CancellationToken ct = default)
        {
            var item = await _repository.GetWorkItemAsync(tenantId, workId, ct);
            if (item == null)
            {
                return (false, $"WorkItem '{workId}' not found for tenant '{tenantId}'.", null);
            }

            // If transitioning to Preparing or beyond, check if blocked by dependencies
            if (targetState == WorkState.Preparing || targetState == WorkState.WaitingForGovernance || targetState == WorkState.ExecutionAdmitted)
            {
                var isBlocked = await _dependencyResolver.IsWorkBlockedAsync(tenantId, workId, ct);
                if (isBlocked)
                {
                    // Transition to Blocked instead
                    var blockResult = _stateMachine.ValidateAndTransition(item, WorkState.Blocked);
                    if (blockResult.Success)
                    {
                        await _repository.SaveWorkItemAsync(item, ct);
                    }
                    return (false, $"WorkItem '{workId}' is blocked by unmet HardBlock prerequisites.", item);
                }
            }

            var result = _stateMachine.ValidateAndTransition(item, targetState, governanceApprovalActor, verificationEvidenceHash);
            if (!result.Success)
            {
                return (false, result.ErrorMessage, item);
            }

            await _repository.SaveWorkItemAsync(item, ct);
            return (true, null, item);
        }

        public async Task<(bool Success, string? ErrorMessage)> AddDependencyAsync(
            string tenantId,
            WorkDependency dependency,
            CancellationToken ct = default)
        {
            if (dependency == null) throw new ArgumentNullException(nameof(dependency));

            if (string.IsNullOrWhiteSpace(dependency.DependencyId))
            {
                dependency.DependencyId = $"DEP-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
            }

            // Check circular dependency
            if (_dependencyResolver.HasCircularDependency(tenantId, dependency.DependentWorkId, dependency.RequiredWorkId))
            {
                return (false, $"Circular dependency detected between '{dependency.DependentWorkId}' and '{dependency.RequiredWorkId}'.");
            }

            await _repository.SaveDependencyAsync(tenantId, dependency, ct);

            // Update dependent work item dependency IDs
            var item = await _repository.GetWorkItemAsync(tenantId, dependency.DependentWorkId, ct);
            if (item != null)
            {
                if (!item.DependencyIds.Contains(dependency.DependencyId))
                {
                    item.DependencyIds.Add(dependency.DependencyId);
                    await _repository.SaveWorkItemAsync(item, ct);
                }
            }

            return (true, null);
        }

        public async Task<WorkOutcome> RecordOutcomeAsync(
            string tenantId,
            string workId,
            string claimedSummary,
            Dictionary<string, double> expectedMetrics,
            Dictionary<string, double> actualMetrics,
            string evidenceSha256,
            string verifierActor,
            CancellationToken ct = default)
        {
            var item = await _repository.GetWorkItemAsync(tenantId, workId, ct);
            if (item == null) throw new KeyNotFoundException($"WorkItem '{workId}' not found.");

            var verifyResult = _outcomeVerifier.VerifyOutcome(
                item,
                claimedSummary,
                expectedMetrics,
                actualMetrics,
                evidenceSha256,
                verifierActor);

            if (!verifyResult.Verified)
            {
                throw new InvalidOperationException($"Outcome verification failed: {verifyResult.ErrorMessage}");
            }

            var outcome = new WorkOutcome
            {
                OutcomeId = $"OUT-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
                WorkId = workId,
                ClaimedOutcomeSummary = claimedSummary,
                VerifiedOutcomeSummary = $"Outcome verified by {verifierActor} with confidence score {verifyResult.CalculatedSuccessScore:P1}.",
                ActualMetrics = actualMetrics ?? new Dictionary<string, double>(),
                SuccessScore = verifyResult.CalculatedSuccessScore,
                IsVerified = true,
                VerificationEvidenceHash = verifyResult.VerificationHash,
                VerifiedBy = verifierActor,
                RecordedUtc = DateTime.UtcNow
            };

            item.OutcomeRecord = outcome;

            // Transition from Verifying to Completed
            if (item.State == WorkState.Verifying)
            {
                _stateMachine.ValidateAndTransition(item, WorkState.Completed, null, verifyResult.VerificationHash);
            }

            await _repository.SaveWorkItemAsync(item, ct);

            // Update Lineage
            var lineage = await _repository.GetLineageRecordAsync(tenantId, workId, ct);
            if (lineage != null)
            {
                lineage.OutcomeId = outcome.OutcomeId;
                lineage.ComputeChainHash();
                await _repository.SaveLineageRecordAsync(tenantId, lineage, ct);
            }

            return outcome;
        }

        public async Task<WorkMissionLineageRecord?> GetLineageAsync(string tenantId, string workId, CancellationToken ct = default)
        {
            return await _repository.GetLineageRecordAsync(tenantId, workId, ct);
        }

        public async Task<IReadOnlyList<WorkCommitment>> EvaluateCommitmentsAsync(string tenantId, CancellationToken ct = default)
        {
            return await _commitmentMonitor.EvaluateCommitmentsAsync(tenantId, ct);
        }
    }
}
