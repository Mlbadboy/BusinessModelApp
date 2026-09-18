using BusinessModelApp.Core.Domain.Runtime.Organizational;
using BusinessModelApp.Core.Domain.Runtime.Organizational.Readiness;

namespace BusinessModelApp.Core.Interfaces.Runtime.Organizational;

/// <summary>
/// Thread-safe multi-tenant store for predictive readiness data.
/// </summary>
public interface IPredictiveReadinessStore
{
    Task SaveScenarioAsync(string tenantId, ReadinessScenario scenario);
    Task<ReadinessScenario?> GetScenarioAsync(string tenantId, string scenarioId);
    Task<IReadOnlyList<ReadinessScenario>> ListScenariosAsync(string tenantId);

    Task SaveAssessmentAsync(string tenantId, OrganizationalReadinessAssessment assessment);
    Task<OrganizationalReadinessAssessment?> GetAssessmentAsync(string tenantId, string assessmentId);
    Task<IReadOnlyList<OrganizationalReadinessAssessment>> ListAssessmentsAsync(string tenantId);

    Task SaveStressTestResultAsync(string tenantId, CapacityStressTestResult result);
    Task<CapacityStressTestResult?> GetStressTestResultAsync(string tenantId, string testId);

    Task SaveDebtRecordAsync(string tenantId, ReadinessDebtRecord debt);
    Task<IReadOnlyList<ReadinessDebtRecord>> ListDebtRecordsAsync(string tenantId);

    Task SaveContingencyAsync(string tenantId, ContingencyProposal contingency);
    Task<ContingencyProposal?> GetContingencyAsync(string tenantId, string contingencyId);
    Task<IReadOnlyList<ContingencyProposal>> ListContingenciesAsync(string tenantId);
}

/// <summary>
/// Engine for stress testing capacity buffers against future scenarios.
/// Invariant I31-F: Stress Test != Real-World Experiment.
/// </summary>
public interface ICapacityStressTestEngine
{
    CapacityStressTestResult SimulateCapacityStress(
        string tenantId,
        ReadinessScenario scenario,
        double baseCapacity,
        double bufferRatio,
        List<string> criticalResourceIds);
}

/// <summary>
/// Engine for deterministically scoring organizational readiness across 8 dimensions.
/// Invariant I31-G: Probability != Certainty.
/// </summary>
public interface IReadinessScoringEngine
{
    OrganizationalReadinessAssessment EvaluateReadiness(
        string tenantId,
        ReadinessScenario scenario,
        Dictionary<ReadinessDimension, double> dimensionInputs,
        CapacityStressTestResult? stressResult = null);

    WhyReadinessTrace ExplainAssessment(OrganizationalReadinessAssessment assessment, ReadinessScenario scenario);
}

/// <summary>
/// Engine for resolving organizational action posture: "Prepare Now vs Wait".
/// Invariant I31-B: Prediction != Inevitability.
/// </summary>
public interface IActionPostureResolver
{
    ReadinessActionPosture ResolvePosture(
        double estimatedProbability,
        double impactMagnitude,
        HorizonWindow horizon,
        double preparationCost,
        double reversibilityScore,
        double confidenceInterval);
}

/// <summary>
/// Engine for tracking persistent readiness debt.
/// Known Future Exposure + Insufficient Preparation = Readiness Debt.
/// </summary>
public interface IReadinessDebtTracker
{
    ReadinessDebtRecord RecordDebt(
        string tenantId,
        ReadinessDebtType debtType,
        double exposureAmount,
        string exposureDescription,
        double compoundingFactor = 1.0);

    IReadOnlyList<ReadinessDebtRecord> EvaluateTenantDebt(string tenantId);
    bool RemediateDebt(string tenantId, string debtId);
}

/// <summary>
/// Engine for managing pre-staged contingency proposals.
/// Invariant I31-K: Contingency Proposal != Approved Work.
/// Invariant I31-O: POR Cannot Create Execution Authority.
/// </summary>
public interface IContingencyPlanningEngine
{
    ContingencyProposal CreateContingency(
        string tenantId,
        string scenarioId,
        string title,
        string objective,
        string proposedWorkPayload,
        string activationTrigger,
        double estimatedCost,
        double reversibility);

    Task<WorkProposal> StageContingentWorkProposalAsync(
        string tenantId,
        string contingencyId,
        string proposerActorId = "POR-ContingencyEngine");
}

/// <summary>
/// Facade coordinating the Predictive Organizational Readiness pipeline.
/// </summary>
public interface IPredictiveReadinessService
{
    Task<OrganizationalReadinessAssessment> EvaluateScenarioReadinessAsync(
        string tenantId,
        ReadinessScenario scenario,
        Dictionary<ReadinessDimension, double> dimensionMetrics,
        double baseCapacity = 100.0,
        double bufferRatio = 0.25);

    Task<WhyReadinessTrace> GetWhyReadinessTraceAsync(string tenantId, string assessmentId);
    Task<IReadOnlyList<ReadinessDebtRecord>> GetTenantReadinessDebtAsync(string tenantId);
    Task<WorkProposal> StageContingencyToControlPlaneAsync(string tenantId, string contingencyId);
}
