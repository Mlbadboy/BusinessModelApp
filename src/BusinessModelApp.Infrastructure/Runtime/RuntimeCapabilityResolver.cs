using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime;
using BusinessModelApp.Core.Interfaces.Runtime;

namespace BusinessModelApp.Infrastructure.Runtime
{
    public class RuntimeCapabilityResolver : IRuntimeCapabilityResolver
    {
        private class RegisteredCapabilityEntry
        {
            public CapabilityId Id { get; init; }
            public CapabilityState State { get; set; }
            public double MinimumTrustRequired { get; init; }
            public int MaxRiskTier { get; init; }
            public bool IsSandboxed { get; init; }
        }

        private readonly ConcurrentDictionary<string, RegisteredCapabilityEntry> _registry = new();

        public RuntimeCapabilityResolver()
        {
            // Register standard default capabilities
            RegisterCapability(new CapabilityId("analytics.query", "v1"), CapabilityState.Active, minimumTrustRequired: 0.2, maxRiskTier: 0);
            RegisterCapability(new CapabilityId("digitaltwin.read", "v1"), CapabilityState.Active, minimumTrustRequired: 0.2, maxRiskTier: 0);
            RegisterCapability(new CapabilityId("crm.read-lead", "v1"), CapabilityState.Active, minimumTrustRequired: 0.3, maxRiskTier: 0);
            RegisterCapability(new CapabilityId("crm.lead-update", "v1"), CapabilityState.Active, minimumTrustRequired: 0.7, maxRiskTier: 2);
            RegisterCapability(new CapabilityId("invoice.reconcile", "v1"), CapabilityState.Active, minimumTrustRequired: 0.8, maxRiskTier: 2);
            RegisterCapability(new CapabilityId("email.draft", "v1"), CapabilityState.Active, minimumTrustRequired: 0.5, maxRiskTier: 1);
            RegisterCapability(new CapabilityId("experimental.tool", "v1-preview"), CapabilityState.Sandboxed, minimumTrustRequired: 0.9, maxRiskTier: 1, isSandboxed: true);
            RegisterCapability(new CapabilityId("legacy.crm-sync", "v0.9"), CapabilityState.Deprecated, minimumTrustRequired: 0.6, maxRiskTier: 2);
        }

        public void RegisterCapability(
            CapabilityId capabilityId,
            CapabilityState state = CapabilityState.Active,
            double minimumTrustRequired = 0.5,
            int maxRiskTier = 2,
            bool isSandboxed = false)
        {
            _registry[capabilityId.ToString()] = new RegisteredCapabilityEntry
            {
                Id = capabilityId,
                State = state,
                MinimumTrustRequired = minimumTrustRequired,
                MaxRiskTier = maxRiskTier,
                IsSandboxed = isSandboxed
            };
        }

        public Task<CapabilityResolutionResult> ResolveCapabilityAsync(
            CapabilityRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            string canonical = request.CapabilityId.ToString();
            if (!_registry.TryGetValue(canonical, out var entry))
            {
                return Task.FromResult(CapabilityResolutionResult.Denied(
                    $"Capability '{canonical}' is not registered in the runtime capability registry."));
            }

            if (entry.State == CapabilityState.Revoked || entry.State == CapabilityState.Killed)
            {
                return Task.FromResult(CapabilityResolutionResult.Denied(
                    $"Capability '{canonical}' is currently {entry.State} and cannot be resolved."));
            }

            if (request.RequiredTrustTier < entry.MinimumTrustRequired)
            {
                return Task.FromResult(CapabilityResolutionResult.Denied(
                    $"Requester trust tier ({request.RequiredTrustTier:F2}) is below required minimum ({entry.MinimumTrustRequired:F2}) for capability '{canonical}'."));
            }

            if (entry.MaxRiskTier > request.MaxPermittedRiskTier)
            {
                return Task.FromResult(CapabilityResolutionResult.Denied(
                    $"Capability risk tier (R{entry.MaxRiskTier}) exceeds requester permitted ceiling (R{request.MaxPermittedRiskTier})."));
            }

            return Task.FromResult(CapabilityResolutionResult.Resolved(entry.Id, entry.State, entry.IsSandboxed));
        }
    }
}
