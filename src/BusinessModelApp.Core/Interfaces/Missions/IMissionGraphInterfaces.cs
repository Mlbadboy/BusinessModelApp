using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Missions;
using BusinessModelApp.Core.Domain.Responsibilities;
using BusinessModelApp.Core.Domain.Runtime;

namespace BusinessModelApp.Core.Interfaces.Missions
{
    public record TenantMissionPolicyContext
    {
        public Guid WorkspaceId { get; init; }
        public AutonomyTier MaxAllowedAutonomyTier { get; init; } = AutonomyTier.L2_Simulate;
        public int MaxNodesPerGraph { get; init; } = 100;
        public int MaxGraphDepth { get; init; } = 25;
        public int MaxBranches { get; init; } = 10;
        public int MaxExpansionCount { get; init; } = 20;
        public int MaxNodesPerExpansion { get; init; } = 20;
        public TimeSpan MaxMissionLifetime { get; init; } = TimeSpan.FromHours(24);
        public int MaxConcurrentNodes { get; init; } = 5;
        public long MaxTotalBudgetTokens { get; init; } = 1_000_000;
        public decimal MaxTotalCostUsd { get; init; } = 50.00m;
        public IReadOnlySet<string> RegisteredCapabilityIds { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public IReadOnlySet<MissionNodeType> DisallowedNodeTypes { get; init; } = new HashSet<MissionNodeType>();
        public bool AllowDynamicExpansion { get; init; } = true;
        public bool IsEmergencyKillActive { get; init; } = false;
    }

    public record GraphValidationResult
    {
        public bool IsValid { get; init; }
        public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
        public AutonomyTier ClampedAutonomyTier { get; init; }
        public bool CycleDetected { get; init; }
        public IReadOnlyList<string> CyclePath { get; init; } = Array.Empty<string>();

        public static GraphValidationResult Success(AutonomyTier clampedTier, IReadOnlyList<string>? warnings = null) =>
            new()
            {
                IsValid = true,
                ClampedAutonomyTier = clampedTier,
                Warnings = warnings ?? Array.Empty<string>()
            };

        public static GraphValidationResult Failed(IReadOnlyList<string> errors, bool cycleDetected = false, IReadOnlyList<string>? cyclePath = null) =>
            new()
            {
                IsValid = false,
                Errors = errors,
                CycleDetected = cycleDetected,
                CyclePath = cyclePath ?? Array.Empty<string>()
            };
    }

    public record CycleDetectionResult
    {
        public bool HasCycle { get; init; }
        public IReadOnlyList<string> CyclePath { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> TopologicalOrder { get; init; } = Array.Empty<string>();

        public static CycleDetectionResult Acyclic(IReadOnlyList<string> topoOrder) =>
            new() { HasCycle = false, TopologicalOrder = topoOrder };

        public static CycleDetectionResult Cyclic(IReadOnlyList<string> cyclePath) =>
            new() { HasCycle = true, CyclePath = cyclePath };
    }

    public interface IMissionPredicateEvaluator
    {
        bool EvaluatePredicate(TypedPredicate predicate, IReadOnlyDictionary<string, object>? context);
    }

    public interface ICycleDetector
    {
        CycleDetectionResult DetectCycles(IEnumerable<string> nodeIds, IEnumerable<(string Source, string Target)> edges);
    }

    public interface IGraphValidator
    {
        Task<GraphValidationResult> ValidateProposalAsync(MissionGraphProposal proposal, TenantMissionPolicyContext tenantPolicy, CancellationToken ct = default);
        Task<GraphValidationResult> ValidateExpansionAsync(MissionGraph currentGraph, DynamicExpansionProposal expansion, TenantMissionPolicyContext tenantPolicy, CancellationToken ct = default);
    }

    public interface IDagCompiler
    {
        Task<MissionGraph> CompileAsync(MissionGraphProposal proposal, TenantMissionPolicyContext tenantPolicy, CancellationToken ct = default);
    }

    public interface IGraphExpansionEngine
    {
        Task<MissionGraph> ApplyExpansionAsync(MissionGraph currentGraph, DynamicExpansionProposal expansion, TenantMissionPolicyContext tenantPolicy, CancellationToken ct = default);
    }

    public interface INodeVerificationEngine
    {
        Task<NodeVerificationResult> VerifyNodeOutcomeAsync(MissionNodeRecord node, object? outputPayload, IReadOnlyList<MissionArtifact>? artifacts = null, CancellationToken ct = default);
    }

    public interface INodeAdmissionGate
    {
        Task<bool> CanAdmitNodeAsync(MissionGraph graph, MissionNodeRecord node, TenantMissionPolicyContext tenantPolicy, CancellationToken ct = default);
    }

    public interface IEffectReconciliationEngine
    {
        Task<NodeExecutionEffect> ReconcileEffectAsync(MissionGraphId graphId, MissionNodeId nodeId, CancellationToken ct = default);
    }

    public interface IMissionGraphStore
    {
        Task SaveGraphAsync(MissionGraph graph, CancellationToken ct = default);
        Task<MissionGraph?> GetGraphAsync(MissionGraphId graphId, MissionGraphVersion? version = null, CancellationToken ct = default);
        Task<IReadOnlyList<MissionGraph>> GetGraphHistoryAsync(MissionGraphId graphId, CancellationToken ct = default);
        Task SaveMissionAsync(MissionRecord mission, CancellationToken ct = default);
        Task<MissionRecord?> GetMissionAsync(MissionId missionId, CancellationToken ct = default);
    }

    public record MissionGraphAuditEntry
    {
        public Guid EntryId { get; init; } = Guid.NewGuid();
        public MissionGraphId GraphId { get; init; }
        public MissionGraphVersion Version { get; init; }
        public string EventType { get; init; } = string.Empty;
        public string Details { get; init; } = string.Empty;
        public string Sha256Hash { get; init; } = string.Empty;
        public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    }

    public interface IMissionGraphAuditLedger
    {
        Task RecordEventAsync(MissionGraphAuditEntry entry, CancellationToken ct = default);
        Task<IReadOnlyList<MissionGraphAuditEntry>> GetEntriesAsync(MissionGraphId graphId, CancellationToken ct = default);
    }
}
