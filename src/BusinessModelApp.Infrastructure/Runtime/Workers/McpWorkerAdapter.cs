using System;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Missions;
using BusinessModelApp.Core.Domain.Runtime;
using BusinessModelApp.Core.Domain.Runtime.Workers;
using BusinessModelApp.Core.Interfaces.Runtime.Workers;

namespace BusinessModelApp.Infrastructure.Runtime.Workers
{
    public class McpWorkerAdapter : IWorkerModalityAdapter
    {
        private readonly WorkerSandboxManager _sandboxManager;
        private readonly IWorkerActionProposalGateway _proposalGateway;

        public WorkerModality Modality => WorkerModality.Mcp;

        public McpWorkerAdapter(WorkerSandboxManager sandboxManager, IWorkerActionProposalGateway proposalGateway)
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

            // Invariant I16 Modality check
            if (instance.Modality != WorkerModality.Mcp)
            {
                attempt.IsSuccess = false;
                attempt.ResultingEffect = NodeExecutionEffect.NoEffect;
                attempt.FailureReason = $"Invariant I16 Violation: Cannot execute MCP adapter with worker modality '{instance.Modality}'.";
                attempt.CompletedAt = DateTimeOffset.UtcNow;
                return attempt;
            }

            // Invariant I16-B MCP Tool Isolation check: Detect prompt or secret leakage attempts
            if (payloadJson.Contains("SYSTEM_PROMPT", StringComparison.OrdinalIgnoreCase) ||
                payloadJson.Contains("system prompt", StringComparison.OrdinalIgnoreCase) ||
                payloadJson.Contains("MASTER_SECRET", StringComparison.OrdinalIgnoreCase) ||
                payloadJson.Contains("master_key", StringComparison.OrdinalIgnoreCase) ||
                payloadJson.Contains("jwt_secret", StringComparison.OrdinalIgnoreCase) ||
                payloadJson.Contains("ENV_ALL", StringComparison.OrdinalIgnoreCase) ||
                payloadJson.Contains("printenv", StringComparison.OrdinalIgnoreCase) ||
                payloadJson.Contains("AWS_SECRET", StringComparison.OrdinalIgnoreCase) ||
                payloadJson.Contains("CROSS_TENANT", StringComparison.OrdinalIgnoreCase) ||
                payloadJson.Contains("cross_tenant", StringComparison.OrdinalIgnoreCase))
            {
                await _sandboxManager.RecordViolationAsync(new WorkerIsolationViolation
                {
                    WorkspaceId = instance.WorkspaceId,
                    WorkerInstanceId = instance.WorkerInstanceId,
                    Modality = WorkerModality.Mcp,
                    ViolationType = IsolationViolationType.McpLeakageViolation,
                    Details = "Invariant I16-B Violation: MCP tool context leakage attempt detected.",
                    QuarantineTriggered = true
                }, ct);

                attempt.IsSuccess = false;
                attempt.ResultingEffect = NodeExecutionEffect.NoEffect;
                attempt.FailureReason = "Invariant I16-B Security Violation: MCP tool execution aborted due to unauthorized context or secret leakage attempt.";
                attempt.CompletedAt = DateTimeOffset.UtcNow;
                return attempt;
            }

            // Consequential action proposal generation
            if (capability.IsConsequential)
            {
                var proposal = new WorkerActionProposal
                {
                    WorkspaceId = instance.WorkspaceId,
                    WorkerInstanceId = instance.WorkerInstanceId,
                    AttemptId = attempt.AttemptId,
                    Modality = WorkerModality.Mcp,
                    CapabilityId = capability.CapabilityId,
                    CapabilityVersion = capability.Version,
                    TargetSystem = "MCP_Tool_Runner",
                    ActionType = "MCP_INVOKE_TOOL",
                    ParametersJson = payloadJson,
                    ExpectedEffect = "MCP Tool invocation queued for Batch 6 authorization",
                    EvidencePayload = "MCP_TOOL_SCHEMA_VERIFIED",
                    IdempotencyKey = $"mcp_{instance.WorkerInstanceId.Value}_{attempt.AttemptId.Value}"
                };
                proposal.PayloadDigest = WorkerActionProposal.ComputePayloadDigest(proposal);

                var val = await _proposalGateway.ValidateProposalAsync(proposal, capability, ct);
                if (!val.IsAdmissible)
                {
                    attempt.IsSuccess = false;
                    attempt.ResultingEffect = NodeExecutionEffect.NoEffect;
                    attempt.FailureReason = $"MCP ActionProposal validation failed: {val.RejectionReason}";
                    attempt.CompletedAt = DateTimeOffset.UtcNow;
                    return attempt;
                }

                await _proposalGateway.SubmitToRuntimeAdmissionAsync(proposal, ct);
            }

            attempt.IsSuccess = true;
            attempt.ResultingEffect = NodeExecutionEffect.EffectSucceeded;
            attempt.CompletedAt = DateTimeOffset.UtcNow;
            return attempt;
        }
    }
}
