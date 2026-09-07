using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Missions;
using BusinessModelApp.Core.Domain.Runtime;
using BusinessModelApp.Core.Domain.Runtime.Capabilities;
using BusinessModelApp.Core.Domain.Runtime.Workers;
using BusinessModelApp.Core.Interfaces.Runtime.Workers;

namespace BusinessModelApp.Infrastructure.Runtime.Workers
{
    /// <summary>
    /// Master Universal Business Worker Fabric Coordinator.
    /// Manages the full lifecycle of workers: resolution, provisioning, sandboxed execution,
    /// health recording, and deterministic teardown.
    /// Enforces I16 (Modality Isolation), I16-A (Resource Ceilings), I16-B (MCP Tool Isolation),
    /// and I16-C (Zero Direct External Consequentiality).
    /// </summary>
    public class WorkerFabric : IWorkerFabric
    {
        private readonly IWorkerStore _store;
        private readonly IWorkerResolver _resolver;
        private readonly IWorkerSandboxManager _sandboxManager;
        private readonly IWorkerHealthManager _healthManager;
        private readonly Dictionary<WorkerModality, IWorkerModalityAdapter> _adapters;

        public WorkerFabric(
            IWorkerStore store,
            IWorkerResolver resolver,
            IWorkerSandboxManager sandboxManager,
            IWorkerHealthManager healthManager,
            IEnumerable<IWorkerModalityAdapter> adapters)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            _sandboxManager = sandboxManager ?? throw new ArgumentNullException(nameof(sandboxManager));
            _healthManager = healthManager ?? throw new ArgumentNullException(nameof(healthManager));
            _adapters = (adapters ?? throw new ArgumentNullException(nameof(adapters)))
                .ToDictionary(a => a.Modality, a => a);
        }

        public async Task<WorkerResolutionDecision> ResolveWorkerAsync(WorkerResolutionRequest request, CancellationToken ct = default)
        {
            return await _resolver.ResolveWorkerAsync(request, ct);
        }

        public async Task<WorkerInstance> ProvisionWorkerAsync(
            WorkerResolutionDecision decision,
            AgentInstanceId agentInstanceId,
            long fenceToken,
            CancellationToken ct = default)
        {
            if (decision == null) throw new ArgumentNullException(nameof(decision));

            if (!decision.IsAdmissible || !decision.SelectedWorkerDefinitionId.HasValue)
            {
                var failedInstance = new WorkerInstance
                {
                    WorkspaceId = decision.WorkspaceId,
                    AgentInstanceId = agentInstanceId,
                    LeasedCapabilityId = decision.CapabilityId,
                    Modality = decision.SelectedModality,
                    State = WorkerState.ProvisioningFailed,
                    FenceToken = fenceToken,
                    TerminationReason = decision.InadmissibilityReason ?? "Resolution inadmissible"
                };
                await _store.SaveWorkerInstanceAsync(failedInstance, ct);
                return failedInstance;
            }

            var def = await _store.GetWorkerDefinitionAsync(decision.SelectedWorkerDefinitionId.Value, ct);
            var sandboxProfile = def?.DefaultSandboxProfile ?? new WorkerSandboxProfile();
            var instance = new WorkerInstance
            {
                WorkerDefinitionId = decision.SelectedWorkerDefinitionId.Value,
                WorkspaceId = decision.WorkspaceId,
                AgentInstanceId = agentInstanceId,
                LeasedCapabilityId = decision.CapabilityId,
                Modality = decision.SelectedModality,
                State = WorkerState.Provisioning,
                FenceToken = fenceToken,
                LeasedAt = DateTimeOffset.UtcNow,
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10)
            };

            // Provision Sandbox environment
            var sandboxId = await _sandboxManager.CreateSandboxAsync(
                decision.WorkspaceId,
                instance.WorkerInstanceId,
                decision.SelectedModality,
                sandboxProfile,
                ct);

            // Provision OS/Runtime Process wrapper
            var process = new WorkerProcess
            {
                WorkerInstanceId = instance.WorkerInstanceId,
                WorkspaceId = decision.WorkspaceId,
                SandboxId = sandboxId,
                Modality = decision.SelectedModality,
                SpawnedAt = DateTimeOffset.UtcNow
            };
            await _store.SaveWorkerProcessAsync(process, ct);

            instance.ActiveProcessId = process.ProcessId;
            instance.State = WorkerState.Ready;

            await _store.SaveWorkerInstanceAsync(instance, ct);
            return instance;
        }

        public async Task<WorkerAttempt> ExecuteAsync(
            WorkerInstance instance,
            UniversalCapabilityDefinition capability,
            string payloadJson,
            CancellationToken ct = default)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            if (capability == null) throw new ArgumentNullException(nameof(capability));

            var startTime = DateTimeOffset.UtcNow;

            // Fencing & State Verification
            if (instance.FenceToken <= 0)
            {
                instance.State = WorkerState.FenceRejected;
                await _store.SaveWorkerInstanceAsync(instance, ct);
                return new WorkerAttempt
                {
                    WorkerInstanceId = instance.WorkerInstanceId,
                    CapabilityId = capability.CapabilityId,
                    IsSuccess = false,
                    ResultingEffect = NodeExecutionEffect.NoEffect,
                    FailureReason = "Stale or invalid fence token.",
                    CompletedAt = DateTimeOffset.UtcNow
                };
            }

            if (instance.State != WorkerState.Ready && instance.State != WorkerState.Leased)
            {
                return new WorkerAttempt
                {
                    WorkerInstanceId = instance.WorkerInstanceId,
                    CapabilityId = capability.CapabilityId,
                    IsSuccess = false,
                    ResultingEffect = NodeExecutionEffect.NoEffect,
                    FailureReason = $"Worker instance in invalid state '{instance.State}' for execution.",
                    CompletedAt = DateTimeOffset.UtcNow
                };
            }

            if (!_adapters.TryGetValue(instance.Modality, out var adapter))
            {
                instance.State = WorkerState.ProvisioningFailed;
                await _store.SaveWorkerInstanceAsync(instance, ct);
                return new WorkerAttempt
                {
                    WorkerInstanceId = instance.WorkerInstanceId,
                    CapabilityId = capability.CapabilityId,
                    IsSuccess = false,
                    ResultingEffect = NodeExecutionEffect.NoEffect,
                    FailureReason = $"No adapter registered for modality '{instance.Modality}'.",
                    CompletedAt = DateTimeOffset.UtcNow
                };
            }

            instance.State = WorkerState.Running;
            await _store.SaveWorkerInstanceAsync(instance, ct);

            string sandboxId = instance.ActiveProcessId?.ToString() ?? Guid.NewGuid().ToString("N");
            if (instance.ActiveProcessId.HasValue)
            {
                var proc = await _store.GetWorkerProcessAsync(instance.ActiveProcessId.Value, ct);
                if (proc != null) sandboxId = proc.SandboxId;
            }

            // Execute capability via sandboxed modality adapter
            WorkerAttempt attempt;
            try
            {
                attempt = await adapter.ExecuteCapabilityAsync(instance, capability, payloadJson, sandboxId, ct);
            }
            catch (Exception ex)
            {
                attempt = new WorkerAttempt
                {
                    WorkerInstanceId = instance.WorkerInstanceId,
                    CapabilityId = capability.CapabilityId,
                    IsSuccess = false,
                    ResultingEffect = capability.IsConsequential ? NodeExecutionEffect.UnknownEffect : NodeExecutionEffect.NoEffect,
                    FailureReason = $"Adapter execution crash: {ex.Message}",
                    CompletedAt = DateTimeOffset.UtcNow
                };
            }

            var duration = DateTimeOffset.UtcNow - startTime;

            // Health metrology recording
            await _healthManager.RecordExecutionResultAsync(
                instance.WorkerDefinitionId,
                instance.Modality,
                attempt.IsSuccess,
                attempt.FailureReason?.Contains("crash", StringComparison.OrdinalIgnoreCase) == true,
                attempt.FailureReason?.Contains("timeout", StringComparison.OrdinalIgnoreCase) == true,
                attempt.ResultingEffect == NodeExecutionEffect.UnknownEffect,
                duration,
                ct);

            // Update Instance state
            if (attempt.IsSuccess)
            {
                instance.State = WorkerState.Succeeded;
            }
            else if (attempt.ResultingEffect == NodeExecutionEffect.UnknownEffect)
            {
                instance.State = WorkerState.UnknownEffect;
            }
            else if (attempt.FailureReason?.Contains("Violation", StringComparison.OrdinalIgnoreCase) == true)
            {
                instance.State = WorkerState.SandboxViolation;
            }
            else
            {
                instance.State = WorkerState.Completed;
            }

            instance.CurrentAttemptId = attempt.AttemptId;
            await _store.SaveWorkerInstanceAsync(instance, ct);

            return attempt;
        }

        public async Task TeardownWorkerAsync(WorkerInstanceId instanceId, string reason, CancellationToken ct = default)
        {
            var instance = await _store.GetWorkerInstanceAsync(instanceId, ct);
            if (instance == null) return;

            if (instance.ActiveProcessId.HasValue)
            {
                var proc = await _store.GetWorkerProcessAsync(instance.ActiveProcessId.Value, ct);
                if (proc != null)
                {
                    await _sandboxManager.TeardownSandboxAsync(proc.SandboxId, ct);
                    proc.IsRunning = false;
                    proc.TerminatedAt = DateTimeOffset.UtcNow;
                    await _store.SaveWorkerProcessAsync(proc, ct);
                }
            }

            instance.State = WorkerState.Completed;
            instance.TerminationReason = reason;
            await _store.SaveWorkerInstanceAsync(instance, ct);
        }
    }
}
