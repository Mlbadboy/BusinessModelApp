using System;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Responsibilities;
using BusinessModelApp.Core.Interfaces.Ambient;

namespace BusinessModelApp.Infrastructure.Runtime.Ambient
{
    public class AmbientWatchdogScheduler : IAmbientWatchdogScheduler
    {
        private readonly IResponsibilityRegistry _registry;
        private readonly IResponsibilityEscalator _escalator;

        public AmbientWatchdogScheduler(
            IResponsibilityRegistry registry,
            IResponsibilityEscalator escalator)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _escalator = escalator ?? throw new ArgumentNullException(nameof(escalator));
        }

        public async Task TriggerWatchdogEvaluationAsync(Guid workspaceId, CancellationToken cancellationToken = default)
        {
            if (workspaceId == Guid.Empty) return;

            var activeResponsibilities = await _registry.GetActiveResponsibilitiesAsync(workspaceId, cancellationToken);
            foreach (var record in activeResponsibilities)
            {
                // 1. Check Suppression Expiration
                if (record.Suppression.IsSuppressed &&
                    record.Suppression.ExpiresAtUtc.HasValue &&
                    DateTime.UtcNow > record.Suppression.ExpiresAtUtc.Value)
                {
                    record.Suppression = new ResponsibilitySuppressionDetails { IsSuppressed = false };
                    record.State = ResponsibilityLifecycleState.Active;
                }

                // 2. Check SLA Watchdog (Active > 24 hours escalates to P1 or P0)
                if (record.State == ResponsibilityLifecycleState.Active && record.FirstTriggeredUtc.HasValue)
                {
                    var activeDuration = DateTime.UtcNow - record.FirstTriggeredUtc.Value;
                    if (activeDuration > TimeSpan.FromHours(24) && record.Priority > ResponsibilityPriority.P1_High)
                    {
                        record.Priority = ResponsibilityPriority.P1_High;
                    }
                }

                await _registry.SaveActiveResponsibilityAsync(record, cancellationToken);
            }
        }
    }
}
