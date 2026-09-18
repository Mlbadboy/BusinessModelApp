using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using BusinessModelApp.Core.Domain.Runtime.Organizational.Simulation;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational.Simulation;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational.Simulation;

public sealed class SimulationEngine : ISimulationEngine
{
    private readonly ISimulationScenarioManager _scenarioManager;
    private readonly ISimulationWorldBuilder _worldBuilder;
    private readonly ISimulationAgentRuntime _agentRuntime;
    private readonly ISimulationEnvironment _environment;
    private readonly ISimulationRunManager _runManager;
    private readonly ISimulationBudgetGuard _budgetGuard;
    private readonly SimulationProviderRegistry _providerRegistry;
    private readonly ISimulationTenantIsolation _tenantIsolation;

    public SimulationEngine(
        ISimulationScenarioManager scenarioManager,
        ISimulationWorldBuilder worldBuilder,
        ISimulationAgentRuntime agentRuntime,
        ISimulationEnvironment environment,
        ISimulationRunManager runManager,
        ISimulationBudgetGuard budgetGuard,
        SimulationProviderRegistry providerRegistry,
        ISimulationTenantIsolation tenantIsolation)
    {
        _scenarioManager = scenarioManager ?? throw new ArgumentNullException(nameof(scenarioManager));
        _worldBuilder = worldBuilder ?? throw new ArgumentNullException(nameof(worldBuilder));
        _agentRuntime = agentRuntime ?? throw new ArgumentNullException(nameof(agentRuntime));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        _runManager = runManager ?? throw new ArgumentNullException(nameof(runManager));
        _budgetGuard = budgetGuard ?? throw new ArgumentNullException(nameof(budgetGuard));
        _providerRegistry = providerRegistry ?? throw new ArgumentNullException(nameof(providerRegistry));
        _tenantIsolation = tenantIsolation ?? throw new ArgumentNullException(nameof(tenantIsolation));
    }

    public async Task<SimulationRunMetadata> StartRunAsync(
        string tenantId,
        string scenarioId,
        string? providerId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));
        if (string.IsNullOrWhiteSpace(scenarioId)) throw new ArgumentNullException(nameof(scenarioId));

        var scenario = await _scenarioManager.GetScenarioAsync(tenantId, scenarioId);
        if (scenario == null)
        {
            throw new KeyNotFoundException($"Scenario '{scenarioId}' not found for tenant '{tenantId}'.");
        }

        _tenantIsolation.AssertTenantAccess(tenantId, scenario.TenantId);

        string effectiveProviderId = providerId ?? "DeterministicSimulationEngine";
        var provider = _providerRegistry.GetProvider(effectiveProviderId)
            ?? throw new InvalidOperationException($"Simulation provider '{effectiveProviderId}' is not registered.");

        // Capture immutable world snapshot
        var snapshot = await _worldBuilder.CaptureWorldSnapshotAsync(tenantId, "SimulationProductionSnapshot", cancellationToken);

        var run = new SimulationRunMetadata
        {
            TenantId = tenantId,
            ScenarioId = scenario.ScenarioId,
            ScenarioName = scenario.Name,
            WorldSnapshotHash = snapshot.IntegrityHash,
            InputSnapshotHash = scenario.ScenarioHash,
            SimulationPolicyHash = scenario.Policy.PolicyHash,
            ProviderId = provider.ProviderId,
            RandomSeed = scenario.RandomSeed,
            State = SimulationLifecycleState.Draft
        };

        await _runManager.SaveRunMetadataAsync(run);

        // Lifecycle: Validating -> Admitted
        run.State = SimulationLifecycleState.Validating;
        _budgetGuard.ValidateBudget(scenario.Budget);

        run.State = SimulationLifecycleState.Admitted;
        run.State = SimulationLifecycleState.Allocated; // OARA mock/envelope check

        var sw = Stopwatch.StartNew();
        try
        {
            // Initializing: Synthesize population
            run.State = SimulationLifecycleState.Initializing;
            var agents = _agentRuntime.SynthesizePopulation(scenario, snapshot);

            if (!_budgetGuard.CheckRunWithinBudget(run, scenario.Budget, agents.Count, 0, sw.Elapsed.TotalSeconds))
            {
                await _runManager.SaveRunMetadataAsync(run);
                return run;
            }

            // Running: Step loop
            run.State = SimulationLifecycleState.Running;
            int totalSteps = Math.Min(scenario.TimeHorizonDays / Math.Max(1, scenario.TimeStepDays), scenario.Budget.MaxSteps);

            for (int s = 0; s < totalSteps; s++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!_budgetGuard.CheckRunWithinBudget(run, scenario.Budget, agents.Count, s + 1, sw.Elapsed.TotalSeconds))
                {
                    await _runManager.SaveRunMetadataAsync(run);
                    return run;
                }

                await _environment.ApplyEnvironmentEvolutionAsync(snapshot, scenario, s);
                await _agentRuntime.RunAgentStepAsync(s, agents, scenario);
            }

            // Provider execution: project final scenario outcomes
            var outcomes = await provider.ExecuteSimulationAsync(scenario, snapshot, agents, cancellationToken);
            foreach (var o in outcomes)
            {
                o.RunId = run.SimulationRunId;
                o.ScenarioId = scenario.ScenarioId;
            }

            await _runManager.SaveOutcomesAsync(tenantId, run.SimulationRunId, outcomes);

            // Compute output hash
            run.OutputHash = ComputeOutcomesHash(outcomes);
            run.State = SimulationLifecycleState.Completed;
            run.State = SimulationLifecycleState.Validated;
            run.CompletedAtUtc = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            run.State = SimulationLifecycleState.Failed;
            run.FailureReason = SimulationFailureReason.ProviderFault;
            run.FailureMessage = ex.Message;
        }
        finally
        {
            sw.Stop();
            await _runManager.SaveRunMetadataAsync(run);
        }

        return run;
    }

    public async Task<List<SimulationOutcome>> GetRunOutcomesAsync(
        string tenantId,
        string runId,
        CancellationToken cancellationToken = default)
    {
        var run = await _runManager.GetRunMetadataAsync(tenantId, runId);
        if (run == null)
        {
            throw new KeyNotFoundException($"Simulation run '{runId}' not found for tenant '{tenantId}'.");
        }

        _tenantIsolation.AssertTenantAccess(tenantId, run.TenantId);
        var outcomes = await _runManager.GetOutcomesAsync(tenantId, runId);
        return outcomes.ToList();
    }

    private static string ComputeOutcomesHash(IEnumerable<SimulationOutcome> outcomes)
    {
        var sb = new StringBuilder();
        foreach (var o in outcomes.OrderBy(x => x.MetricName))
        {
            sb.Append($"{o.MetricName}:{o.ProjectedValue:F4}:{o.Confidence:F4};");
        }
        using var sha = SHA256.Create();
        return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString())));
    }
}
