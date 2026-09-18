using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Organizational.Collaboration;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational.Collaboration
{
    public class DisputeArbitrator : IDisputeArbitrator
    {
        private readonly ITeamCharterStore _store;

        public DisputeArbitrator(ITeamCharterStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public async Task<DisputeRecord> ArbitrateDisputeAsync(
            string tenantId,
            string charterId,
            DisputeType disputeType,
            string topic,
            string initiatorAgentId,
            IReadOnlyList<string> contendingAgentIds,
            Dictionary<string, string> claims,
            Dictionary<string, double>? evidenceScores = null,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentException("TenantId is required.");
            if (string.IsNullOrWhiteSpace(charterId)) throw new ArgumentException("CharterId is required.");

            var charter = await _store.GetCharterAsync(tenantId, charterId, ct);
            var contenders = contendingAgentIds?.ToList() ?? new List<string>();
            if (!contenders.Contains(initiatorAgentId))
            {
                contenders.Add(initiatorAgentId);
            }

            ArbitrationOutcome outcome;
            string resolutionEvidence;
            bool escalated = false;

            // Invariant I29-E Protocol:
            // 1. Policy & Constraint Dominance
            if (disputeType == DisputeType.PolicyInterpretation)
            {
                outcome = ArbitrationOutcome.PolicyPrevails;
                resolutionEvidence = "Hard business constraints and enterprise policy boundaries dominate agent preferences per Invariant I29-E.";
            }
            // 2. Evidence Precedence
            else if (evidenceScores != null && evidenceScores.Count > 0)
            {
                var sorted = evidenceScores.OrderByDescending(kv => kv.Value).ToList();
                if (sorted.Count >= 2 && Math.Abs(sorted[0].Value - sorted[1].Value) > 0.05)
                {
                    outcome = ArbitrationOutcome.EvidencePrevails;
                    resolutionEvidence = $"Contender '{sorted[0].Key}' prevailed with highest verifiable evidence score ({sorted[0].Value:0.00} vs {sorted[1].Value:0.00}).";
                }
                else if (sorted.Count == 1)
                {
                    outcome = ArbitrationOutcome.EvidencePrevails;
                    resolutionEvidence = $"Contender '{sorted[0].Key}' produced verifiable evidence score ({sorted[0].Value:0.00}).";
                }
                else
                {
                    // Tied evidence -> Specialization check
                    outcome = ResolveBySpecializationOrEscalate(charter, contenders, topic, out resolutionEvidence, out escalated);
                }
            }
            // 3. Specialization / Reputation check
            else
            {
                outcome = ResolveBySpecializationOrEscalate(charter, contenders, topic, out resolutionEvidence, out escalated);
            }

            var dispute = new DisputeRecord
            {
                DisputeId = $"DSP-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
                TenantId = tenantId,
                CharterId = charterId,
                DisputeType = disputeType,
                Topic = topic,
                InitiatorAgentId = initiatorAgentId,
                ContendingAgentIds = contenders,
                ConflictingClaims = claims != null ? new Dictionary<string, string>(claims) : new(),
                ResolutionOutcome = outcome,
                ResolutionEvidence = resolutionEvidence,
                EscalatedToGovernance = escalated,
                ResolvedUtc = DateTime.UtcNow
            };

            dispute.ComputeDisputeHash();
            await _store.SaveDisputeAsync(dispute, ct);

            // Update charter status if escalated
            if (charter != null)
            {
                if (escalated)
                {
                    charter.Status = TeamLifecycleStatus.Disputed;
                    await _store.SaveCharterAsync(charter, ct);
                }
            }

            return dispute;
        }

        private static ArbitrationOutcome ResolveBySpecializationOrEscalate(
            TeamCharter? charter,
            List<string> contenders,
            string topic,
            out string resolutionEvidence,
            out bool escalated)
        {
            if (charter != null)
            {
                // Check if any contender has exact specialization domain matching the topic
                var matchingMembers = charter.Members
                    .Where(m => contenders.Contains(m.AgentInstanceId, StringComparer.OrdinalIgnoreCase) &&
                                !string.IsNullOrWhiteSpace(m.SpecializationDomain) &&
                                topic.Contains(m.SpecializationDomain, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (matchingMembers.Count == 1)
                {
                    escalated = false;
                    resolutionEvidence = $"Contender '{matchingMembers[0].AgentInstanceId}' has primary specialization authority in '{matchingMembers[0].SpecializationDomain}'.";
                    return ArbitrationOutcome.ReputationPrevails;
                }

                // If governance tier >= 2 or resource contention without clear winner, escalate
                if (charter.GovernanceTier >= 2)
                {
                    escalated = true;
                    resolutionEvidence = $"Dispute on high-governance charter (Tier {charter.GovernanceTier}) cannot be autonomously resolved; escalated to PRG-1 human governance.";
                    return ArbitrationOutcome.EscalatedToGovernance;
                }
            }

            escalated = false;
            resolutionEvidence = "Dispute resolved via balanced split compromise between contenders.";
            return ArbitrationOutcome.SplitCompromise;
        }
    }
}
