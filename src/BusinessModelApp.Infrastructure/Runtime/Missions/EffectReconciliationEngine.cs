using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Missions;
using BusinessModelApp.Core.Domain.Runtime;
using BusinessModelApp.Core.Interfaces.Missions;

namespace BusinessModelApp.Infrastructure.Runtime.Missions
{
    public class EffectReconciliationEngine : IEffectReconciliationEngine
    {
        private readonly ConcurrentDictionary<string, NodeExecutionEffect> _reconciliationRegistry = new(StringComparer.Ordinal);
        private readonly IMissionGraphAuditLedger _auditLedger;

        public EffectReconciliationEngine(IMissionGraphAuditLedger auditLedger)
        {
            _auditLedger = auditLedger ?? throw new ArgumentNullException(nameof(auditLedger));
        }

        public void RegisterReconciledEffect(MissionGraphId graphId, MissionNodeId nodeId, NodeExecutionEffect effect)
        {
            var key = $"{graphId}:{nodeId}";
            _reconciliationRegistry[key] = effect;
        }

        public async Task<NodeExecutionEffect> ReconcileEffectAsync(
            MissionGraphId graphId,
            MissionNodeId nodeId,
            CancellationToken ct = default)
        {
            var key = $"{graphId}:{nodeId}";

            // If a reconciliation result was established, return it
            if (_reconciliationRegistry.TryGetValue(key, out var effect))
            {
                await _auditLedger.RecordEventAsync(new MissionGraphAuditEntry
                {
                    GraphId = graphId,
                    Version = MissionGraphVersion.Initial,
                    EventType = "EffectReconciled",
                    Details = $"Reconciled effect for node {nodeId}: {effect}",
                    Sha256Hash = string.Empty
                }, ct);

                return effect;
            }

            // Otherwise, effect remains UnknownEffect (fail-closed, blocks blind retries)
            return NodeExecutionEffect.UnknownEffect;
        }
    }
}
