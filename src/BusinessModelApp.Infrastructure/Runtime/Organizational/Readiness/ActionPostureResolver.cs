using BusinessModelApp.Core.Domain.Runtime.Organizational.Readiness;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational.Readiness;

/// <summary>
/// Resolves organizational posture: "Prepare Now vs Wait".
/// Invariant I31-B: Prediction != Inevitability.
/// Invariant I31-G: Probability != Certainty.
/// Replaces opaque single scores with an executive decision matrix.
/// </summary>
public class ActionPostureResolver : IActionPostureResolver
{
    public ReadinessActionPosture ResolvePosture(
        double estimatedProbability,
        double impactMagnitude,
        HorizonWindow horizon,
        double preparationCost,
        double reversibilityScore,
        double confidenceInterval)
    {
        // 1. Check for insufficient epistemic confidence / ungrounded forecast
        if (confidenceInterval < 0.40)
        {
            return ReadinessActionPosture.InsufficientEvidence;
        }

        // Clamp inputs
        estimatedProbability = Math.Clamp(estimatedProbability, 0.0, 1.0);
        impactMagnitude = Math.Clamp(impactMagnitude, 0.0, 1.0);
        reversibilityScore = Math.Clamp(reversibilityScore, 0.0, 1.0);
        preparationCost = Math.Clamp(preparationCost, 0.0, 1.0);

        // 2. High exposure regime: High probability and significant impact
        if (estimatedProbability >= 0.70 && impactMagnitude >= 0.40)
        {
            if (horizon == HorizonWindow.Immediate)
            {
                // If preparation is irreversibly costly and probability < 0.90, exercise caution -> Prepare instead of ActNow
                if (preparationCost > 0.85 && reversibilityScore < 0.20 && estimatedProbability < 0.90)
                {
                    return ReadinessActionPosture.Prepare;
                }
                return ReadinessActionPosture.ActNow;
            }

            if (horizon == HorizonWindow.NearTerm)
            {
                return ReadinessActionPosture.Prepare;
            }

            // Distant horizon: Watch drift via Watchtower
            return ReadinessActionPosture.Watch;
        }

        // 3. Moderate exposure regime: Moderate probability or moderate impact
        if (estimatedProbability >= 0.40 && impactMagnitude >= 0.30)
        {
            if (horizon <= HorizonWindow.NearTerm)
            {
                return ReadinessActionPosture.Prepare;
            }
            return ReadinessActionPosture.Watch;
        }

        // 4. Low impact or low probability regime
        if (impactMagnitude >= 0.50 && horizon == HorizonWindow.Immediate)
        {
            // Low probability but catastrophic if immediate
            return ReadinessActionPosture.Watch;
        }

        return ReadinessActionPosture.Wait;
    }
}
