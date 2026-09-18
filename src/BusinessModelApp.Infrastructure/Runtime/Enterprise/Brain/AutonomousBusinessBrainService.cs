using BusinessModelApp.Core.Domain.Runtime.Enterprise.Brain;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Brain;

namespace BusinessModelApp.Infrastructure.Runtime.Enterprise.Brain;

public sealed class AutonomousBusinessBrainService : IAutonomousBusinessBrainService
{
    private readonly ICognitiveStateSynthesizer _synthesizer;
    private readonly IBrainAuditRepository _repository;

    public AutonomousBusinessBrainService(
        ICognitiveStateSynthesizer synthesizer,
        IBrainAuditRepository repository)
    {
        _synthesizer = synthesizer ?? throw new ArgumentNullException(nameof(synthesizer));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<ExecutiveCognitiveState> GetCurrentCognitiveStateAsync(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));

        var latest = await _repository.GetLatestSnapshotAsync(tenantId);
        if (latest != null)
        {
            return latest;
        }

        // If no snapshot exists yet, synthesize an initial state
        return await ForceCognitiveSynthesisAsync(tenantId);
    }

    public async Task<ExecutiveCognitiveState> ForceCognitiveSynthesisAsync(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));

        var snapshot = await _synthesizer.SynthesizeStateAsync(tenantId);
        await _repository.SaveSnapshotAsync(snapshot);
        return snapshot;
    }

    public async Task<IReadOnlyList<EnterpriseDeltasRecord>> GetRecentDeltasAsync(string tenantId)
    {
        var state = await GetCurrentCognitiveStateAsync(tenantId);
        return state.RecentDeltas;
    }

    public async Task<IReadOnlyList<AttentionPriorityItem>> GetAttentionPrioritiesAsync(string tenantId)
    {
        var state = await GetCurrentCognitiveStateAsync(tenantId);
        return state.AttentionPriorities;
    }

    public async Task<IReadOnlyList<EpistemicGapItem>> GetEpistemicGapsAsync(string tenantId)
    {
        var state = await GetCurrentCognitiveStateAsync(tenantId);
        return state.EpistemicGaps;
    }

    public async Task<IReadOnlyList<CognitiveContradictionItem>> GetCognitiveContradictionsAsync(string tenantId)
    {
        var state = await GetCurrentCognitiveStateAsync(tenantId);
        return state.CognitiveContradictions;
    }

    public async Task<IReadOnlyList<ExecutiveEscalationItem>> GetExecutiveEscalationsAsync(string tenantId)
    {
        var state = await GetCurrentCognitiveStateAsync(tenantId);
        return state.ExecutiveEscalations;
    }

    public Task<ExecutiveCognitiveState?> GetSnapshotByIdAsync(string tenantId, string snapshotId)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));
        if (string.IsNullOrWhiteSpace(snapshotId)) throw new ArgumentNullException(nameof(snapshotId));

        return _repository.GetSnapshotByIdAsync(tenantId, snapshotId);
    }
}
