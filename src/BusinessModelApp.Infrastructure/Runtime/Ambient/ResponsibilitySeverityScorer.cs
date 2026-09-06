using System;
using BusinessModelApp.Core.Interfaces.Ambient;

namespace BusinessModelApp.Infrastructure.Runtime.Ambient
{
    public class ResponsibilitySeverityScorer : IResponsibilitySeverityScorer
    {
        public decimal CalculateSeverityScore(
            decimal businessImpact,
            decimal urgency,
            double confidence,
            int persistenceBreaches,
            decimal exposureValue)
        {
            var conf = (decimal)Math.Clamp(confidence, 0.1, 1.0);
            var persistMultiplier = Math.Clamp(1.0m + (persistenceBreaches * 0.2m), 1.0m, 3.0m);
            var normalizedExposure = Math.Clamp(exposureValue > 0 ? (decimal)Math.Log10((double)exposureValue + 1.0) : 1.0m, 1.0m, 5.0m);

            var rawScore = businessImpact * urgency * conf * persistMultiplier * normalizedExposure;
            return Math.Round(Math.Clamp(rawScore, 0.1m, 100.0m), 2);
        }
    }
}
