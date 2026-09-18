using System.Collections.Concurrent;
using BusinessModelApp.Core.Domain.Runtime.Organizational.Simulation;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational.Simulation;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational.Simulation;

public sealed class SimulationScenarioManager : ISimulationScenarioManager
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, SimulationScenario>> _scenariosByTenant = new();
    private readonly ConcurrentDictionary<string, List<ScenarioBranch>> _branchesByTenant = new();
    private readonly ISimulationTenantIsolation _tenantIsolation;
    private readonly ISimulationBudgetGuard _budgetGuard;

    public SimulationScenarioManager(
        ISimulationTenantIsolation tenantIsolation,
        ISimulationBudgetGuard budgetGuard)
    {
        _tenantIsolation = tenantIsolation ?? throw new ArgumentNullException(nameof(tenantIsolation));
        _budgetGuard = budgetGuard ?? throw new ArgumentNullException(nameof(budgetGuard));
    }

    public Task<SimulationScenario> RegisterScenarioAsync(SimulationScenario scenario)
    {
        if (scenario == null) throw new ArgumentNullException(nameof(scenario));
        if (string.IsNullOrWhiteSpace(scenario.TenantId)) throw new ArgumentNullException(nameof(scenario.TenantId));

        _budgetGuard.ValidateBudget(scenario.Budget);
        scenario.Policy.TenantId = scenario.TenantId;
        scenario.Policy.ComputeHash();
        scenario.ComputeScenarioHash();

        var tenantMap = _scenariosByTenant.GetOrAdd(scenario.TenantId, _ => new ConcurrentDictionary<string, SimulationScenario>());
        tenantMap[scenario.ScenarioId] = scenario;

        return Task.FromResult(scenario);
    }

    public Task<SimulationScenario?> GetScenarioAsync(string tenantId, string scenarioId)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));
        if (string.IsNullOrWhiteSpace(scenarioId)) throw new ArgumentNullException(nameof(scenarioId));

        if (_scenariosByTenant.TryGetValue(tenantId, out var tenantMap) &&
            tenantMap.TryGetValue(scenarioId, out var scenario))
        {
            _tenantIsolation.AssertTenantAccess(tenantId, scenario.TenantId);
            return Task.FromResult<SimulationScenario?>(scenario);
        }

        // Cross-tenant penetration defense (I34-R): if scenario exists under another tenant, fail immediately
        foreach (var (otherTenant, otherMap) in _scenariosByTenant)
        {
            if (otherMap.ContainsKey(scenarioId))
            {
                _tenantIsolation.AssertTenantAccess(tenantId, otherTenant);
            }
        }

        return Task.FromResult<SimulationScenario?>(null);
    }

    public Task<IReadOnlyList<SimulationScenario>> ListScenariosAsync(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));

        if (_scenariosByTenant.TryGetValue(tenantId, out var tenantMap))
        {
            return Task.FromResult<IReadOnlyList<SimulationScenario>>(tenantMap.Values.ToList());
        }

        return Task.FromResult<IReadOnlyList<SimulationScenario>>(Array.Empty<SimulationScenario>());
    }

    public async Task<ScenarioBranch> BranchScenarioAsync(string tenantId, string parentScenarioId, string branchHypothesis)
    {
        var parent = await GetScenarioAsync(tenantId, parentScenarioId);
        if (parent == null)
        {
            throw new KeyNotFoundException($"Parent scenario '{parentScenarioId}' not found for tenant '{tenantId}'.");
        }

        // Create branched child scenario with deep-copied properties
        var child = new SimulationScenario
        {
            TenantId = tenantId,
            Name = $"{parent.Name} [Branch: {branchHypothesis}]",
            Description = $"Hypothesis branch off {parent.ScenarioId}: {branchHypothesis}",
            ParentScenarioId = parent.ScenarioId,
            BaselineSnapshotHash = parent.BaselineSnapshotHash,
            AgentPopulationSize = parent.AgentPopulationSize,
            TimeHorizonDays = parent.TimeHorizonDays,
            TimeStepDays = parent.TimeStepDays,
            RandomSeed = parent.RandomSeed + 1,
            DecisionVariables = parent.DecisionVariables.Select(d => new DecisionVariable
            {
                Name = d.Name,
                OriginalValue = d.OriginalValue,
                SimulatedValue = d.SimulatedValue,
                Unit = d.Unit
            }).ToList(),
            EnvironmentRules = parent.EnvironmentRules.Select(r => new EnvironmentRule
            {
                RuleName = r.RuleName,
                RuleType = r.RuleType,
                SensitivityMultiplier = r.SensitivityMultiplier
            }).ToList()
        };

        var registeredChild = await RegisterScenarioAsync(child);

        var branch = new ScenarioBranch
        {
            ParentScenarioId = parentScenarioId,
            ChildScenarioId = registeredChild.ScenarioId,
            DivergenceStep = 0,
            BranchingHypothesis = branchHypothesis
        };

        var branches = _branchesByTenant.GetOrAdd(tenantId, _ => new List<ScenarioBranch>());
        lock (branches)
        {
            branches.Add(branch);
        }

        return branch;
    }
}
