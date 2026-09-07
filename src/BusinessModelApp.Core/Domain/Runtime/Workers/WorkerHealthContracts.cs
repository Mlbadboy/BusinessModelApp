using System;

namespace BusinessModelApp.Core.Domain.Runtime.Workers
{
    public enum WorkerCircuitState
    {
        Healthy,
        Degraded,
        CircuitOpen,
        Quarantined,
        Recovering
    }

    public class WorkerHealthMetrics
    {
        public int TotalExecutions { get; set; }
        public int SuccessCount { get; set; }
        public int CrashCount { get; set; }
        public int TimeoutCount { get; set; }
        public int UnknownEffectCount { get; set; }
        public int SandboxViolations { get; set; }
        public double AverageExecutionTimeMs { get; set; }

        public double FailureRate => TotalExecutions > 0 ? (double)(TotalExecutions - SuccessCount) / TotalExecutions : 0.0;
        public double UnknownEffectRate => TotalExecutions > 0 ? (double)UnknownEffectCount / TotalExecutions : 0.0;
    }

    public class WorkerHealthSnapshot
    {
        public WorkerDefinitionId WorkerDefinitionId { get; init; }
        public WorkerModality Modality { get; init; }
        public WorkerCircuitState CircuitState { get; set; } = WorkerCircuitState.Healthy;
        public WorkerHealthMetrics Metrics { get; set; } = new();
        public string? QuarantineReason { get; set; }
        public DateTimeOffset LastStateChangedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
