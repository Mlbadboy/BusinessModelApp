using System.Collections.Concurrent;
using BusinessModelApp.Core.Domain.Runtime.Organizational.Learning;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational.Learning;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational.Learning;

public sealed class InMemoryLearningRepository : ILearningAuditRepository
{
    private readonly ConcurrentDictionary<string, List<EmpiricalOutcomeEvent>> _outcomesByTenant = new();
    private readonly ConcurrentDictionary<string, List<MetrologyRecord>> _metrologyByTenant = new();
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, OrganizationalLesson>> _lessonsByTenant = new();
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, AdaptationProposal>> _proposalsByTenant = new();
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, AdaptationHysteresisState>> _hysteresisByTenant = new();

    public Task SaveOutcomeAsync(EmpiricalOutcomeEvent outcome)
    {
        if (outcome == null) throw new ArgumentNullException(nameof(outcome));
        if (string.IsNullOrWhiteSpace(outcome.TenantId)) throw new ArgumentNullException(nameof(outcome.TenantId));

        var list = _outcomesByTenant.GetOrAdd(outcome.TenantId, _ => new List<EmpiricalOutcomeEvent>());
        lock (list)
        {
            list.Add(outcome);
        }
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<EmpiricalOutcomeEvent>> ListOutcomesAsync(string tenantId, string? domain = null)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));

        if (_outcomesByTenant.TryGetValue(tenantId, out var list))
        {
            lock (list)
            {
                var query = list.AsEnumerable();
                if (!string.IsNullOrWhiteSpace(domain))
                {
                    query = query.Where(o => string.Equals(o.SourceDomain, domain, StringComparison.OrdinalIgnoreCase));
                }
                return Task.FromResult<IReadOnlyList<EmpiricalOutcomeEvent>>(query.OrderByDescending(o => o.ObservedUtc).ToList());
            }
        }
        return Task.FromResult<IReadOnlyList<EmpiricalOutcomeEvent>>(Array.Empty<EmpiricalOutcomeEvent>());
    }

    public Task SaveMetrologyAsync(MetrologyRecord record)
    {
        if (record == null) throw new ArgumentNullException(nameof(record));
        var list = _metrologyByTenant.GetOrAdd(record.TenantId, _ => new List<MetrologyRecord>());
        lock (list)
        {
            list.Add(record);
        }
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<MetrologyRecord>> ListMetrologyAsync(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));
        if (_metrologyByTenant.TryGetValue(tenantId, out var list))
        {
            lock (list)
            {
                return Task.FromResult<IReadOnlyList<MetrologyRecord>>(list.OrderByDescending(m => m.CalculatedUtc).ToList());
            }
        }
        return Task.FromResult<IReadOnlyList<MetrologyRecord>>(Array.Empty<MetrologyRecord>());
    }

    public Task SaveLessonAsync(OrganizationalLesson lesson)
    {
        if (lesson == null) throw new ArgumentNullException(nameof(lesson));
        var map = _lessonsByTenant.GetOrAdd(lesson.TenantId, _ => new ConcurrentDictionary<string, OrganizationalLesson>());
        map[lesson.LessonId] = lesson;
        return Task.CompletedTask;
    }

    public Task<OrganizationalLesson?> GetLessonAsync(string tenantId, string lessonId)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));
        if (string.IsNullOrWhiteSpace(lessonId)) throw new ArgumentNullException(nameof(lessonId));

        if (_lessonsByTenant.TryGetValue(tenantId, out var map) && map.TryGetValue(lessonId, out var lesson))
        {
            return Task.FromResult<OrganizationalLesson?>(lesson);
        }
        // Multi-tenant penetration defense (I35-M): if exists in another tenant, throw UnauthorizedAccessException
        foreach (var (otherTenant, otherMap) in _lessonsByTenant)
        {
            if (otherMap.ContainsKey(lessonId))
            {
                throw new UnauthorizedAccessException($"Cross-tenant learning access violation: Tenant '{tenantId}' cannot access lessons belonging to '{otherTenant}'. (I35-M)");
            }
        }
        return Task.FromResult<OrganizationalLesson?>(null);
    }

    public Task<IReadOnlyList<OrganizationalLesson>> ListLessonsAsync(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));
        if (_lessonsByTenant.TryGetValue(tenantId, out var map))
        {
            return Task.FromResult<IReadOnlyList<OrganizationalLesson>>(map.Values.OrderByDescending(l => l.DistilledUtc).ToList());
        }
        return Task.FromResult<IReadOnlyList<OrganizationalLesson>>(Array.Empty<OrganizationalLesson>());
    }

    public Task SaveProposalAsync(AdaptationProposal proposal)
    {
        if (proposal == null) throw new ArgumentNullException(nameof(proposal));
        var map = _proposalsByTenant.GetOrAdd(proposal.TenantId, _ => new ConcurrentDictionary<string, AdaptationProposal>());
        map[proposal.ProposalId] = proposal;
        return Task.CompletedTask;
    }

    public Task<AdaptationProposal?> GetProposalAsync(string tenantId, string proposalId)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));
        if (string.IsNullOrWhiteSpace(proposalId)) throw new ArgumentNullException(nameof(proposalId));

        if (_proposalsByTenant.TryGetValue(tenantId, out var map) && map.TryGetValue(proposalId, out var prop))
        {
            return Task.FromResult<AdaptationProposal?>(prop);
        }
        // Multi-tenant penetration defense (I35-M)
        foreach (var (otherTenant, otherMap) in _proposalsByTenant)
        {
            if (otherMap.ContainsKey(proposalId))
            {
                throw new UnauthorizedAccessException($"Cross-tenant adaptation access violation: Tenant '{tenantId}' cannot access proposals belonging to '{otherTenant}'. (I35-M)");
            }
        }
        return Task.FromResult<AdaptationProposal?>(null);
    }

    public Task<IReadOnlyList<AdaptationProposal>> ListProposalsAsync(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));
        if (_proposalsByTenant.TryGetValue(tenantId, out var map))
        {
            return Task.FromResult<IReadOnlyList<AdaptationProposal>>(map.Values.OrderByDescending(p => p.ProposedUtc).ToList());
        }
        return Task.FromResult<IReadOnlyList<AdaptationProposal>>(Array.Empty<AdaptationProposal>());
    }

    public AdaptationHysteresisState GetOrCreateHysteresisState(string tenantId, string parameterKey)
    {
        var map = _hysteresisByTenant.GetOrAdd(tenantId, _ => new ConcurrentDictionary<string, AdaptationHysteresisState>());
        return map.GetOrAdd(parameterKey, k => new AdaptationHysteresisState { ParameterKey = k });
    }

    public void UpdateHysteresisState(string tenantId, string parameterKey, double value, DateTime timestamp)
    {
        var state = GetOrCreateHysteresisState(tenantId, parameterKey);
        state.LastAdaptedValue = value;
        state.LastAdaptedUtc = timestamp;
        state.AdaptationCountInEpoch++;
    }
}
