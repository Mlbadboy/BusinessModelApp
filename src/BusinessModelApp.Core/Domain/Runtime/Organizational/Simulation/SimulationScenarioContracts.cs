using System.Security.Cryptography;
using System.Text;

namespace BusinessModelApp.Core.Domain.Runtime.Organizational.Simulation;

public sealed class DecisionVariable
{
    public string Name { get; set; } = string.Empty;
    public double OriginalValue { get; set; }
    public double SimulatedValue { get; set; }
    public string Unit { get; set; } = "%";
}

public sealed class InitialConditions
{
    public string BaselineSnapshotHash { get; set; } = string.Empty;
    public Dictionary<string, double> NumericParameters { get; set; } = new();
    public Dictionary<string, string> CategoricalParameters { get; set; } = new();
}

public sealed class EnvironmentRule
{
    public string RuleId { get; set; } = Guid.NewGuid().ToString("N");
    public string RuleName { get; set; } = string.Empty;
    public string RuleType { get; set; } = "DemandResponse"; // PriceElasticity, CompetitorReaction, ChurnThreshold
    public double SensitivityMultiplier { get; set; } = 1.0;
}

/// <summary>
/// Definition of a simulation scenario. Immutable once admitted.
/// </summary>
public sealed class SimulationScenario
{
    public string ScenarioId { get; set; } = Guid.NewGuid().ToString("N");
    public string TenantId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ParentScenarioId { get; set; }

    public string BaselineSnapshotHash { get; set; } = string.Empty;
    public string ScenarioHash { get; set; } = string.Empty;

    public InitialConditions InitialConditions { get; set; } = new();
    public List<DecisionVariable> DecisionVariables { get; set; } = new();
    public List<EnvironmentRule> EnvironmentRules { get; set; } = new();

    public int AgentPopulationSize { get; set; } = 100;
    public int TimeHorizonDays { get; set; } = 90;
    public int TimeStepDays { get; set; } = 7;
    public int RandomSeed { get; set; } = 42;

    public SimulationBudget Budget { get; set; } = new();
    public SimulationPolicy Policy { get; set; } = new();
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public void ComputeScenarioHash()
    {
        var sb = new StringBuilder();
        sb.Append($"{TenantId}:{Name}:{BaselineSnapshotHash}:{ParentScenarioId}:");
        sb.Append($"{AgentPopulationSize}:{TimeHorizonDays}:{TimeStepDays}:{RandomSeed}:");
        foreach (var v in DecisionVariables.OrderBy(d => d.Name))
        {
            sb.Append($"{v.Name}={v.OriginalValue}->{v.SimulatedValue};");
        }
        foreach (var r in EnvironmentRules.OrderBy(e => e.RuleName))
        {
            sb.Append($"{r.RuleName}:{r.SensitivityMultiplier};");
        }
        using var sha = SHA256.Create();
        ScenarioHash = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString())));
    }
}

/// <summary>
/// Branch record for counterfactual scenario exploration.
/// </summary>
public sealed class ScenarioBranch
{
    public string BranchId { get; set; } = Guid.NewGuid().ToString("N");
    public string ParentScenarioId { get; set; } = string.Empty;
    public string ChildScenarioId { get; set; } = string.Empty;
    public int DivergenceStep { get; set; }
    public string BranchingHypothesis { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
