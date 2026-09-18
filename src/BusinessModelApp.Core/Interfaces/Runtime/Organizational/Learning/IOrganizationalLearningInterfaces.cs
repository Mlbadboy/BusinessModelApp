using BusinessModelApp.Core.Domain.Runtime.Organizational.Learning;

namespace BusinessModelApp.Core.Interfaces.Runtime.Organizational.Learning;

public interface IEmpiricalOutcomeIngestor
{
    Task<EmpiricalOutcomeEvent> IngestOutcomeAsync(EmpiricalOutcomeEvent outcomeEvent);
    Task<IReadOnlyList<EmpiricalOutcomeEvent>> GetOutcomesAsync(string tenantId, string? domain = null);
}

public interface IModelMetrologyEngine
{
    MetrologyRecord CalculateMetrology(
        string tenantId,
        string modelOrDomain,
        IReadOnlyList<EmpiricalOutcomeEvent> outcomes);
}

public interface IStructuralDriftDetector
{
    DriftDetectionResult EvaluateDrift(
        string tenantId,
        string targetModel,
        IReadOnlyList<EmpiricalOutcomeEvent> recentOutcomes,
        IReadOnlyList<EmpiricalOutcomeEvent> baselineOutcomes);
}

public interface IAdaptationTargetRegistry
{
    bool TryValidateTarget(string parameterKey, double proposedDelta, out string rejectionReason, out WhitelistTargetDefinition? targetDef);
    IReadOnlyList<WhitelistTargetDefinition> GetRegisteredTargets();
}

public interface ILessonDistiller
{
    Task<OrganizationalLesson?> DistillLessonAsync(
        string tenantId,
        string title,
        IReadOnlyList<EmpiricalOutcomeEvent> outcomes,
        string causalHypothesis,
        string counterfactualEvidence);
}

public interface IAdaptationEngine
{
    Task<AdaptationProposal?> ProposeAdaptationAsync(
        string tenantId,
        OrganizationalLesson lesson,
        string parameterKey,
        double proposedDelta);
}

public interface ILearningAuditRepository
{
    Task SaveOutcomeAsync(EmpiricalOutcomeEvent outcome);
    Task<IReadOnlyList<EmpiricalOutcomeEvent>> ListOutcomesAsync(string tenantId, string? domain = null);

    Task SaveMetrologyAsync(MetrologyRecord record);
    Task<IReadOnlyList<MetrologyRecord>> ListMetrologyAsync(string tenantId);

    Task SaveLessonAsync(OrganizationalLesson lesson);
    Task<OrganizationalLesson?> GetLessonAsync(string tenantId, string lessonId);
    Task<IReadOnlyList<OrganizationalLesson>> ListLessonsAsync(string tenantId);

    Task SaveProposalAsync(AdaptationProposal proposal);
    Task<AdaptationProposal?> GetProposalAsync(string tenantId, string proposalId);
    Task<IReadOnlyList<AdaptationProposal>> ListProposalsAsync(string tenantId);

    AdaptationHysteresisState GetOrCreateHysteresisState(string tenantId, string parameterKey);
    void UpdateHysteresisState(string tenantId, string parameterKey, double value, DateTime timestamp);
}

public interface ILearningProvenanceService
{
    Task<WhyAdaptationTrace> GenerateTraceAsync(
        AdaptationProposal proposal,
        OrganizationalLesson lesson,
        IReadOnlyList<EmpiricalOutcomeEvent> triggeringOutcomes,
        MetrologyRecord metrology);
}

/// <summary>
/// Unified facade coordinating the entire OLMA pipeline for Charlie (I35).
/// </summary>
public interface IOrganizationalLearningService
{
    Task<EmpiricalOutcomeEvent> RecordEmpiricalOutcomeAsync(string tenantId, EmpiricalOutcomeEvent outcome);
    Task<IReadOnlyList<EmpiricalOutcomeEvent>> GetOutcomesAsync(string tenantId, string? domain = null);

    Task<MetrologyRecord> EvaluateCalibrationAsync(string tenantId, string modelOrDomain);
    Task<DriftDetectionResult> CheckStructuralDriftAsync(string tenantId, string modelOrDomain);

    Task<OrganizationalLesson?> DistillLessonAsync(
        string tenantId,
        string title,
        string domain,
        string causalHypothesis,
        string counterfactualInsight);

    Task<IReadOnlyList<OrganizationalLesson>> ListLessonsAsync(string tenantId);
    Task<OrganizationalLesson?> GetLessonAsync(string tenantId, string lessonId);

    Task<AdaptationProposal?> ProposeAdaptationAsync(
        string tenantId,
        string lessonId,
        string parameterKey,
        double proposedDelta);

    Task<IReadOnlyList<AdaptationProposal>> ListProposalsAsync(string tenantId);
    Task<WhyAdaptationTrace> GetAdaptationWhyTraceAsync(string tenantId, string proposalId);
}
