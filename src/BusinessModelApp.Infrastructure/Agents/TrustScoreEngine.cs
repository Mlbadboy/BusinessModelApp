using System;
using System.Collections.Concurrent;
using BusinessModelApp.Core.Agents;

namespace BusinessModelApp.Infrastructure.Agents
{
    public class TrustScoreEngine : ITrustScoreEngine
    {
        private readonly ConcurrentDictionary<string, AgentTrustScore> _scores = new(StringComparer.OrdinalIgnoreCase);

        public AgentTrustScore UpdateTrustScore(string agentId, bool taskSucceeded, bool budgetAdhered, bool policyComplied, bool hallucinationDetected)
        {
            if (string.IsNullOrWhiteSpace(agentId)) throw new ArgumentNullException(nameof(agentId));

            var score = _scores.GetOrAdd(agentId, _ => new AgentTrustScore());

            // Rolling adjustment
            score.EvaluatedObservationsCount++;
            double weight = 1.0 / Math.Min(score.EvaluatedObservationsCount, 20);

            score.AccuracyScore = Math.Clamp(score.AccuracyScore * (1 - weight) + (taskSucceeded ? 1.0 : 0.0) * weight, 0.0, 1.0);
            score.BudgetDisciplineScore = Math.Clamp(score.BudgetDisciplineScore * (1 - weight) + (budgetAdhered ? 1.0 : 0.0) * weight, 0.0, 1.0);
            score.PolicyComplianceScore = Math.Clamp(score.PolicyComplianceScore * (1 - weight) + (policyComplied ? 1.0 : 0.0) * weight, 0.0, 1.0);
            score.HallucinationRate = Math.Clamp(score.HallucinationRate * (1 - weight) + (hallucinationDetected ? 1.0 : 0.0) * weight, 0.0, 1.0);

            score.Recalculate();
            return score;
        }

        public bool CanExecuteCapability(AgentCard card, string capability, bool constitutionalPolicyAllows)
        {
            if (card == null) throw new ArgumentNullException(nameof(card));

            // Invariant 1: Constitutional policy overrides everything. If Constitution denies, Trust CANNOT override.
            if (!constitutionalPolicyAllows)
            {
                return false;
            }

            // Invariant 2: Card authority check (ForbiddenCapabilities takes precedence)
            if (!card.IsCapabilityPermitted(capability))
            {
                return false;
            }

            // Invariant 3: Trust score threshold check. Below 0.70 cannot execute sensitive commercial actions
            if (card.TrustScore.OverallScore < 0.70)
            {
                // Unearned or degraded trust blocks autonomous execution
                return false;
            }

            return true;
        }
    }
}
