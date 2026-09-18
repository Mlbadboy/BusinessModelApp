using System.Collections.Concurrent;
using BusinessModelApp.Core.Domain.Runtime.Organizational.Readiness;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational.Readiness;

/// <summary>
/// Thread-safe multi-tenant in-memory store for POR scenarios, assessments, debt, and contingencies.
/// </summary>
public class InMemoryPredictiveReadinessStore : IPredictiveReadinessStore
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, ReadinessScenario>> _scenarios = new();
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, OrganizationalReadinessAssessment>> _assessments = new();
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, CapacityStressTestResult>> _stressResults = new();
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, ReadinessDebtRecord>> _debtRecords = new();
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, ContingencyProposal>> _contingencies = new();

    public Task SaveScenarioAsync(string tenantId, ReadinessScenario scenario)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));
        if (scenario == null) throw new ArgumentNullException(nameof(scenario));

        var tenantStore = _scenarios.GetOrAdd(tenantId, _ => new ConcurrentDictionary<string, ReadinessScenario>());
        tenantStore[scenario.ScenarioId] = scenario;
        return Task.CompletedTask;
    }

    public Task<ReadinessScenario?> GetScenarioAsync(string tenantId, string scenarioId)
    {
        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(scenarioId))
            return Task.FromResult<ReadinessScenario?>(null);

        if (_scenarios.TryGetValue(tenantId, out var tenantStore) && tenantStore.TryGetValue(scenarioId, out var scenario))
        {
            return Task.FromResult<ReadinessScenario?>(scenario);
        }
        return Task.FromResult<ReadinessScenario?>(null);
    }

    public Task<IReadOnlyList<ReadinessScenario>> ListScenariosAsync(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId) || !_scenarios.TryGetValue(tenantId, out var tenantStore))
        {
            return Task.FromResult<IReadOnlyList<ReadinessScenario>>(Array.Empty<ReadinessScenario>());
        }
        return Task.FromResult<IReadOnlyList<ReadinessScenario>>(tenantStore.Values.ToList());
    }

    public Task SaveAssessmentAsync(string tenantId, OrganizationalReadinessAssessment assessment)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));
        if (assessment == null) throw new ArgumentNullException(nameof(assessment));

        var tenantStore = _assessments.GetOrAdd(tenantId, _ => new ConcurrentDictionary<string, OrganizationalReadinessAssessment>());
        tenantStore[assessment.AssessmentId] = assessment;
        return Task.CompletedTask;
    }

    public Task<OrganizationalReadinessAssessment?> GetAssessmentAsync(string tenantId, string assessmentId)
    {
        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(assessmentId))
            return Task.FromResult<OrganizationalReadinessAssessment?>(null);

        if (_assessments.TryGetValue(tenantId, out var tenantStore) && tenantStore.TryGetValue(assessmentId, out var assessment))
        {
            return Task.FromResult<OrganizationalReadinessAssessment?>(assessment);
        }
        return Task.FromResult<OrganizationalReadinessAssessment?>(null);
    }

    public Task<IReadOnlyList<OrganizationalReadinessAssessment>> ListAssessmentsAsync(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId) || !_assessments.TryGetValue(tenantId, out var tenantStore))
        {
            return Task.FromResult<IReadOnlyList<OrganizationalReadinessAssessment>>(Array.Empty<OrganizationalReadinessAssessment>());
        }
        return Task.FromResult<IReadOnlyList<OrganizationalReadinessAssessment>>(tenantStore.Values.ToList());
    }

    public Task SaveStressTestResultAsync(string tenantId, CapacityStressTestResult result)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));
        if (result == null) throw new ArgumentNullException(nameof(result));

        var tenantStore = _stressResults.GetOrAdd(tenantId, _ => new ConcurrentDictionary<string, CapacityStressTestResult>());
        tenantStore[result.TestId] = result;
        return Task.CompletedTask;
    }

    public Task<CapacityStressTestResult?> GetStressTestResultAsync(string tenantId, string testId)
    {
        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(testId))
            return Task.FromResult<CapacityStressTestResult?>(null);

        if (_stressResults.TryGetValue(tenantId, out var tenantStore) && tenantStore.TryGetValue(testId, out var result))
        {
            return Task.FromResult<CapacityStressTestResult?>(result);
        }
        return Task.FromResult<CapacityStressTestResult?>(null);
    }

    public Task SaveDebtRecordAsync(string tenantId, ReadinessDebtRecord debt)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));
        if (debt == null) throw new ArgumentNullException(nameof(debt));

        var tenantStore = _debtRecords.GetOrAdd(tenantId, _ => new ConcurrentDictionary<string, ReadinessDebtRecord>());
        tenantStore[debt.DebtId] = debt;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ReadinessDebtRecord>> ListDebtRecordsAsync(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId) || !_debtRecords.TryGetValue(tenantId, out var tenantStore))
        {
            return Task.FromResult<IReadOnlyList<ReadinessDebtRecord>>(Array.Empty<ReadinessDebtRecord>());
        }
        return Task.FromResult<IReadOnlyList<ReadinessDebtRecord>>(tenantStore.Values.ToList());
    }

    public Task SaveContingencyAsync(string tenantId, ContingencyProposal contingency)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));
        if (contingency == null) throw new ArgumentNullException(nameof(contingency));

        var tenantStore = _contingencies.GetOrAdd(tenantId, _ => new ConcurrentDictionary<string, ContingencyProposal>());
        tenantStore[contingency.ContingencyId] = contingency;
        return Task.CompletedTask;
    }

    public Task<ContingencyProposal?> GetContingencyAsync(string tenantId, string contingencyId)
    {
        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(contingencyId))
            return Task.FromResult<ContingencyProposal?>(null);

        if (_contingencies.TryGetValue(tenantId, out var tenantStore) && tenantStore.TryGetValue(contingencyId, out var c))
        {
            return Task.FromResult<ContingencyProposal?>(c);
        }
        return Task.FromResult<ContingencyProposal?>(null);
    }

    public Task<IReadOnlyList<ContingencyProposal>> ListContingenciesAsync(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId) || !_contingencies.TryGetValue(tenantId, out var tenantStore))
        {
            return Task.FromResult<IReadOnlyList<ContingencyProposal>>(Array.Empty<ContingencyProposal>());
        }
        return Task.FromResult<IReadOnlyList<ContingencyProposal>>(tenantStore.Values.ToList());
    }
}
