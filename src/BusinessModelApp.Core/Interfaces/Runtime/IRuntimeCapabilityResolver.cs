using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime;

namespace BusinessModelApp.Core.Interfaces.Runtime
{
    public interface IRuntimeCapabilityResolver
    {
        Task<CapabilityResolutionResult> ResolveCapabilityAsync(
            CapabilityRequest request,
            CancellationToken cancellationToken = default);

        void RegisterCapability(
            CapabilityId capabilityId,
            CapabilityState state = CapabilityState.Active,
            double minimumTrustRequired = 0.5,
            int maxRiskTier = 2,
            bool isSandboxed = false);
    }
}
