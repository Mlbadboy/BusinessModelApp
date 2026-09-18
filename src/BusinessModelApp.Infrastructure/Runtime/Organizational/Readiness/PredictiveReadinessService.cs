using BusinessModelApp.Core.Domain.Runtime.Organizational;
using BusinessModelApp.Core.Domain.Runtime.Organizational.Readiness;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational.Readiness;

/// <summary>
/// Unified Predictive Organizational Readiness (POR) Service.
/// Orchestrates stress testing, 8-dimensional scoring, "Prepare Now vs Wait" posture, debt tracking, and contingency staging.
/// </summary>
public class PredictiveReadinessService : IPredictiveReadinessService
{
    private readonly IPredictiveReadinessStore _store;
    private readonly ICapacityStressTestEngine _stressTestEngine;
    private readonly IReadinessScoringEngine _scoringEngine;
    private readonly IReadinessDebtTracker _debtTracker;
    private readonly IContingencyPlanningEngine _contingencyEngine;

    public PredictiveReadinessService(
        IPredictiveReadinessStore store,
        ICapacityStressTestEngine stressTestEngine,
        IReadinessScoringEngine scoringEngine,
        IReadinessDebtTracker debtTracker,
        IContingencyPlanningEngine contingencyEngine)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _stressTestEngine = stressTestEngine ?? throw new ArgumentNullException(nameof(stressTestEngine));
        _scoringEngine = scoringEngine ?? throw new ArgumentNullException(nameof(scoringEngine));
        _debtTracker = debtTracker ?? throw new ArgumentNullException(nameof(debtTracker));
        _contingencyEngine = contingencyEngine ?? throw new ArgumentNullException(nameof(contingencyEngine));
    }

    public async Task<OrganizationalReadinessAssessment> EvaluateScenarioReadinessAsync(
        string tenantId,
        ReadinessScenario scenario,
        Dictionary<ReadinessDimension, double> dimensionMetrics,
        double baseCapacity = 100.0,
        double bufferRatio = 0.25)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));
        if (scenario == null) throw new ArgumentNullException(nameof(scenario));

        // 1. Persist scenario
        await _store.SaveScenarioAsync(tenantId, scenario);

        // 2. Perform buffer stress test
        var stressResult = _stressTestEngine.SimulateCapacityStress(
            tenantId,
            scenario,
            baseCapacity,
            bufferRatio,
            criticalResourceIds: new List<string> { "RES-CORE-BANDWIDTH", "RES-SUPPLY-LINE" });
        await _store.SaveStressTestResultAsync(tenantId, stressResult);

        // 3. Compute deterministic 8-dimensional assessment
        var assessment = _scoringEngine.EvaluateReadiness(
            tenantId,
            scenario,
            dimensionMetrics,
            stressResult);

        // 4. If buffer is exhausted and capacity is in deficit, record readiness debt automatically
        if (stressResult.IsBufferExhaustedWithinHorizon)
        {
            _debtTracker.RecordDebt(
                tenantId,
                ReadinessDebtType.CapacityDebt,
                exposureAmount: stressResult.SimulatedStressDemand - baseCapacity,
                exposureDescription: $"Capacity buffer exhaustion within {stressResult.BufferExhaustionHorizonWeeks} weeks under '{scenario.Name}'.");
        }

        // 5. Persist assessment
        await _store.SaveAssessmentAsync(tenantId, assessment);

        return assessment;
    }

    public async Task<WhyReadinessTrace> GetWhyReadinessTraceAsync(string tenantId, string assessmentId)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));
        if (string.IsNullOrWhiteSpace(assessmentId)) throw new ArgumentNullException(nameof(assessmentId));

        var assessment = await _store.GetAssessmentAsync(tenantId, assessmentId)
            ?? throw new InvalidOperationException($"Assessment '{assessmentId}' not found for tenant '{tenantId}'.");

        if (assessment.WhyTrace != null && assessment.WhyTrace.DimensionScores.Count > 0)
        {
            return assessment.WhyTrace;
        }

        var scenario = await _store.GetScenarioAsync(tenantId, assessment.ScenarioId)
            ?? new ReadinessScenario { ScenarioId = assessment.ScenarioId, Name = "Unknown Scenario" };

        return _scoringEngine.ExplainAssessment(assessment, scenario);
    }

    public Task<IReadOnlyList<ReadinessDebtRecord>> GetTenantReadinessDebtAsync(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return Task.FromResult<IReadOnlyList<ReadinessDebtRecord>>(Array.Empty<ReadinessDebtRecord>());
        }

        var debts = _debtTracker.EvaluateTenantDebt(tenantId);
        return Task.FromResult(debts);
    }

    public async Task<WorkProposal> StageContingencyToControlPlaneAsync(string tenantId, string contingencyId)
    {
        return await _contingencyEngine.StageContingentWorkProposalAsync(tenantId, contingencyId);
    }
}
