using BusinessModelApp.Core.Domain.Runtime.Organizational.Simulation;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational.Simulation;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational.Simulation;

public sealed class OrganizationalSimulationService : IOrganizationalSimulationService
{
    private readonly ISimulationScenarioManager _scenarioManager;
    private readonly ISimulationEngine _simulationEngine;
    private readonly ISimulationRunManager _runManager;
    private readonly ISimulationProvenanceService _provenanceService;
    private readonly ISimulationCalibrationService _calibrationService;
    private readonly ISimulationWorldBuilder _worldBuilder;
    private readonly SimulationProviderRegistry _providerRegistry;
    private readonly ISimulationTenantIsolation _tenantIsolation;

    public OrganizationalSimulationService(
        ISimulationScenarioManager scenarioManager,
        ISimulationEngine simulationEngine,
        ISimulationRunManager runManager,
        ISimulationProvenanceService provenanceService,
        ISimulationCalibrationService calibrationService,
        ISimulationWorldBuilder worldBuilder,
        SimulationProviderRegistry providerRegistry,
        ISimulationTenantIsolation tenantIsolation)
    {
        _scenarioManager = scenarioManager ?? throw new ArgumentNullException(nameof(scenarioManager));
        _simulationEngine = simulationEngine ?? throw new ArgumentNullException(nameof(simulationEngine));
        _runManager = runManager ?? throw new ArgumentNullException(nameof(runManager));
        _provenanceService = provenanceService ?? throw new ArgumentNullException(nameof(provenanceService));
        _calibrationService = calibrationService ?? throw new ArgumentNullException(nameof(calibrationService));
        _worldBuilder = worldBuilder ?? throw new ArgumentNullException(nameof(worldBuilder));
        _providerRegistry = providerRegistry ?? throw new ArgumentNullException(nameof(providerRegistry));
        _tenantIsolation = tenantIsolation ?? throw new ArgumentNullException(nameof(tenantIsolation));
    }

    public Task<SimulationScenario> CreateScenarioAsync(string tenantId, SimulationScenario scenario)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));
        if (scenario == null) throw new ArgumentNullException(nameof(scenario));

        scenario.TenantId = tenantId;
        return _scenarioManager.RegisterScenarioAsync(scenario);
    }

    public Task<SimulationScenario?> GetScenarioAsync(string tenantId, string scenarioId)
    {
        return _scenarioManager.GetScenarioAsync(tenantId, scenarioId);
    }

    public Task<IReadOnlyList<SimulationScenario>> ListScenariosAsync(string tenantId)
    {
        return _scenarioManager.ListScenariosAsync(tenantId);
    }

    public Task<ScenarioBranch> BranchScenarioAsync(string tenantId, string parentScenarioId, string hypothesis)
    {
        return _scenarioManager.BranchScenarioAsync(tenantId, parentScenarioId, hypothesis);
    }

    public Task<SimulationRunMetadata> LaunchSimulationRunAsync(string tenantId, string scenarioId, string? providerId = null)
    {
        return _simulationEngine.StartRunAsync(tenantId, scenarioId, providerId);
    }

    public Task<SimulationRunMetadata?> GetRunStatusAsync(string tenantId, string runId)
    {
        return _runManager.GetRunMetadataAsync(tenantId, runId);
    }

    public Task<IReadOnlyList<SimulationOutcome>> GetRunOutcomesAsync(string tenantId, string runId)
    {
        return _runManager.GetOutcomesAsync(tenantId, runId);
    }

    public async Task<SimulationProvenanceTrace> GetRunProvenanceAsync(string tenantId, string runId)
    {
        var run = await _runManager.GetRunMetadataAsync(tenantId, runId);
        if (run == null)
            throw new KeyNotFoundException($"Simulation run '{runId}' not found for tenant '{tenantId}'.");

        var scenario = await _scenarioManager.GetScenarioAsync(tenantId, run.ScenarioId);
        if (scenario == null)
            throw new KeyNotFoundException($"Scenario '{run.ScenarioId}' not found for run '{runId}'.");

        var outcomes = await _runManager.GetOutcomesAsync(tenantId, runId);
        var snapshot = await _worldBuilder.CaptureWorldSnapshotAsync(tenantId);

        return await _provenanceService.GenerateTraceAsync(run, scenario, snapshot, outcomes);
    }

    public async Task<SimulationComparisonResult> CompareScenariosAsync(string tenantId, IEnumerable<string> scenarioIds)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));
        if (scenarioIds == null) throw new ArgumentNullException(nameof(scenarioIds));

        var ids = scenarioIds.Distinct().ToList();
        var result = new SimulationComparisonResult
        {
            TenantId = tenantId,
            ComparedScenarioIds = ids
        };

        foreach (var id in ids)
        {
            var scenario = await _scenarioManager.GetScenarioAsync(tenantId, id);
            if (scenario == null) continue;

            var run = await _simulationEngine.StartRunAsync(tenantId, id);
            var outcomes = await _runManager.GetOutcomesAsync(tenantId, run.SimulationRunId);

            result.OutcomesByScenario[id] = outcomes.ToList();

            var revenueOutcome = outcomes.FirstOrDefault(o => o.MetricName == "ProjectedAnnualRevenue");
            double yieldScore = revenueOutcome != null ? revenueOutcome.ProjectedValue / 5_000_000.0 : 1.0;
            result.ComparativeYieldScores[id] = Math.Round(yieldScore, 3);
        }

        result.TradeoffAnalysis = $"Compared {ids.Count} scenarios. Counterfactual projections carry TruthClassification = Simulation. Does not constitute an execution decision.";
        return result;
    }

    public async Task<SimulationPerformanceRecord> CalibrateOutcomeAsync(
        string tenantId,
        string scenarioType,
        double predicted,
        double actual)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));

        var record = new SimulationPerformanceRecord
        {
            TenantId = tenantId,
            ScenarioType = scenarioType,
            PredictedMetricValue = predicted,
            ActualMetricValue = actual
        };

        await _calibrationService.RecordCalibrationAsync(record);
        return record;
    }

    public Task<IReadOnlyList<ISimulationProvider>> ListProvidersAsync()
    {
        return Task.FromResult(_providerRegistry.ListProviders());
    }
}
