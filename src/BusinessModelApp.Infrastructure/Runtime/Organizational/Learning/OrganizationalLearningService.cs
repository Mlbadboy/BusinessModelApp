using BusinessModelApp.Core.Domain.Runtime.Organizational.Learning;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational.Learning;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational.Learning;

public sealed class OrganizationalLearningService : IOrganizationalLearningService
{
    private readonly IEmpiricalOutcomeIngestor _ingestor;
    private readonly IModelMetrologyEngine _metrologyEngine;
    private readonly IStructuralDriftDetector _driftDetector;
    private readonly ILessonDistiller _distiller;
    private readonly IAdaptationEngine _adaptationEngine;
    private readonly ILearningAuditRepository _repository;
    private readonly ILearningProvenanceService _provenanceService;

    public OrganizationalLearningService(
        IEmpiricalOutcomeIngestor ingestor,
        IModelMetrologyEngine metrologyEngine,
        IStructuralDriftDetector driftDetector,
        ILessonDistiller distiller,
        IAdaptationEngine adaptationEngine,
        ILearningAuditRepository repository,
        ILearningProvenanceService provenanceService)
    {
        _ingestor = ingestor ?? throw new ArgumentNullException(nameof(ingestor));
        _metrologyEngine = metrologyEngine ?? throw new ArgumentNullException(nameof(metrologyEngine));
        _driftDetector = driftDetector ?? throw new ArgumentNullException(nameof(driftDetector));
        _distiller = distiller ?? throw new ArgumentNullException(nameof(distiller));
        _adaptationEngine = adaptationEngine ?? throw new ArgumentNullException(nameof(adaptationEngine));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _provenanceService = provenanceService ?? throw new ArgumentNullException(nameof(provenanceService));
    }

    public async Task<EmpiricalOutcomeEvent> RecordEmpiricalOutcomeAsync(string tenantId, EmpiricalOutcomeEvent outcome)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));
        if (outcome == null) throw new ArgumentNullException(nameof(outcome));
        outcome.TenantId = tenantId;
        return await _ingestor.IngestOutcomeAsync(outcome);
    }

    public Task<IReadOnlyList<EmpiricalOutcomeEvent>> GetOutcomesAsync(string tenantId, string? domain = null)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));
        return _ingestor.GetOutcomesAsync(tenantId, domain);
    }

    public async Task<MetrologyRecord> EvaluateCalibrationAsync(string tenantId, string modelOrDomain)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));
        if (string.IsNullOrWhiteSpace(modelOrDomain)) throw new ArgumentNullException(nameof(modelOrDomain));

        var outcomes = await _repository.ListOutcomesAsync(tenantId, modelOrDomain);
        var record = _metrologyEngine.CalculateMetrology(tenantId, modelOrDomain, outcomes);
        await _repository.SaveMetrologyAsync(record);
        return record;
    }

    public async Task<DriftDetectionResult> CheckStructuralDriftAsync(string tenantId, string modelOrDomain)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));
        if (string.IsNullOrWhiteSpace(modelOrDomain)) throw new ArgumentNullException(nameof(modelOrDomain));

        var allOutcomes = await _repository.ListOutcomesAsync(tenantId, modelOrDomain);
        if (allOutcomes.Count < 4)
        {
            return new DriftDetectionResult
            {
                DriftId = Guid.NewGuid().ToString("N"),
                TenantId = tenantId,
                TargetModel = modelOrDomain,
                DetectedDriftType = DriftType.None,
                DivergenceScore = 0.0,
                PValue = 1.0,
                RequiresCaution = false,
                RecommendedPosture = "Nominal",
                DetectedUtc = DateTime.UtcNow
            };
        }

        int half = allOutcomes.Count / 2;
        var recent = allOutcomes.Take(half).ToList();
        var baseline = allOutcomes.Skip(half).ToList();

        return _driftDetector.EvaluateDrift(tenantId, modelOrDomain, recent, baseline);
    }

    public async Task<OrganizationalLesson?> DistillLessonAsync(
        string tenantId,
        string title,
        string domain,
        string causalHypothesis,
        string counterfactualInsight)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));
        var outcomes = await _repository.ListOutcomesAsync(tenantId, domain);
        return await _distiller.DistillLessonAsync(tenantId, title, outcomes, causalHypothesis, counterfactualInsight);
    }

    public Task<IReadOnlyList<OrganizationalLesson>> ListLessonsAsync(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));
        return _repository.ListLessonsAsync(tenantId);
    }

    public Task<OrganizationalLesson?> GetLessonAsync(string tenantId, string lessonId)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));
        if (string.IsNullOrWhiteSpace(lessonId)) throw new ArgumentNullException(nameof(lessonId));
        return _repository.GetLessonAsync(tenantId, lessonId);
    }

    public async Task<AdaptationProposal?> ProposeAdaptationAsync(
        string tenantId,
        string lessonId,
        string parameterKey,
        double proposedDelta)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));
        if (string.IsNullOrWhiteSpace(lessonId)) throw new ArgumentNullException(nameof(lessonId));

        var lesson = await _repository.GetLessonAsync(tenantId, lessonId);
        if (lesson == null)
        {
            throw new KeyNotFoundException($"Lesson '{lessonId}' not found for tenant '{tenantId}'.");
        }

        return await _adaptationEngine.ProposeAdaptationAsync(tenantId, lesson, parameterKey, proposedDelta);
    }

    public Task<IReadOnlyList<AdaptationProposal>> ListProposalsAsync(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));
        return _repository.ListProposalsAsync(tenantId);
    }

    public async Task<WhyAdaptationTrace> GetAdaptationWhyTraceAsync(string tenantId, string proposalId)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));
        if (string.IsNullOrWhiteSpace(proposalId)) throw new ArgumentNullException(nameof(proposalId));

        var proposal = await _repository.GetProposalAsync(tenantId, proposalId);
        if (proposal == null)
        {
            throw new KeyNotFoundException($"Adaptation proposal '{proposalId}' not found for tenant '{tenantId}'.");
        }

        var lesson = await _repository.GetLessonAsync(tenantId, proposal.LessonId);
        var outcomes = await _repository.ListOutcomesAsync(tenantId);
        var metrology = (await _repository.ListMetrologyAsync(tenantId)).FirstOrDefault() ?? new MetrologyRecord
        {
            TenantId = tenantId,
            ModelOrDomain = proposal.ParameterKey
        };

        return await _provenanceService.GenerateTraceAsync(
            proposal,
            lesson ?? new OrganizationalLesson { LessonId = proposal.LessonId, Title = "Reference Lesson" },
            outcomes,
            metrology);
    }
}
