using System;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Responsibilities;
using BusinessModelApp.Core.Domain.Runtime;
using BusinessModelApp.Core.Interfaces.Ambient;

namespace BusinessModelApp.Infrastructure.Runtime.Ambient
{
    public class ResponsibilityDebounceEngine : IResponsibilityDebouncer
    {
        private readonly IResponsibilityRegistry _registry;

        public ResponsibilityDebounceEngine(IResponsibilityRegistry registry)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        public Task<bool> ShouldSuppressAsync(ResponsibilityRecord responsibility, AmbientBusinessEvent businessEvent, CancellationToken cancellationToken = default)
        {
            if (responsibility == null) throw new ArgumentNullException(nameof(responsibility));

            // 1. Check Active Deliberate Suppression
            if (responsibility.Suppression.IsSuppressed)
            {
                if (responsibility.Suppression.ExpiresAtUtc.HasValue &&
                    DateTime.UtcNow > responsibility.Suppression.ExpiresAtUtc.Value)
                {
                    // Suppression has expired! Clear it
                    responsibility.Suppression = new ResponsibilitySuppressionDetails { IsSuppressed = false };
                }
                else
                {
                    // Actively suppressed
                    return Task.FromResult(true);
                }
            }

            // 2. Check Cooldown Window (Prevents multiple triggers in quick succession)
            if (responsibility.LastTriggeredUtc.HasValue)
            {
                var timeSinceLastTrigger = DateTime.UtcNow - responsibility.LastTriggeredUtc.Value;
                if (timeSinceLastTrigger < responsibility.CooldownWindow)
                {
                    // Within cooldown window: Suppress duplicate trigger
                    return Task.FromResult(true);
                }
            }

            return Task.FromResult(false);
        }

        public async Task RecordTriggerAsync(ResponsibilityRecord responsibility, AmbientBusinessEvent businessEvent, CancellationToken cancellationToken = default)
        {
            if (responsibility == null) throw new ArgumentNullException(nameof(responsibility));

            responsibility.LastTriggeredUtc = DateTime.UtcNow;
            if (!responsibility.FirstTriggeredUtc.HasValue)
            {
                responsibility.FirstTriggeredUtc = DateTime.UtcNow;
            }

            responsibility.ConsecutiveBreaches++;
            responsibility.ConsecutiveRecoveries = 0;
            responsibility.RecoveryStartedUtc = null;

            if (responsibility.State == ResponsibilityLifecycleState.Detected)
            {
                responsibility.State = ResponsibilityLifecycleState.Active;
            }

            await _registry.SaveActiveResponsibilityAsync(responsibility, cancellationToken);
        }

        public async Task<bool> CheckRecoveryAsync(ResponsibilityRecord responsibility, decimal currentValue, decimal recoveryThreshold, CancellationToken cancellationToken = default)
        {
            if (responsibility == null) throw new ArgumentNullException(nameof(responsibility));

            // Hysteresis check: metric must cross recovery threshold
            if (currentValue >= recoveryThreshold)
            {
                responsibility.ConsecutiveRecoveries++;
                if (!responsibility.RecoveryStartedUtc.HasValue)
                {
                    responsibility.RecoveryStartedUtc = DateTime.UtcNow;
                }

                var sustainedRecovery = DateTime.UtcNow - responsibility.RecoveryStartedUtc.Value;
                
                // Must sustain past recovery persistence window (e.g. 2 consecutive observations or time window)
                if (responsibility.ConsecutiveRecoveries >= 2 || sustainedRecovery >= responsibility.RecoveryPersistenceWindow)
                {
                    responsibility.State = ResponsibilityLifecycleState.Resolved;
                    responsibility.ResolvedAtUtc = DateTime.UtcNow;
                    await _registry.SaveActiveResponsibilityAsync(responsibility, cancellationToken);
                    return true;
                }
            }
            else
            {
                // Dropped back below recovery threshold - reset recovery tracking
                responsibility.ConsecutiveRecoveries = 0;
                responsibility.RecoveryStartedUtc = null;
            }

            return false;
        }

        public async Task SuppressResponsibilityAsync(ResponsibilityId responsibilityId, string reason, string actor, TimeSpan duration, CancellationToken cancellationToken = default)
        {
            var active = await _registry.GetActiveResponsibilityAsync(responsibilityId, cancellationToken);
            if (active != null)
            {
                active.State = ResponsibilityLifecycleState.Suppressed;
                active.Suppression = new ResponsibilitySuppressionDetails
                {
                    IsSuppressed = true,
                    Reason = reason,
                    SuppressedByActor = actor,
                    SuppressedAtUtc = DateTime.UtcNow,
                    ExpiresAtUtc = DateTime.UtcNow + duration
                };

                await _registry.SaveActiveResponsibilityAsync(active, cancellationToken);
            }
        }
    }
}
