using System.Collections.Concurrent;
using BusinessModelApp.Core.Domain.Runtime.Organizational.Portfolio;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational.Portfolio;

/// <summary>
/// Thread-safe multi-tenant portfolio repository with versioned snapshots.
/// Invariant I33-L: Cross-tenant portfolio isolation.
/// Invariant I33-N: Versioned snapshots with deterministic SHA-256 hashes.
/// </summary>
public class InMemoryPortfolioRepository : IPortfolioRepository
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<int, OrganizationalPortfolio>> _store = new();

    public Task SavePortfolioAsync(string tenantId, OrganizationalPortfolio portfolio)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));
        if (portfolio == null) throw new ArgumentNullException(nameof(portfolio));

        portfolio.TenantId = tenantId;
        portfolio.ComputeDecisionHash();

        var tenantPortfolios = _store.GetOrAdd(tenantId, _ => new ConcurrentDictionary<int, OrganizationalPortfolio>());
        tenantPortfolios[portfolio.VersionNumber] = portfolio;

        return Task.CompletedTask;
    }

    public Task<OrganizationalPortfolio?> GetActivePortfolioAsync(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId) || !_store.TryGetValue(tenantId, out var tenantPortfolios))
        {
            return Task.FromResult<OrganizationalPortfolio?>(null);
        }

        var latest = tenantPortfolios.Values
            .OrderByDescending(p => p.VersionNumber)
            .FirstOrDefault();

        return Task.FromResult(latest);
    }

    public Task<OrganizationalPortfolio?> GetPortfolioVersionAsync(string tenantId, int versionNumber)
    {
        if (string.IsNullOrWhiteSpace(tenantId) || !_store.TryGetValue(tenantId, out var tenantPortfolios))
        {
            return Task.FromResult<OrganizationalPortfolio?>(null);
        }

        tenantPortfolios.TryGetValue(versionNumber, out var portfolio);
        return Task.FromResult(portfolio);
    }

    public Task<IReadOnlyList<OrganizationalPortfolio>> ListPortfolioHistoryAsync(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId) || !_store.TryGetValue(tenantId, out var tenantPortfolios))
        {
            return Task.FromResult<IReadOnlyList<OrganizationalPortfolio>>(Array.Empty<OrganizationalPortfolio>());
        }

        var list = tenantPortfolios.Values
            .OrderByDescending(p => p.VersionNumber)
            .ToList();

        return Task.FromResult<IReadOnlyList<OrganizationalPortfolio>>(list);
    }
}
