using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Production;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Production;

namespace BusinessModelApp.Infrastructure.Runtime.Enterprise.Production
{
    public sealed class ProductionIncidentRecoveryService : IProductionIncidentRecoveryService
    {
        private readonly ConcurrentDictionary<string, ProductionKillSwitchState> _killSwitches = new();
        private readonly ConcurrentDictionary<string, ExternalEffectRecord> _effects = new();

        public Task<ProductionKillSwitchState> TriggerKillSwitchAsync(
            string tenantId,
            string authority,
            string reason,
            CancellationToken cancellationToken = default)
        {
            var state = _killSwitches.GetOrAdd(tenantId, _ => new ProductionKillSwitchState { TenantId = tenantId });
            state.Trigger(authority, reason);
            return Task.FromResult(state);
        }

        public Task<ProductionKillSwitchState> ResetKillSwitchAsync(
            string tenantId,
            string authority,
            CancellationToken cancellationToken = default)
        {
            var state = _killSwitches.GetOrAdd(tenantId, _ => new ProductionKillSwitchState { TenantId = tenantId });
            state.Reset(authority);
            return Task.FromResult(state);
        }

        public Task<ProductionKillSwitchState> GetKillSwitchStateAsync(
            string tenantId,
            CancellationToken cancellationToken = default)
        {
            var state = _killSwitches.GetOrAdd(tenantId, _ => new ProductionKillSwitchState { TenantId = tenantId });
            return Task.FromResult(state);
        }

        public Task<ExternalEffectRecord> RecordExternalEffectAsync(
            ExternalEffectRecord effect,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(effect.IdempotencyKey))
                throw new ArgumentException("Idempotency key required for all external effects (Law I43-P).", nameof(effect));

            _effects[effect.ExternalEffectId] = effect;
            return Task.FromResult(effect);
        }

        public Task<ExternalEffectRecord> ReconcileUnknownEffectAsync(
            string effectId,
            ExternalEffectState resolvedState,
            string notes,
            CancellationToken cancellationToken = default)
        {
            if (!_effects.TryGetValue(effectId, out var effect))
                throw new KeyNotFoundException($"External effect '{effectId}' not found.");

            effect.Reconcile(resolvedState, notes);
            return Task.FromResult(effect);
        }
    }
}
