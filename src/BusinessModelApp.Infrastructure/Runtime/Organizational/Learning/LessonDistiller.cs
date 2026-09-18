using BusinessModelApp.Core.Domain.Runtime.Organizational.Learning;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational.Learning;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational.Learning;

public sealed class LessonDistiller : ILessonDistiller
{
    private readonly ILearningAuditRepository _repository;

    public LessonDistiller(ILearningAuditRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<OrganizationalLesson?> DistillLessonAsync(
        string tenantId,
        string title,
        IReadOnlyList<EmpiricalOutcomeEvent> outcomes,
        string causalHypothesis,
        string counterfactualEvidence)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentNullException(nameof(title));

        var lesson = new OrganizationalLesson
        {
            LessonId = Guid.NewGuid().ToString("N"),
            TenantId = tenantId,
            Title = title,
            CausalAttribution = causalHypothesis ?? string.Empty,
            CounterfactualInsight = counterfactualEvidence ?? string.Empty,
            DistilledUtc = DateTime.UtcNow,
            EpistemicTag = OrganizationalLearningInvariants.EpistemicClassificationLesson
        };

        if (outcomes == null || outcomes.Count == 0)
        {
            lesson.ObservationSummary = "No empirical outcomes provided for lesson distillation.";
            lesson.EvidenceSufficiency = new EvidenceSufficiencyScore
            {
                SampleSize = 0,
                EffectSize = 0.0,
                Confidence = 0.0,
                Stability = 0.0,
                AttributionQuality = 0.0,
                RegimeConsistency = 0.0
            };
            lesson.State = LearningEpistemicState.InconclusiveEvidence;
            lesson.Confidence = 0.0;
            lesson.ComputeLessonHash();
            await _repository.SaveLessonAsync(lesson);
            return lesson;
        }

        lesson.ObservationSummary = $"Aggregated {outcomes.Count} empirical outcomes across domains: {string.Join(", ", outcomes.Select(o => o.SourceDomain).Distinct())}.";

        // Calculate EvidenceSufficiency (I35-V)
        int sampleSize = outcomes.Count;
        double avgVariance = outcomes.Average(o => o.VarianceScore);
        double stability = Math.Clamp(1.0 - (avgVariance / 2.0), 0.0, 1.0);
        double effectSize = Math.Clamp(outcomes.Average(o => Math.Abs(o.DeltaValue)) / 10.0, 0.05, 0.95);
        double regimeConsistency = outcomes.Select(o => o.EnvironmentRegime).Distinct().Count() == 1 ? 0.95 : 0.60;
        double attributionQuality = string.IsNullOrWhiteSpace(causalHypothesis) ? 0.30 : 0.85;
        double confidence = Math.Round(Math.Clamp(0.65 + (sampleSize * 0.015) * stability, 0.10, 0.98), 4);

        var sufficiency = new EvidenceSufficiencyScore
        {
            SampleSize = sampleSize,
            EffectSize = Math.Round(effectSize, 4),
            Confidence = confidence,
            Stability = Math.Round(stability, 4),
            AttributionQuality = attributionQuality,
            RegimeConsistency = regimeConsistency
        };

        lesson.EvidenceSufficiency = sufficiency;
        lesson.Confidence = confidence;

        // Anti-hallucination & Evidence Sufficiency Gating (I35-I, I35-V)
        if (!sufficiency.IsSufficient)
        {
            lesson.State = LearningEpistemicState.InconclusiveEvidence;
        }
        else
        {
            lesson.State = LearningEpistemicState.LessonDistilled;
        }

        lesson.ComputeLessonHash();
        await _repository.SaveLessonAsync(lesson);
        return lesson;
    }
}
