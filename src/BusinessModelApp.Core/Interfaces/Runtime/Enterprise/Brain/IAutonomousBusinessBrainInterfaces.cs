using BusinessModelApp.Core.Domain.Runtime.Enterprise.Brain;

namespace BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Brain;

public interface ICognitiveStateSynthesizer
{
    Task<ExecutiveCognitiveState> SynthesizeStateAsync(string tenantId);
}

public interface IEpistemicGapDetector
{
    Task<IReadOnlyList<EpistemicGapItem>> DetectGapsAsync(string tenantId);
}

public interface ICognitiveContradictionResolver
{
    Task<IReadOnlyList<CognitiveContradictionItem>> DetectContradictionsAsync(string tenantId);
}

public interface IBrainAuditRepository
{
    Task SaveSnapshotAsync(ExecutiveCognitiveState snapshot);
    Task<ExecutiveCognitiveState?> GetLatestSnapshotAsync(string tenantId);
    Task<ExecutiveCognitiveState?> GetSnapshotByIdAsync(string tenantId, string snapshotId);
    Task<IReadOnlyList<ExecutiveCognitiveState>> ListSnapshotsAsync(string tenantId, int limit = 50);
}

/// <summary>
/// Unified facade for Charlie's Autonomous Business Brain (Batch 4.0).
/// Answers the 12 core executive inquiries under Constitutional Invariant I36.
/// </summary>
public interface IAutonomousBusinessBrainService
{
    Task<ExecutiveCognitiveState> GetCurrentCognitiveStateAsync(string tenantId);
    Task<ExecutiveCognitiveState> ForceCognitiveSynthesisAsync(string tenantId);
    Task<IReadOnlyList<EnterpriseDeltasRecord>> GetRecentDeltasAsync(string tenantId);
    Task<IReadOnlyList<AttentionPriorityItem>> GetAttentionPrioritiesAsync(string tenantId);
    Task<IReadOnlyList<EpistemicGapItem>> GetEpistemicGapsAsync(string tenantId);
    Task<IReadOnlyList<CognitiveContradictionItem>> GetCognitiveContradictionsAsync(string tenantId);
    Task<IReadOnlyList<ExecutiveEscalationItem>> GetExecutiveEscalationsAsync(string tenantId);
    Task<ExecutiveCognitiveState?> GetSnapshotByIdAsync(string tenantId, string snapshotId);
}
