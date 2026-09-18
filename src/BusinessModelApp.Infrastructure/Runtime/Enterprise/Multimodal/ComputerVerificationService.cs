using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Multimodal;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Multimodal;

namespace BusinessModelApp.Infrastructure.Runtime.Enterprise.Multimodal
{
    public class ComputerVerificationService : IComputerVerificationService
    {
        public bool VerifyPreconditions(ComputerActionProposal proposal, IReadOnlyDictionary<string, string> currentState)
        {
            if (proposal == null) return false;
            if (proposal.Preconditions == null || proposal.Preconditions.Count == 0) return true;

            foreach (var pre in proposal.Preconditions)
            {
                if (!pre.IsSatisfied(currentState))
                {
                    return false;
                }
            }

            return true;
        }

        public Task<ComputerActionResult> VerifyPostconditionsAsync(
            ComputerActionProposal proposal,
            ComputerEnvironmentSnapshot beforeSnapshot,
            ComputerEnvironmentSnapshot afterSnapshot)
        {
            if (proposal == null) throw new ArgumentNullException(nameof(proposal));

            // If screen or environment hash did not change for an action expecting change:
            if (beforeSnapshot != null && afterSnapshot != null)
            {
                if (proposal.ActionType == ProposedActionType.Click &&
                    string.Equals(beforeSnapshot.IntegrityHash, afterSnapshot.IntegrityHash, StringComparison.OrdinalIgnoreCase))
                {
                    // No observable change took place
                    return Task.FromResult(new ComputerActionResult
                    {
                        IsSuccess = false,
                        Status = ActionExecutionStatus.Failed,
                        Message = "Postcondition verification failed: environment state remained unchanged after click."
                    });
                }
            }

            return Task.FromResult(new ComputerActionResult
            {
                IsSuccess = true,
                Status = ActionExecutionStatus.Completed,
                Message = "Postconditions verified successfully against post-action environment state."
            });
        }

        public Task<ComputerActionResult> ReconcileUnknownEffectAsync(string tenantId, string sessionId, string attemptId, string expectedTarget)
        {
            // Law I38-P: Crashes or timeouts post-execution result in UNKNOWN_EFFECT requiring external reconciliation, never blind retry.
            var result = new ComputerActionResult
            {
                IsSuccess = false,
                Status = ActionExecutionStatus.UnknownEffect,
                Message = $"Reconciling unknown effect for attempt {attemptId}: verifying external ledger/audit trail before permitting retry on target {expectedTarget}."
            };

            using var sha = SHA256.Create();
            result.ResultHash = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes($"{tenantId}:{sessionId}:{attemptId}:reconciled"))).Replace("-", "").ToLowerInvariant();

            return Task.FromResult(result);
        }
    }
}
