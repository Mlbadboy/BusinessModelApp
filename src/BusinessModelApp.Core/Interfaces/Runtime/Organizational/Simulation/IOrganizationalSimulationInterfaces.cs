using BusinessModelApp.Core.Domain.Runtime.Organizational.Simulation;

namespace BusinessModelApp.Core.Interfaces.Runtime.Organizational.Simulation;

/// <summary>
/// Pluggable external or internal simulation compute provider (I34-V).
/// Supplies compute/execution capacity; never holds organizational authority.
/// </summary>
public interface ISimulationProvider
{
    string ProviderId { get; }
    string ProviderName { get; }
    string ProviderVersion { get; }
    bool SupportsMultiAgent { get; }

    Task<List<SimulationOutcome>> ExecuteSimulationAsync(
        SimulationScenario scenario,
        SimulationWorldSnapshot snapshot,
        IReadOnlyList<SimulationAgent> agents,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Specialized provider interface for multi-agent emergent behavior simulation (e.g. MiroFish/OASIS adapter).
/// </summary>
public interface IMultiAgentSimulationProvider : ISimulationProvider
{
    Task<List<AgentInteractionEvent>> SimulateAgentInteractionsAsync(
        SimulationScenario scenario,
        IReadOnlyList<SimulationAgent> agents,
        int steps,
        CancellationToken cancellationToken = default);
}

public interface ISimulationEngine
{
    Task<SimulationRunMetadata> StartRunAsync(
        string tenantId,
        string scenarioId,
        string? providerId = null,
        CancellationToken cancellationToken = default);

    Task<List<SimulationOutcome>> GetRunOutcomesAsync(
        string tenantId,
        string runId,
        CancellationToken cancellationToken = default);
}

public interface ISimulationWorldBuilder
{
    Task<SimulationWorldSnapshot> CaptureWorldSnapshotAsync(
        string tenantId,
        string source = "ProductionStateSnapshot",
        CancellationToken cancellationToken = default);

    Task<bool> VerifySnapshotIntegrityAsync(SimulationWorldSnapshot snapshot);
}

public interface ISimulationAgentRuntime
{
    List<SimulationAgent> SynthesizePopulation(
        SimulationScenario scenario,
        SimulationWorldSnapshot snapshot);

    Task<List<AgentInteractionEvent>> RunAgentStepAsync(
        int stepIndex,
        List<SimulationAgent> agents,
        SimulationScenario scenario);
}

public interface ISimulationEnvironment
{
    Task ApplyEnvironmentEvolutionAsync(
        SimulationWorldSnapshot snapshot,
        SimulationScenario scenario,
        int stepIndex);
}

public interface ISimulationScenarioManager
{
    Task<SimulationScenario> RegisterScenarioAsync(SimulationScenario scenario);
    Task<SimulationScenario?> GetScenarioAsync(string tenantId, string scenarioId);
    Task<IReadOnlyList<SimulationScenario>> ListScenariosAsync(string tenantId);
    Task<ScenarioBranch> BranchScenarioAsync(string tenantId, string parentScenarioId, string branchHypothesis);
}

public interface ISimulationRunManager
{
    Task SaveRunMetadataAsync(SimulationRunMetadata metadata);
    Task<SimulationRunMetadata?> GetRunMetadataAsync(string tenantId, string runId);
    Task<IReadOnlyList<SimulationRunMetadata>> ListRunsAsync(string tenantId);
    Task SaveOutcomesAsync(string tenantId, string runId, IEnumerable<SimulationOutcome> outcomes);
    Task<IReadOnlyList<SimulationOutcome>> GetOutcomesAsync(string tenantId, string runId);
}

public interface ISimulationBudgetGuard
{
    void ValidateBudget(SimulationBudget budget);
    bool CheckRunWithinBudget(SimulationRunMetadata run, SimulationBudget budget, int currentAgents, int currentSteps, double elapsedSeconds);
}

public interface ISimulationCalibrationService
{
    Task RecordCalibrationAsync(SimulationPerformanceRecord record);
    Task<IReadOnlyList<SimulationPerformanceRecord>> GetCalibrationHistoryAsync(string tenantId, string? domain = null);
    double ComputeHistoricalReliability(IEnumerable<SimulationPerformanceRecord> records);
}

public interface ISimulationProvenanceService
{
    Task<SimulationProvenanceTrace> GenerateTraceAsync(
        SimulationRunMetadata run,
        SimulationScenario scenario,
        SimulationWorldSnapshot snapshot,
        IReadOnlyList<SimulationOutcome> outcomes);
}

public interface ISimulationSecurityGuard
{
    string SanitizeAgentPayload(string rawPayload);
    bool ValidateNoExecutionPermitRequested(string payload);
    bool ValidateNoProductionCredentialExposed(SimulationAgent agent);
}

public interface ISimulationTenantIsolation
{
    void AssertTenantAccess(string requestingTenantId, string resourceTenantId);
}

/// <summary>
/// Unified facade coordinating all simulation operations for Charlie.
/// </summary>
public interface IOrganizationalSimulationService
{
    Task<SimulationScenario> CreateScenarioAsync(string tenantId, SimulationScenario scenario);
    Task<SimulationScenario?> GetScenarioAsync(string tenantId, string scenarioId);
    Task<IReadOnlyList<SimulationScenario>> ListScenariosAsync(string tenantId);
    Task<ScenarioBranch> BranchScenarioAsync(string tenantId, string parentScenarioId, string hypothesis);

    Task<SimulationRunMetadata> LaunchSimulationRunAsync(string tenantId, string scenarioId, string? providerId = null);
    Task<SimulationRunMetadata?> GetRunStatusAsync(string tenantId, string runId);
    Task<IReadOnlyList<SimulationOutcome>> GetRunOutcomesAsync(string tenantId, string runId);
    Task<SimulationProvenanceTrace> GetRunProvenanceAsync(string tenantId, string runId);

    Task<SimulationComparisonResult> CompareScenariosAsync(string tenantId, IEnumerable<string> scenarioIds);
    Task<SimulationPerformanceRecord> CalibrateOutcomeAsync(string tenantId, string scenarioType, double predicted, double actual);
    Task<IReadOnlyList<ISimulationProvider>> ListProvidersAsync();
}
