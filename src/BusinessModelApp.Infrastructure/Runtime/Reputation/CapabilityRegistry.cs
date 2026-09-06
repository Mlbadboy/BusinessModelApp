using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime;
using BusinessModelApp.Core.Domain.Runtime.Capabilities;
using BusinessModelApp.Core.Domain.Runtime.Reputation;
using BusinessModelApp.Core.Interfaces.Runtime.Reputation;

namespace BusinessModelApp.Infrastructure.Runtime.Reputation
{
    public class CapabilityRegistry : ICapabilityRegistry
    {
        private readonly ConcurrentDictionary<string, CapabilityDefinitionRecord> _capabilities = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, AgentCapabilityBinding> _bindings = new(StringComparer.OrdinalIgnoreCase);

        public Task RegisterCapabilityAsync(CapabilityDefinitionRecord capability, CancellationToken ct = default)
        {
            if (capability == null) throw new ArgumentNullException(nameof(capability));
            _capabilities[capability.CapabilityId.ToString()] = capability;
            return Task.CompletedTask;
        }

        public Task<CapabilityDefinitionRecord?> GetCapabilityAsync(CapabilityId capabilityId, CancellationToken ct = default)
        {
            _capabilities.TryGetValue(capabilityId.ToString(), out var capability);
            return Task.FromResult(capability);
        }

        public Task<IReadOnlyList<CapabilityDefinitionRecord>> ListCapabilitiesAsync(CancellationToken ct = default)
        {
            var list = _capabilities.Values.Where(c => c.IsActive).ToList();
            return Task.FromResult<IReadOnlyList<CapabilityDefinitionRecord>>(list);
        }

        public Task BindAgentCapabilityAsync(AgentCapabilityBinding binding, CancellationToken ct = default)
        {
            if (binding == null) throw new ArgumentNullException(nameof(binding));
            var key = $"{binding.AgentDefinitionId.Value}:{binding.CapabilityId}";
            _bindings[key] = binding;
            return Task.CompletedTask;
        }

        public Task<bool> IsAgentBoundToCapabilityAsync(AgentDefinitionId agentId, CapabilityId capabilityId, CancellationToken ct = default)
        {
            var key = $"{agentId.Value}:{capabilityId}";
            if (_bindings.TryGetValue(key, out var binding))
            {
                return Task.FromResult(binding.IsEnabled);
            }
            return Task.FromResult(false);
        }

        public Task<IReadOnlyList<CapabilityId>> GetCapabilitiesForAgentAsync(AgentDefinitionId agentId, CancellationToken ct = default)
        {
            var prefix = $"{agentId.Value}:";
            var capabilities = _bindings.Values
                .Where(b => b.AgentDefinitionId == agentId && b.IsEnabled)
                .Select(b => b.CapabilityId)
                .ToList();
            return Task.FromResult<IReadOnlyList<CapabilityId>>(capabilities);
        }
    }
}
