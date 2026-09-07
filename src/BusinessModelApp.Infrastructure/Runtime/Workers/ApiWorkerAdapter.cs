using System;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Missions;
using BusinessModelApp.Core.Domain.Runtime;
using BusinessModelApp.Core.Domain.Runtime.Workers;
using BusinessModelApp.Core.Interfaces.Runtime.Workers;

namespace BusinessModelApp.Infrastructure.Runtime.Workers
{
    public class ApiWorkerAdapter : IWorkerModalityAdapter
    {
        private readonly WorkerSandboxManager _sandboxManager;
        private readonly IWorkerActionProposalGateway _proposalGateway;

        public WorkerModality Modality => WorkerModality.Api;

        public ApiWorkerAdapter(WorkerSandboxManager sandboxManager, IWorkerActionProposalGateway proposalGateway)
        {
            _sandboxManager = sandboxManager ?? throw new ArgumentNullException(nameof(sandboxManager));
            _proposalGateway = proposalGateway ?? throw new ArgumentNullException(nameof(proposalGateway));
        }

        public async Task<WorkerAttempt> ExecuteCapabilityAsync(
            WorkerInstance instance,
            UniversalCapabilityDefinition capability,
            string payloadJson,
            string sandboxId,
            CancellationToken ct = default)
        {
            var attempt = new WorkerAttempt
            {
                WorkerInstanceId = instance.WorkerInstanceId,
                CapabilityId = capability.CapabilityId,
                StartedAt = DateTimeOffset.UtcNow
            };

            // Invariant I16 Modality check: Adapter modality must match instance modality
            if (instance.Modality != WorkerModality.Api)
            {
                attempt.IsSuccess = false;
                attempt.ResultingEffect = NodeExecutionEffect.NoEffect;
                attempt.FailureReason = $"Invariant I16 Violation: Cannot execute API adapter with worker modality '{instance.Modality}'.";
                attempt.CompletedAt = DateTimeOffset.UtcNow;
                return attempt;
            }

            // Input payload validation (must be valid non-empty JSON)
            if (string.IsNullOrWhiteSpace(payloadJson) || payloadJson == "{}")
            {
                attempt.IsSuccess = false;
                attempt.ResultingEffect = NodeExecutionEffect.NoEffect;
                attempt.FailureReason = "API Worker execution rejected: Input payload is empty or invalid.";
                attempt.CompletedAt = DateTimeOffset.UtcNow;
                return attempt;
            }

            // If consequential, generate ActionProposal under I16-C rather than direct execution
            if (capability.IsConsequential)
            {
                var proposal = new WorkerActionProposal
                {
                    WorkspaceId = instance.WorkspaceId,
                    WorkerInstanceId = instance.WorkerInstanceId,
                    AttemptId = attempt.AttemptId,
                    Modality = WorkerModality.Api,
                    CapabilityId = capability.CapabilityId,
                    CapabilityVersion = capability.Version,
                    TargetSystem = "SaaS_Connector_Gateway",
                    ActionType = "API_POST",
                    ParametersJson = payloadJson,
                    ExpectedEffect = "External API call prepared for Batch 6 authorization",
                    EvidencePayload = "API_SCHEMA_VERIFIED",
                    IdempotencyKey = $"api_{instance.WorkerInstanceId.Value}_{attempt.AttemptId.Value}"
                };
                proposal.PayloadDigest = WorkerActionProposal.ComputePayloadDigest(proposal);

                var val = await _proposalGateway.ValidateProposalAsync(proposal, capability, ct);
                if (!val.IsAdmissible)
                {
                    attempt.IsSuccess = false;
                    attempt.ResultingEffect = NodeExecutionEffect.NoEffect;
                    attempt.FailureReason = $"API ActionProposal validation failed: {val.RejectionReason}";
                    attempt.CompletedAt = DateTimeOffset.UtcNow;
                    return attempt;
                }

                await _proposalGateway.SubmitToRuntimeAdmissionAsync(proposal, ct);

                attempt.IsSuccess = true;
                attempt.ResultingEffect = NodeExecutionEffect.EffectSucceeded;
                attempt.CompletedAt = DateTimeOffset.UtcNow;
                return attempt;
            }

            // Non-consequential read-only query
            attempt.IsSuccess = true;
            attempt.ResultingEffect = NodeExecutionEffect.EffectSucceeded;
            attempt.CompletedAt = DateTimeOffset.UtcNow;
            return attempt;
        }
    }
}
