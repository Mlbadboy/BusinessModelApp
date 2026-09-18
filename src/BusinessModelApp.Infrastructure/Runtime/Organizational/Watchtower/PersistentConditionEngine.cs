using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Organizational.Watchtower;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational.Watchtower
{
    public class PersistentConditionEngine : IPersistentConditionTracker
    {
        private readonly IWatchtowerStore _store;

        public PersistentConditionEngine(IWatchtowerStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public async Task<PersistentCondition> UpdateConditionAsync(
            string tenantId,
            string correlationKey,
            string entityId,
            string metricName,
            double observedValue,
            double baselineValue,
            int persistenceThreshold,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentException("TenantId is required.");
            if (string.IsNullOrWhiteSpace(correlationKey)) throw new ArgumentException("CorrelationKey is required.");

            var conditionId = $"CND-{correlationKey.Replace("::", "-")}";
            var condition = await _store.GetConditionAsync(tenantId, conditionId, ct);
            var now = DateTime.UtcNow;

            if (condition == null)
            {
                // Initial observation
                double initialVariance = baselineValue != 0.0 ? (observedValue - baselineValue) / baselineValue : 0.0;
                var initialTrajectory = Math.Abs(initialVariance) > 0.25
                    ? ConditionTrajectory.TransientSpike
                    : ConditionTrajectory.Unknown;

                condition = new PersistentCondition
                {
                    ConditionId = conditionId,
                    TenantId = tenantId,
                    CorrelationKey = correlationKey,
                    EntityId = entityId,
                    MetricName = metricName,
                    BaselineValue = baselineValue,
                    ObservedValue = observedValue,
                    Trajectory = initialTrajectory,
                    ObservationCount = 1,
                    FirstObservedUtc = now,
                    LastObservedUtc = now,
                    Velocity = 0.0,
                    Acceleration = 0.0,
                    IsPersistent = persistenceThreshold <= 1
                };
            }
            else
            {
                // Subsequent observation
                var dt = Math.Max(1.0, (now - condition.LastObservedUtc).TotalSeconds);
                var prevObserved = condition.ObservedValue;
                var prevVelocity = condition.Velocity;

                var newVelocity = (observedValue - prevObserved) / dt;
                var newAcceleration = (newVelocity - prevVelocity) / dt;

                condition.ObservedValue = observedValue;
                condition.ObservationCount++;
                condition.LastObservedUtc = now;
                condition.Velocity = Math.Round(newVelocity, 4);
                condition.Acceleration = Math.Round(newAcceleration, 4);

                double variance = condition.Variance;
                bool isPersistent = condition.ObservationCount >= persistenceThreshold;
                condition.IsPersistent = isPersistent;

                // Classify Trajectory
                if (Math.Abs(variance) <= 0.05)
                {
                    condition.Trajectory = ConditionTrajectory.Recovering;
                }
                else if (isPersistent)
                {
                    if (newAcceleration > 0.01 && Math.Abs(variance) > 0.2)
                    {
                        condition.Trajectory = ConditionTrajectory.AcceleratingDeterioration;
                    }
                    else if (Math.Abs(newVelocity) < 0.05)
                    {
                        condition.Trajectory = ConditionTrajectory.PersistentDeviation;
                    }
                    else
                    {
                        condition.Trajectory = ConditionTrajectory.Drift;
                    }
                }
                else
                {
                    condition.Trajectory = ConditionTrajectory.TransientSpike;
                }
            }

            condition.ComputeConditionHash();
            await _store.SaveConditionAsync(condition, ct);
            return condition;
        }

        public Task<PersistentCondition?> GetConditionAsync(string tenantId, string conditionId, CancellationToken ct = default)
        {
            return _store.GetConditionAsync(tenantId, conditionId, ct);
        }

        public Task<IReadOnlyList<PersistentCondition>> ListConditionsAsync(string tenantId, CancellationToken ct = default)
        {
            return _store.ListConditionsAsync(tenantId, ct);
        }
    }
}
