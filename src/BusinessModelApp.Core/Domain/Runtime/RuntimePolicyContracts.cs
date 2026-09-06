using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.Domain.Runtime
{
    public record PolicySnapshotContext
    {
        public string PolicySnapshotId { get; init; } = $"POL-{Guid.NewGuid():N}";
        public string AuthoritySnapshotId { get; init; } = $"AUTH-{Guid.NewGuid():N}";
        public string BudgetSnapshotId { get; init; } = $"BUD-{Guid.NewGuid():N}";
        public string RiskSnapshotId { get; init; } = $"RISK-{Guid.NewGuid():N}";
        public string CapabilitySnapshotId { get; init; } = $"CAP-{Guid.NewGuid():N}";
        public string ModelPolicySnapshotId { get; init; } = $"MDL-{Guid.NewGuid():N}";
        public DateTime CapturedAtUtc { get; init; } = DateTime.UtcNow;

        public static PolicySnapshotContext CreateDefault() => new();
    }

    public record ModelProvenanceContext
    {
        public string ModelProvider { get; init; } = string.Empty;
        public string ModelId { get; init; } = string.Empty;
        public string ModelVersion { get; init; } = string.Empty;
        public string PromptRegistryId { get; init; } = string.Empty;
        public string PromptVersion { get; init; } = string.Empty;
        public string InputHash { get; init; } = string.Empty;
        public string OutputHash { get; init; } = string.Empty;
        public string InferencePolicyId { get; init; } = string.Empty;
        public string SchemaVersion { get; init; } = "1.0.0";
        public long PromptTokens { get; init; }
        public long CompletionTokens { get; init; }
        public long LatencyMs { get; init; }
        public decimal EstimatedCost { get; init; }
    }

    public record CapabilityRequest
    {
        public CapabilityId CapabilityId { get; init; }
        public Guid WorkspaceId { get; init; }
        public string RequesterId { get; init; } = string.Empty;
        public double RequiredTrustTier { get; init; } = 0.5;
        public int MaxPermittedRiskTier { get; init; } = 2; // R2 moderate by default
        public IReadOnlyList<string> RequiredPermissions { get; init; } = Array.Empty<string>();

        public CapabilityRequest(CapabilityId capabilityId, Guid workspaceId)
        {
            if (workspaceId == Guid.Empty)
                throw new ArgumentException("WorkspaceId cannot be empty.", nameof(workspaceId));

            CapabilityId = capabilityId;
            WorkspaceId = workspaceId;
        }
    }

    public record CapabilityResolutionResult
    {
        public bool IsResolved { get; init; }
        public CapabilityId? MatchedCapabilityId { get; init; }
        public CapabilityState State { get; init; }
        public string? FailureReason { get; init; }
        public bool IsSandboxedOnly { get; init; }

        public static CapabilityResolutionResult Resolved(CapabilityId id, CapabilityState state, bool isSandboxed = false) => new()
        {
            IsResolved = true,
            MatchedCapabilityId = id,
            State = state,
            IsSandboxedOnly = isSandboxed
        };

        public static CapabilityResolutionResult Denied(string reason) => new()
        {
            IsResolved = false,
            FailureReason = reason
        };
    }
}
