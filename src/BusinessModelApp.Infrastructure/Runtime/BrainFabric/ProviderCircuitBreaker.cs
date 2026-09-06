using System;
using System.Collections.Concurrent;
using BusinessModelApp.Core.Domain.Runtime;
using BusinessModelApp.Core.Interfaces.Runtime;

namespace BusinessModelApp.Infrastructure.Runtime.BrainFabric
{
    public class ProviderCircuitBreaker : IProviderCircuitBreaker
    {
        private readonly ConcurrentDictionary<string, ProviderHealthScore> _scores = new();
        private readonly int _consecutiveFailureThreshold;
        private readonly TimeSpan _coolDownPeriod;

        public ProviderCircuitBreaker(int consecutiveFailureThreshold = 3, TimeSpan? coolDownPeriod = null)
        {
            _consecutiveFailureThreshold = consecutiveFailureThreshold;
            _coolDownPeriod = coolDownPeriod ?? TimeSpan.FromSeconds(30);
        }

        public bool IsCircuitOpen(ProviderId providerId)
        {
            if (!_scores.TryGetValue(providerId.Value, out var score))
                return false;

            if (!score.IsCircuitOpen)
                return false;

            // Check cooldown period (half-open check)
            if (score.CircuitOpenedAtUtc.HasValue &&
                DateTime.UtcNow - score.CircuitOpenedAtUtc.Value > _coolDownPeriod)
            {
                // Permit half-open probe
                return false;
            }

            return true;
        }

        public void RecordSuccess(ProviderId providerId)
        {
            _scores.AddOrUpdate(providerId.Value,
                _ => new ProviderHealthScore
                {
                    ConsecutiveFailures = 0,
                    SuccessCount = 1,
                    FailureCount = 0,
                    IsCircuitOpen = false,
                    CircuitOpenedAtUtc = null
                },
                (_, existing) => new ProviderHealthScore
                {
                    ConsecutiveFailures = 0,
                    SuccessCount = existing.SuccessCount + 1,
                    FailureCount = existing.FailureCount,
                    IsCircuitOpen = false,
                    CircuitOpenedAtUtc = null,
                    LastFailureUtc = existing.LastFailureUtc
                });
        }

        public void RecordFailure(ProviderId providerId, string reason)
        {
            _scores.AddOrUpdate(providerId.Value,
                _ =>
                {
                    var consecutive = 1;
                    var isOpen = consecutive >= _consecutiveFailureThreshold;
                    return new ProviderHealthScore
                    {
                        ConsecutiveFailures = consecutive,
                        SuccessCount = 0,
                        FailureCount = 1,
                        IsCircuitOpen = isOpen,
                        CircuitOpenedAtUtc = isOpen ? DateTime.UtcNow : null,
                        LastFailureUtc = DateTime.UtcNow
                    };
                },
                (_, existing) =>
                {
                    var consecutive = existing.ConsecutiveFailures + 1;
                    var isOpen = consecutive >= _consecutiveFailureThreshold;
                    return new ProviderHealthScore
                    {
                        ConsecutiveFailures = consecutive,
                        SuccessCount = existing.SuccessCount,
                        FailureCount = existing.FailureCount + 1,
                        IsCircuitOpen = isOpen,
                        CircuitOpenedAtUtc = isOpen ? (existing.CircuitOpenedAtUtc ?? DateTime.UtcNow) : null,
                        LastFailureUtc = DateTime.UtcNow
                    };
                });
        }

        public ProviderHealthScore GetHealthScore(ProviderId providerId)
        {
            if (_scores.TryGetValue(providerId.Value, out var score))
                return score;

            return new ProviderHealthScore
            {
                ConsecutiveFailures = 0,
                SuccessCount = 0,
                FailureCount = 0,
                IsCircuitOpen = false
            };
        }
    }
}
