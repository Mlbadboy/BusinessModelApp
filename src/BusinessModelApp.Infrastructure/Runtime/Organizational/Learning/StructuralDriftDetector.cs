using BusinessModelApp.Core.Domain.Runtime.Organizational.Learning;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational.Learning;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational.Learning;

public sealed class StructuralDriftDetector : IStructuralDriftDetector
{
    public DriftDetectionResult EvaluateDrift(
        string tenantId,
        string targetModel,
        IReadOnlyList<EmpiricalOutcomeEvent> recentOutcomes,
        IReadOnlyList<EmpiricalOutcomeEvent> baselineOutcomes)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));
        if (string.IsNullOrWhiteSpace(targetModel)) throw new ArgumentNullException(nameof(targetModel));

        var result = new DriftDetectionResult
        {
            DriftId = Guid.NewGuid().ToString("N"),
            TenantId = tenantId,
            TargetModel = targetModel,
            DetectedUtc = DateTime.UtcNow
        };

        if (recentOutcomes == null || recentOutcomes.Count == 0 || baselineOutcomes == null || baselineOutcomes.Count == 0)
        {
            result.DetectedDriftType = DriftType.None;
            result.DivergenceScore = 0.0;
            result.PValue = 1.0;
            result.RequiresCaution = false;
            result.RecommendedPosture = "Nominal";
            return result;
        }

        // Check for regime transition across recent outcomes
        var recentRegimes = recentOutcomes.Select(o => o.EnvironmentRegime).Distinct().ToList();
        var baselineRegimes = baselineOutcomes.Select(o => o.EnvironmentRegime).Distinct().ToList();
        bool regimeMismatched = recentRegimes.Any(r => !baselineRegimes.Contains(r));

        var recentMeanError = recentOutcomes.Average(o => Math.Abs(o.DeltaValue));
        var baselineMeanError = baselineOutcomes.Average(o => Math.Abs(o.DeltaValue));
        var denominator = baselineMeanError > 0.0001 ? baselineMeanError : 1.0;
        var divergence = Math.Clamp(Math.Abs(recentMeanError - baselineMeanError) / denominator, 0.0, 1.0);

        result.DivergenceScore = Math.Round(divergence, 4);

        if (regimeMismatched)
        {
            result.DetectedDriftType = DriftType.RegimeTransition;
            result.PValue = 0.01;
            result.RequiresCaution = true;
            result.RecommendedPosture = "Cautious";
        }
        else if (divergence >= 0.40)
        {
            result.DetectedDriftType = DriftType.ConceptDrift;
            result.PValue = 0.02;
            result.RequiresCaution = true;
            result.RecommendedPosture = "Cautious";
        }
        else if (divergence >= 0.20)
        {
            result.DetectedDriftType = DriftType.CovariateShift;
            result.PValue = 0.04;
            result.RequiresCaution = true;
            result.RecommendedPosture = "Cautious";
        }
        else
        {
            result.DetectedDriftType = DriftType.None;
            result.PValue = 0.50;
            result.RequiresCaution = false;
            result.RecommendedPosture = "Nominal";
        }

        // Adheres strictly to I35-Z: Drift Detection evaluates and recommends cautious posture,
        // but NEVER executes model alterations or policy mutations automatically.
        return result;
    }
}
