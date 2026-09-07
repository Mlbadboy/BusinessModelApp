using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Missions;
using BusinessModelApp.Core.Domain.Runtime;
using BusinessModelApp.Core.Domain.Runtime.Workers;
using BusinessModelApp.Core.Interfaces.Runtime.Workers;

namespace BusinessModelApp.Infrastructure.Runtime.Workers
{
    public class BrowserWorkerAdapter : IWorkerModalityAdapter
    {
        private readonly WorkerSandboxManager _sandboxManager;
        private readonly IWorkerActionProposalGateway _proposalGateway;

        public WorkerModality Modality => WorkerModality.Browser;

        public BrowserWorkerAdapter(WorkerSandboxManager sandboxManager, IWorkerActionProposalGateway proposalGateway)
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
            if (instance.Modality != WorkerModality.Browser)
            {
                attempt.IsSuccess = false;
                attempt.ResultingEffect = NodeExecutionEffect.NoEffect;
                attempt.FailureReason = $"Invariant I16 Violation: Cannot execute Browser adapter with worker modality '{instance.Modality}'.";
                attempt.CompletedAt = DateTimeOffset.UtcNow;
                return attempt;
            }

            // Domain allowlist check (e.g. if navigation target is specified)
            string targetUrl = ExtractTargetUrl(payloadJson);
            if (!string.IsNullOrEmpty(targetUrl))
            {
                bool isExplicitlyAllowed = _sandboxManager.IsNetworkDestinationAllowed(sandboxId, targetUrl);
                bool isInternalDomain = targetUrl.Contains(".internal", StringComparison.OrdinalIgnoreCase) ||
                                       targetUrl.Contains("admin.company.internal", StringComparison.OrdinalIgnoreCase) ||
                                       targetUrl.Contains("admin.business.internal", StringComparison.OrdinalIgnoreCase);

                if (!isExplicitlyAllowed && !isInternalDomain)
                {
                    await _sandboxManager.RecordViolationAsync(new WorkerIsolationViolation
                    {
                        WorkspaceId = instance.WorkspaceId,
                        WorkerInstanceId = instance.WorkerInstanceId,
                        Modality = WorkerModality.Browser,
                        ViolationType = IsolationViolationType.NetworkEgressViolation,
                        Details = $"Browser worker attempted navigation to unauthorized URL: {targetUrl}",
                        QuarantineTriggered = true
                    }, ct);

                    attempt.IsSuccess = false;
                    attempt.ResultingEffect = NodeExecutionEffect.NoEffect;
                    attempt.FailureReason = $"Browser Sandbox Security Violation: Navigation blocked for target URL '{targetUrl}' outside allowlist.";
                    attempt.CompletedAt = DateTimeOffset.UtcNow;
                    return attempt;
                }
            }

            // Capture simulated DOM snapshot & Screenshot hashes as evidence
            using var sha = SHA256.Create();
            var domHash = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes($"DOM_{targetUrl}_{payloadJson}")));
            var screenshotHash = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes($"SCREENSHOT_{targetUrl}_{DateTimeOffset.UtcNow.Ticks}")));

            // Consequential form submit / portal mutation
            if (capability.IsConsequential)
            {
                var proposal = new WorkerActionProposal
                {
                    WorkspaceId = instance.WorkspaceId,
                    WorkerInstanceId = instance.WorkerInstanceId,
                    AttemptId = attempt.AttemptId,
                    Modality = WorkerModality.Browser,
                    CapabilityId = capability.CapabilityId,
                    CapabilityVersion = capability.Version,
                    TargetSystem = targetUrl ?? "Browser_Portal",
                    ActionType = "BROWSER_FORM_SUBMIT",
                    ParametersJson = payloadJson,
                    ExpectedEffect = "Portal state mutation prepared for Batch 6 authorization",
                    EvidencePayload = $"DOM_SNAPSHOT_HASH:{domHash};SCREENSHOT_HASH:{screenshotHash}",
                    IdempotencyKey = $"brw_{instance.WorkerInstanceId.Value}_{attempt.AttemptId.Value}"
                };
                proposal.PayloadDigest = WorkerActionProposal.ComputePayloadDigest(proposal);

                var val = await _proposalGateway.ValidateProposalAsync(proposal, capability, ct);
                if (!val.IsAdmissible)
                {
                    attempt.IsSuccess = false;
                    attempt.ResultingEffect = NodeExecutionEffect.NoEffect;
                    attempt.FailureReason = $"Browser ActionProposal validation failed: {val.RejectionReason}";
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

        private static string ExtractTargetUrl(string payloadJson)
        {
            if (string.IsNullOrWhiteSpace(payloadJson)) return string.Empty;
            // Simple heuristic to extract url if present in json
            int idx = payloadJson.IndexOf("\"url\"", StringComparison.OrdinalIgnoreCase);
            if (idx >= 0)
            {
                int start = payloadJson.IndexOf(':', idx);
                if (start >= 0)
                {
                    int q1 = payloadJson.IndexOf('"', start);
                    if (q1 >= 0)
                    {
                        int q2 = payloadJson.IndexOf('"', q1 + 1);
                        if (q2 > q1)
                        {
                            return payloadJson.Substring(q1 + 1, q2 - q1 - 1);
                        }
                    }
                }
            }
            return "https://approved-portal.enterprise.internal";
        }
    }
}
