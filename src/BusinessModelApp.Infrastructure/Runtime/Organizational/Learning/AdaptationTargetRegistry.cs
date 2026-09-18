using BusinessModelApp.Core.Domain.Runtime.Organizational.Learning;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational.Learning;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational.Learning;

public sealed class AdaptationTargetRegistry : IAdaptationTargetRegistry
{
    public bool TryValidateTarget(
        string parameterKey,
        double proposedDelta,
        out string rejectionReason,
        out WhitelistTargetDefinition? targetDef)
    {
        if (string.IsNullOrWhiteSpace(parameterKey))
        {
            rejectionReason = "Parameter key cannot be empty or null.";
            targetDef = null;
            return false;
        }

        // Closed-world whitelist enforcement (I35-C, I35-K)
        if (!AdaptationTargetWhitelist.IsWhitelisted(parameterKey, out targetDef) || targetDef == null)
        {
            rejectionReason = $"Parameter '{parameterKey}' is not whitelisted. Closed-world governance strictly prohibits adapting arbitrary parameters, system policies, credentials, or execution permissions. (I35-C, I35-K)";
            return false;
        }

        // Check delta magnitude
        if (Math.Abs(proposedDelta) > targetDef.MaxDeltaPerAdaptation + 0.0001)
        {
            rejectionReason = $"Proposed delta {proposedDelta:F4} exceeds maximum permissible single-step adaptation delta {targetDef.MaxDeltaPerAdaptation:F4} for parameter '{parameterKey}'.";
            return false;
        }

        rejectionReason = string.Empty;
        return true;
    }

    public IReadOnlyList<WhitelistTargetDefinition> GetRegisteredTargets()
    {
        return AdaptationTargetWhitelist.AllowedTargets.Values.ToList();
    }
}
