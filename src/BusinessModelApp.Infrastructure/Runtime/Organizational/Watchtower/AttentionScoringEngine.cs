using System;
using BusinessModelApp.Core.Domain.Runtime.Organizational.Watchtower;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational.Watchtower
{
    public class AttentionScoringEngine : IAttentionScoringEngine
    {
        public AttentionScoreBreakdown ScoreCondition(
            PersistentCondition condition,
            double exposureScore = 0.5,
            double confidenceScore = 1.0)
        {
            if (condition == null) throw new ArgumentNullException(nameof(condition));

            double impact = Math.Clamp(Math.Abs(condition.Variance), 0.0, 1.0);

            double urgency = condition.Trajectory switch
            {
                ConditionTrajectory.AcceleratingDeterioration => 0.95,
                ConditionTrajectory.PersistentDeviation => 0.70,
                ConditionTrajectory.Drift => 0.50,
                ConditionTrajectory.TransientSpike => 0.35,
                ConditionTrajectory.Recovering => 0.15,
                _ => 0.10
            };

            double persistence = Math.Clamp(condition.ObservationCount / 10.0, 0.0, 1.0);
            double exposure = Math.Clamp(exposureScore, 0.0, 1.0);
            double confidence = Math.Clamp(confidenceScore, 0.0, 1.0);

            var breakdown = new AttentionScoreBreakdown
            {
                ImpactScore = Math.Round(impact, 4),
                UrgencyScore = Math.Round(urgency, 4),
                PersistenceScore = Math.Round(persistence, 4),
                ExposureScore = Math.Round(exposure, 4),
                ConfidenceScore = Math.Round(confidence, 4)
            };

            breakdown.CalculateComposite();
            return breakdown;
        }
    }
}
