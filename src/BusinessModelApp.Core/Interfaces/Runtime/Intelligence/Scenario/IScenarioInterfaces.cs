using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Intelligence.Scenario;

namespace BusinessModelApp.Core.Interfaces.Runtime.Intelligence.Scenario
{
    public sealed record ScenarioConstraintCheckResult(
        ScenarioConstraintStatus Status,
        IReadOnlyList<string> ViolatedConstraintNames,
        IReadOnlyList<string> SuggestedMitigations);

    public interface IScenarioSimulator
    {
        Task<ScenarioOutcome> SimulateAsync(
            ScenarioDefinition scenario,
            SimulationInputSnapshot inputSnapshot,
            ModelSnapshot modelSnapshot,
            PolicySnapshot policySnapshot,
            ConstraintSnapshot constraintSnapshot,
            CancellationToken ct = default);
    }

    public interface ICounterfactualEngine
    {
        Task<ScenarioOutcome> EvaluateCounterfactualAsync(
            string tenantId,
            CounterfactualIntervention intervention,
            SimulationInputSnapshot inputSnapshot,
            CancellationToken ct = default);
    }

    public interface IScenarioConstraintChecker
    {
        Task<ScenarioConstraintCheckResult> CheckConstraintsAsync(
            string tenantId,
            IReadOnlyDictionary<string, ScenarioOutcomeMetric> metrics,
            IReadOnlyList<ScenarioConstraint> constraints,
            CancellationToken ct = default);
    }

    public interface IScenarioComparisonEngine
    {
        Task<ScenarioComparisonResult> CompareScenariosAsync(
            string tenantId,
            IReadOnlyList<ScenarioOutcome> outcomes,
            PolicySnapshot policySnapshot,
            CancellationToken ct = default);
    }

    public interface IScenarioStore
    {
        Task SaveDefinitionAsync(ScenarioDefinition scenario, CancellationToken ct = default);
        Task<ScenarioDefinition?> GetDefinitionAsync(string tenantId, string scenarioId, CancellationToken ct = default);
        Task SaveOutcomeAsync(ScenarioOutcome outcome, CancellationToken ct = default);
        Task<ScenarioOutcome?> GetOutcomeAsync(string tenantId, string scenarioId, CancellationToken ct = default);
        Task<IReadOnlyList<ScenarioOutcome>> GetOutcomesForSignalAsync(string tenantId, string radarSignalId, CancellationToken ct = default);
        Task<IReadOnlyList<ScenarioDefinition>> ListDefinitionsAsync(string tenantId, ScenarioLifecycleState? state = null, CancellationToken ct = default);
        Task<bool> UpdateLifecycleStateAsync(string tenantId, string scenarioId, ScenarioLifecycleState newState, string reason, CancellationToken ct = default);
        Task SaveProvenanceAsync(ScenarioProvenance provenance, CancellationToken ct = default);
        Task<ScenarioProvenance?> GetProvenanceAsync(string tenantId, string scenarioId, CancellationToken ct = default);
    }

    public interface IScenarioOrchestrator
    {
        Task<ScenarioOutcome> RunScenarioPipelineAsync(ScenarioDefinition scenario, CancellationToken ct = default);
        Task<ScenarioComparisonResult> RunComparisonPipelineAsync(string tenantId, IReadOnlyList<string> scenarioIds, CancellationToken ct = default);
        Task<ScenarioDefinition> GenerateScenarioFromRadarSignalAsync(string tenantId, string radarSignalId, ScenarioType type, CancellationToken ct = default);
    }
}
