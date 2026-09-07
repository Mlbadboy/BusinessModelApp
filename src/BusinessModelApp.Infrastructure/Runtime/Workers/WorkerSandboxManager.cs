using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Workers;
using BusinessModelApp.Core.Interfaces.Runtime.Workers;

namespace BusinessModelApp.Infrastructure.Runtime.Workers
{
    public class WorkerSandboxManager : IWorkerSandboxManager
    {
        private readonly IWorkerStore _store;
        private readonly ConcurrentDictionary<string, (Guid WorkspaceId, WorkerInstanceId InstanceId, WorkerModality Modality, WorkerSandboxProfile Profile)> _sandboxes = new();

        public WorkerSandboxManager(IWorkerStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public Task<string> CreateSandboxAsync(
            Guid workspaceId,
            WorkerInstanceId instanceId,
            WorkerModality modality,
            WorkerSandboxProfile profile,
            CancellationToken ct = default)
        {
            var sandboxId = $"sbx_{modality.ToString().ToLowerInvariant()}_{Guid.NewGuid():N}";
            _sandboxes[sandboxId] = (workspaceId, instanceId, modality, profile ?? new WorkerSandboxProfile());
            return Task.FromResult(sandboxId);
        }

        public async Task<bool> ValidateResourceLimitsAsync(string sandboxId, WorkerResourceUsage usage, CancellationToken ct = default)
        {
            if (!_sandboxes.TryGetValue(sandboxId, out var ctx))
                return false;

            var profile = ctx.Profile;

            // 1. Memory check
            if (usage.MemoryConsumedMB > profile.MemoryLimitMB)
            {
                await RecordViolationAsync(new WorkerIsolationViolation
                {
                    WorkspaceId = ctx.WorkspaceId,
                    WorkerInstanceId = ctx.InstanceId,
                    Modality = ctx.Modality,
                    ViolationType = IsolationViolationType.MemoryExceeded,
                    Details = $"Memory consumed {usage.MemoryConsumedMB:F1}MB exceeded ceiling {profile.MemoryLimitMB:F1}MB.",
                    QuarantineTriggered = false
                }, ct);
                return false;
            }

            // 2. Timeout check
            if (usage.ElapsedTime > profile.MaxExecutionTimeout)
            {
                await RecordViolationAsync(new WorkerIsolationViolation
                {
                    WorkspaceId = ctx.WorkspaceId,
                    WorkerInstanceId = ctx.InstanceId,
                    Modality = ctx.Modality,
                    ViolationType = IsolationViolationType.TimeoutExceeded,
                    Details = $"Elapsed time {usage.ElapsedTime.TotalSeconds:F1}s exceeded execution timeout {profile.MaxExecutionTimeout.TotalSeconds:F1}s.",
                    QuarantineTriggered = false
                }, ct);
                return false;
            }

            // 3. Token limit check
            if (usage.TokensConsumed > profile.MaxTokenAllowance)
            {
                await RecordViolationAsync(new WorkerIsolationViolation
                {
                    WorkspaceId = ctx.WorkspaceId,
                    WorkerInstanceId = ctx.InstanceId,
                    Modality = ctx.Modality,
                    ViolationType = IsolationViolationType.TokenLimitExceeded,
                    Details = $"Tokens consumed {usage.TokensConsumed} exceeded ceiling {profile.MaxTokenAllowance}.",
                    QuarantineTriggered = false
                }, ct);
                return false;
            }

            // 4. Process spawn check
            if (usage.ChildProcessesSpawned > profile.MaxChildProcesses)
            {
                await RecordViolationAsync(new WorkerIsolationViolation
                {
                    WorkspaceId = ctx.WorkspaceId,
                    WorkerInstanceId = ctx.InstanceId,
                    Modality = ctx.Modality,
                    ViolationType = IsolationViolationType.ProcessSpawnViolation,
                    Details = $"Child processes spawned {usage.ChildProcessesSpawned} exceeded limit {profile.MaxChildProcesses}.",
                    QuarantineTriggered = true
                }, ct);
                return false;
            }

            // 5. CPU check
            if (usage.CpuSecondsUsed > (profile.CpuLimitCores * profile.MaxExecutionTimeout.TotalSeconds))
            {
                await RecordViolationAsync(new WorkerIsolationViolation
                {
                    WorkspaceId = ctx.WorkspaceId,
                    WorkerInstanceId = ctx.InstanceId,
                    Modality = ctx.Modality,
                    ViolationType = IsolationViolationType.TimeoutExceeded,
                    Details = $"CPU seconds consumed {usage.CpuSecondsUsed:F1}s exceeded core execution ceiling.",
                    QuarantineTriggered = false
                }, ct);
                return false;
            }

            // 6. Network bandwidth check
            if (usage.NetworkBandwidthUsedMB > profile.MaxNetworkBandwidthMB)
            {
                await RecordViolationAsync(new WorkerIsolationViolation
                {
                    WorkspaceId = ctx.WorkspaceId,
                    WorkerInstanceId = ctx.InstanceId,
                    Modality = ctx.Modality,
                    ViolationType = IsolationViolationType.NetworkEgressViolation,
                    Details = $"Network bandwidth {usage.NetworkBandwidthUsedMB:F1}MB exceeded limit {profile.MaxNetworkBandwidthMB:F1}MB.",
                    QuarantineTriggered = false
                }, ct);
                return false;
            }

            // 7. Browser pages check
            if (usage.BrowserPagesOpened > profile.MaxBrowserPages)
            {
                await RecordViolationAsync(new WorkerIsolationViolation
                {
                    WorkspaceId = ctx.WorkspaceId,
                    WorkerInstanceId = ctx.InstanceId,
                    Modality = ctx.Modality,
                    ViolationType = IsolationViolationType.ProcessSpawnViolation,
                    Details = $"Browser pages {usage.BrowserPagesOpened} exceeded allowed limit {profile.MaxBrowserPages}.",
                    QuarantineTriggered = false
                }, ct);
                return false;
            }

            return true;
        }

        public async Task RecordViolationAsync(WorkerIsolationViolation violation, CancellationToken ct = default)
        {
            if (violation == null) throw new ArgumentNullException(nameof(violation));
            await _store.RecordIsolationViolationAsync(violation, ct);
        }

        public Task TeardownSandboxAsync(string sandboxId, CancellationToken ct = default)
        {
            _sandboxes.TryRemove(sandboxId, out _);
            return Task.CompletedTask;
        }

        // Helper methods for modality-specific enforcement
        public bool IsNetworkDestinationAllowed(string sandboxId, string destinationUrl)
        {
            if (!_sandboxes.TryGetValue(sandboxId, out var ctx)) return false;
            if (!ctx.Profile.IsNetworkRestricted) return true;

            if (string.IsNullOrWhiteSpace(destinationUrl)) return false;

            return ctx.Profile.AllowedNetworkEndpoints.Any(endpoint =>
                destinationUrl.StartsWith(endpoint, StringComparison.OrdinalIgnoreCase) ||
                endpoint.Equals("*", StringComparison.Ordinal));
        }

        public bool IsFileSystemWriteAllowed(string sandboxId, string targetPath)
        {
            if (!_sandboxes.TryGetValue(sandboxId, out var ctx)) return false;
            if (!ctx.Profile.AllowFileSystemWrite) return false;

            if (string.IsNullOrWhiteSpace(targetPath)) return false;

            return ctx.Profile.AllowedPathPrefixes.Any(p =>
                targetPath.StartsWith(p, StringComparison.OrdinalIgnoreCase));
        }

        public bool IsShellExecutionAllowed(string sandboxId)
        {
            if (!_sandboxes.TryGetValue(sandboxId, out var ctx)) return false;
            return ctx.Profile.AllowShell;
        }

        public Task<System.Collections.Generic.IReadOnlyList<WorkerIsolationViolation>> GetViolationsAsync(Guid workspaceId, CancellationToken ct = default)
        {
            return _store.GetIsolationViolationsAsync(workspaceId, ct);
        }
    }
}
