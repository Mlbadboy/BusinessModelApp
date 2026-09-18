using System.Collections.Concurrent;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational.Simulation;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational.Simulation;

public sealed class SimulationProviderRegistry
{
    private readonly ConcurrentDictionary<string, ISimulationProvider> _providers = new();

    public SimulationProviderRegistry(IEnumerable<ISimulationProvider> providers)
    {
        if (providers != null)
        {
            foreach (var p in providers)
            {
                _providers[p.ProviderId] = p;
            }
        }
    }

    public void RegisterProvider(ISimulationProvider provider)
    {
        if (provider == null) throw new ArgumentNullException(nameof(provider));
        _providers[provider.ProviderId] = provider;
    }

    public ISimulationProvider? GetProvider(string providerId)
    {
        if (_providers.TryGetValue(providerId, out var provider))
        {
            return provider;
        }
        return null;
    }

    public IReadOnlyList<ISimulationProvider> ListProviders()
    {
        return _providers.Values.ToList();
    }
}
