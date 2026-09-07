using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.Domain.Runtime.Workers
{
    public enum IsolationViolationType
    {
        ModalityEscape,
        MemoryExceeded,
        TimeoutExceeded,
        TokenLimitExceeded,
        NetworkEgressViolation,
        FileSystemViolation,
        ProcessSpawnViolation,
        ShellViolation,
        McpLeakageViolation,
        CredentialScopeViolation
    }

    /// <summary>
    /// Sandbox Profile enforcing immutable resource and security boundaries (Invariant I16-A).
    /// </summary>
    public class WorkerSandboxProfile
    {
        public double MemoryLimitMB { get; init; } = 1024.0;
        public double CpuLimitCores { get; init; } = 2.0;
        public TimeSpan MaxExecutionTimeout { get; init; } = TimeSpan.FromMinutes(2);
        public int MaxTokenAllowance { get; init; } = 25000;
        public double MaxNetworkBandwidthMB { get; init; } = 50.0;
        public int MaxChildProcesses { get; init; } = 0; // Default DENY process spawning
        public int MaxBrowserPages { get; init; } = 5;
        public bool AllowShell { get; init; } = false; // Default DENY shell execution
        public bool AllowFileSystemWrite { get; init; } = false;
        public List<string> AllowedPathPrefixes { get; init; } = new();
        public List<string> AllowedNetworkEndpoints { get; init; } = new();
        public bool IsNetworkRestricted { get; init; } = true;
    }

    /// <summary>
    /// Realtime resource consumption tracking.
    /// </summary>
    public class WorkerResourceUsage
    {
        public double MemoryConsumedMB { get; set; }
        public double CpuSecondsUsed { get; set; }
        public TimeSpan ElapsedTime { get; set; }
        public int TokensConsumed { get; set; }
        public double NetworkBandwidthUsedMB { get; set; }
        public int ChildProcessesSpawned { get; set; }
        public int BrowserPagesOpened { get; set; }
    }

    /// <summary>
    /// Security audit record generated on any sandbox or isolation breach attempt.
    /// </summary>
    public class WorkerIsolationViolation
    {
        public Guid ViolationId { get; init; } = Guid.NewGuid();
        public Guid WorkspaceId { get; init; }
        public WorkerInstanceId WorkerInstanceId { get; init; }
        public WorkerModality Modality { get; init; }
        public IsolationViolationType ViolationType { get; init; }
        public string Details { get; init; } = string.Empty;
        public bool QuarantineTriggered { get; init; }
        public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
    }
}
