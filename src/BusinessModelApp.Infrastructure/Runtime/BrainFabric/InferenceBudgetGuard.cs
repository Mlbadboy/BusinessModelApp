using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime;
using BusinessModelApp.Core.Interfaces.Runtime;

namespace BusinessModelApp.Infrastructure.Runtime.BrainFabric
{
    public class InferenceBudgetGuard : IInferenceBudgetGuard
    {
        private readonly ConcurrentDictionary<string, InferenceBudgetTracker> _trackers = new();
        private readonly decimal _defaultBudgetUsd;
        private readonly long _defaultTokens;

        public InferenceBudgetGuard(decimal defaultBudgetUsd = 10.0m, long defaultTokens = 1_000_000)
        {
            _defaultBudgetUsd = defaultBudgetUsd;
            _defaultTokens = defaultTokens;
        }

        private string GetKey(Guid workspaceId, MissionRunId? missionRunId) =>
            $"{workspaceId}:{(missionRunId.HasValue ? missionRunId.Value.Value.ToString() : "default")}";

        public Task<InferenceBudgetTracker> GetBudgetTrackerAsync(Guid workspaceId, MissionRunId? missionRunId, CancellationToken cancellationToken = default)
        {
            var key = GetKey(workspaceId, missionRunId);
            var tracker = _trackers.GetOrAdd(key, _ => new InferenceBudgetTracker
            {
                WorkspaceId = workspaceId,
                MissionRunId = missionRunId,
                AllocatedBudgetUsd = _defaultBudgetUsd,
                ConsumedBudgetUsd = 0m,
                AllocatedTokens = _defaultTokens,
                ConsumedTokens = 0
            });

            return Task.FromResult(tracker);
        }

        public async Task<bool> ValidateBudgetAvailabilityAsync(Guid workspaceId, MissionRunId? missionRunId, decimal estimatedCostUsd, long estimatedTokens, CancellationToken cancellationToken = default)
        {
            var tracker = await GetBudgetTrackerAsync(workspaceId, missionRunId, cancellationToken);
            lock (tracker)
            {
                if (tracker.RemainingBudgetUsd < estimatedCostUsd)
                    return false;

                if (tracker.RemainingTokens < estimatedTokens)
                    return false;

                return true;
            }
        }

        public async Task RecordUsageAsync(Guid workspaceId, MissionRunId? missionRunId, decimal actualCostUsd, long actualTokens, CancellationToken cancellationToken = default)
        {
            var tracker = await GetBudgetTrackerAsync(workspaceId, missionRunId, cancellationToken);
            lock (tracker)
            {
                tracker.ConsumedBudgetUsd += actualCostUsd;
                tracker.ConsumedTokens += actualTokens;
            }
        }
    }
}
