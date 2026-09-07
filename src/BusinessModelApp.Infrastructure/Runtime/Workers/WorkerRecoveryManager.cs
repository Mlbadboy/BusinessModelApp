using System;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Missions;
using BusinessModelApp.Core.Domain.Runtime;
using BusinessModelApp.Core.Domain.Runtime.Workers;
using BusinessModelApp.Core.Interfaces.Runtime.Workers;

namespace BusinessModelApp.Infrastructure.Runtime.Workers
{
    /// <summary>
    /// Governed Worker Recovery Manager.
    /// Reconciles UnknownEffect execution states without blind retries (preserving ARK-15).
    /// Inspects idempotency records and deterministic audit logs to resolve effect state.
    /// </summary>
    public class WorkerRecoveryManager : IWorkerRecoveryManager
    {
        private readonly IWorkerStore _store;

        public WorkerRecoveryManager(IWorkerStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public async Task<NodeExecutionEffect> ReconcileUnknownEffectAsync(WorkerAttempt attempt, CancellationToken ct = default)
        {
            if (attempt == null)
                throw new ArgumentNullException(nameof(attempt));

            // If the attempt is already resolved with known effect, return it directly
            if (attempt.ResultingEffect != NodeExecutionEffect.UnknownEffect)
            {
                return attempt.ResultingEffect;
            }

            // Retrieve associated worker instance
            var instance = await _store.GetWorkerInstanceAsync(attempt.WorkerInstanceId, ct);
            if (instance == null)
            {
                return NodeExecutionEffect.UnknownEffect;
            }

            // Check if an ActionProposal exists for this attempt
            var proposals = await _store.GetActionProposalsAsync(instance.WorkspaceId, ct);
            WorkerActionProposal? matchedProposal = null;
            foreach (var p in proposals)
            {
                if (p.AttemptId == attempt.AttemptId && p.WorkerInstanceId == attempt.WorkerInstanceId)
                {
                    matchedProposal = p;
                    break;
                }
            }

            if (matchedProposal != null)
            {
                // If proposal was admitted and has verified proof of execution receipt
                if (matchedProposal.EvidencePayload.Contains("VERIFIED_RECEIPT") ||
                    matchedProposal.EvidencePayload.Contains("EFFECT_COMMITTED"))
                {
                    attempt.ResultingEffect = NodeExecutionEffect.EffectSucceeded;
                    attempt.IsSuccess = true;
                    return NodeExecutionEffect.EffectSucceeded;
                }

                // If proof explicitly confirms rejection or pre-flight cancellation without mutation
                if (matchedProposal.EvidencePayload.Contains("ABORTED_PRE_COMMIT") ||
                    matchedProposal.EvidencePayload.Contains("NO_MUTATION"))
                {
                    attempt.ResultingEffect = NodeExecutionEffect.NoEffect;
                    return NodeExecutionEffect.NoEffect;
                }
            }

            // Indeterminate state: Cannot verify external mutation -> Remain UnknownEffect
            // Strictly prevents blind replay or duplicate consequential side-effects
            return NodeExecutionEffect.UnknownEffect;
        }
    }
}
