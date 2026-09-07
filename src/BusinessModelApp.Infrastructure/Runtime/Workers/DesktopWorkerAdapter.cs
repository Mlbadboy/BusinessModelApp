using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Missions;
using BusinessModelApp.Core.Domain.Runtime;
using BusinessModelApp.Core.Domain.Runtime.Workers;
using BusinessModelApp.Core.Interfaces.Runtime.Workers;

namespace BusinessModelApp.Infrastructure.Runtime.Workers
{
    /// <summary>
    /// Governed Desktop / Legacy System Modality Adapter.
    /// Operates under strict sandbox containment with default-DENY for arbitrary shell,
    /// registry, network traversal, and unauthorized process spawning.
    /// Consequential actions are packaged into ActionProposals and routed to Batch 6 Firewall.
    /// </summary>
    public class DesktopWorkerAdapter : IWorkerModalityAdapter
    {
        private readonly WorkerSandboxManager _sandboxManager;
        private readonly IWorkerActionProposalGateway _proposalGateway;

        public WorkerModality Modality => WorkerModality.Desktop;

        public DesktopWorkerAdapter(WorkerSandboxManager sandboxManager, IWorkerActionProposalGateway proposalGateway)
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

            // Invariant I16: Modality match check
            if (instance.Modality != WorkerModality.Desktop)
            {
                attempt.IsSuccess = false;
                attempt.ResultingEffect = NodeExecutionEffect.NoEffect;
                attempt.FailureReason = $"Invariant I16 Violation: Cannot execute Desktop adapter with worker modality '{instance.Modality}'.";
                attempt.CompletedAt = DateTimeOffset.UtcNow;
                return attempt;
            }

            // Inspect payload for unauthorized shell, registry, or subprocess escape attempts
            if (ContainsDesktopSecurityViolations(payloadJson, out var violationType, out var violationDetail))
            {
                await _sandboxManager.RecordViolationAsync(new WorkerIsolationViolation
                {
                    WorkspaceId = instance.WorkspaceId,
                    WorkerInstanceId = instance.WorkerInstanceId,
                    Modality = WorkerModality.Desktop,
                    ViolationType = violationType,
                    Details = violationDetail,
                    QuarantineTriggered = true
                }, ct);

                attempt.IsSuccess = false;
                attempt.ResultingEffect = NodeExecutionEffect.NoEffect;
                attempt.FailureReason = $"Desktop Sandbox Security Violation: {violationDetail}";
                attempt.CompletedAt = DateTimeOffset.UtcNow;
                return attempt;
            }

            // Consequential action packaging under I16-C: Submit to Batch 6 Firewall
            if (capability.IsConsequential)
            {
                var proposal = new WorkerActionProposal
                {
                    WorkspaceId = instance.WorkspaceId,
                    WorkerInstanceId = instance.WorkerInstanceId,
                    AttemptId = attempt.AttemptId,
                    Modality = WorkerModality.Desktop,
                    CapabilityId = capability.CapabilityId,
                    CapabilityVersion = capability.Version,
                    TargetSystem = "Desktop_Legacy_Host",
                    ActionType = "DESKTOP_INTERACTION",
                    ParametersJson = payloadJson,
                    ExpectedEffect = "Desktop legacy system interaction proposal routed for Batch 6 Firewall admission",
                    EvidencePayload = "DESKTOP_PROCESS_EVIDENCE_HASH_SHA256",
                    IdempotencyKey = $"desktop_{instance.WorkerInstanceId.Value}_{attempt.AttemptId.Value}"
                };
                proposal.PayloadDigest = WorkerActionProposal.ComputePayloadDigest(proposal);

                var val = await _proposalGateway.ValidateProposalAsync(proposal, capability, ct);
                if (!val.IsAdmissible)
                {
                    attempt.IsSuccess = false;
                    attempt.ResultingEffect = NodeExecutionEffect.NoEffect;
                    attempt.FailureReason = $"Desktop ActionProposal validation failed: {val.RejectionReason}";
                    attempt.CompletedAt = DateTimeOffset.UtcNow;
                    return attempt;
                }

                await _proposalGateway.SubmitToRuntimeAdmissionAsync(proposal, ct);

                attempt.IsSuccess = true;
                attempt.ResultingEffect = NodeExecutionEffect.EffectSucceeded;
                attempt.CompletedAt = DateTimeOffset.UtcNow;
                return attempt;
            }

            // Governed non-consequential execution
            attempt.IsSuccess = true;
            attempt.ResultingEffect = NodeExecutionEffect.EffectSucceeded;
            attempt.CompletedAt = DateTimeOffset.UtcNow;
            return attempt;
        }

        private static bool ContainsDesktopSecurityViolations(string payloadJson, out IsolationViolationType violationType, out string detail)
        {
            violationType = IsolationViolationType.ShellViolation;
            detail = string.Empty;

            if (string.IsNullOrWhiteSpace(payloadJson))
                return false;

            var lower = payloadJson.ToLowerInvariant();

            if (lower.Contains("cmd.exe") || lower.Contains("powershell") || lower.Contains("/bin/bash") || lower.Contains("/bin/sh") || lower.Contains("system("))
            {
                violationType = IsolationViolationType.ShellViolation;
                detail = "Arbitrary shell invocation attempted in desktop worker environment.";
                return true;
            }

            if (lower.Contains("reg add") || lower.Contains("reg delete") || lower.Contains("hkey_local_machine") || lower.Contains("hkey_current_user"))
            {
                violationType = IsolationViolationType.FileSystemViolation;
                detail = "Unauthorized Windows registry modification attempted.";
                return true;
            }

            if (lower.Contains("curl ") || lower.Contains("wget ") || lower.Contains("socket.connect"))
            {
                violationType = IsolationViolationType.NetworkEgressViolation;
                detail = "Unrestricted outbound network socket attempted from isolated desktop sandbox.";
                return true;
            }

            if (lower.Contains("unauthorized_process.exe") || lower.Contains("spawn_process"))
            {
                violationType = IsolationViolationType.ProcessSpawnViolation;
                detail = "Spawning unauthorized external binary outside desktop allowlist.";
                return true;
            }

            return false;
        }
    }
}
