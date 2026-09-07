using System;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Workers;
using BusinessModelApp.Core.Interfaces.Runtime.Workers;

namespace BusinessModelApp.Infrastructure.Runtime.Workers
{
    public class WorkerActionProposalGateway : IWorkerActionProposalGateway
    {
        private readonly IWorkerStore _store;

        public WorkerActionProposalGateway(IWorkerStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public async Task<ActionProposalValidationResult> ValidateProposalAsync(
            WorkerActionProposal proposal,
            UniversalCapabilityDefinition capability,
            CancellationToken ct = default)
        {
            if (proposal == null) throw new ArgumentNullException(nameof(proposal));
            if (capability == null) throw new ArgumentNullException(nameof(capability));

            // 1. Check capability match
            if (proposal.CapabilityId != capability.CapabilityId)
            {
                return ActionProposalValidationResult.Reject(
                    $"Proposal capability '{proposal.CapabilityId}' does not match registered capability '{capability.CapabilityId}'.");
            }

            // 2. Check idempotency key
            if (string.IsNullOrWhiteSpace(proposal.IdempotencyKey))
            {
                return ActionProposalValidationResult.Reject("ActionProposal requires a valid IdempotencyKey.");
            }

            var existing = await _store.GetActionProposalsAsync(proposal.WorkspaceId, ct);
            if (existing.Any(p => p.IdempotencyKey == proposal.IdempotencyKey && p.ProposalId != proposal.ProposalId))
            {
                return ActionProposalValidationResult.Reject($"Duplicate idempotency key '{proposal.IdempotencyKey}' detected in workspace.");
            }

            // 3. Verify Payload Digest (anti-tamper check)
            var computedDigest = WorkerActionProposal.ComputePayloadDigest(proposal);
            if (!string.Equals(proposal.PayloadDigest, computedDigest, StringComparison.OrdinalIgnoreCase))
            {
                return ActionProposalValidationResult.Reject(
                    "PayloadDigest mismatch: ActionProposal payload has been modified or tampered in transit.");
            }

            return ActionProposalValidationResult.Success(capability.IsConsequential);
        }

        public async Task<bool> SubmitToRuntimeAdmissionAsync(
            WorkerActionProposal proposal,
            CancellationToken ct = default)
        {
            if (proposal == null) throw new ArgumentNullException(nameof(proposal));

            // Save immutable proposal audit record
            await _store.SaveActionProposalAsync(proposal, ct);

            // Successfully queued for Runtime Admission and Batch 6 Execution Firewall
            return true;
        }
    }
}
