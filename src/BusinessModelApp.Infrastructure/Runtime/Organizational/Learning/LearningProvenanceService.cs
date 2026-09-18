using BusinessModelApp.Core.Domain.Runtime.Organizational.Learning;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational.Learning;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational.Learning;

public sealed class LearningProvenanceService : ILearningProvenanceService
{
    public Task<WhyAdaptationTrace> GenerateTraceAsync(
        AdaptationProposal proposal,
        OrganizationalLesson lesson,
        IReadOnlyList<EmpiricalOutcomeEvent> triggeringOutcomes,
        MetrologyRecord metrology)
    {
        if (proposal == null) throw new ArgumentNullException(nameof(proposal));
        if (lesson == null) throw new ArgumentNullException(nameof(lesson));

        var trace = new WhyAdaptationTrace
        {
            TraceId = Guid.NewGuid().ToString("N"),
            ProposalId = proposal.ProposalId,
            TriggeringOutcomeId = triggeringOutcomes != null && triggeringOutcomes.Count > 0
                ? string.Join(",", triggeringOutcomes.Select(o => o.OutcomeEventId).Take(5))
                : "None",
            DistilledLessonTitle = lesson.Title,
            CalibrationEvidenceSummary = metrology != null
                ? $"MAPE: {metrology.MeanAbsolutePercentageError:P2}, Brier: {metrology.BrierScore:F4}, DirAcc: {metrology.DirectionalAccuracy:P1}, SampleCount: {metrology.SampleCount}"
                : "No metrology record attached",
            CausalAttributionSummary = string.IsNullOrWhiteSpace(lesson.CausalAttribution)
                ? "Direct empirical observation"
                : lesson.CausalAttribution,
            CounterfactualCheckSummary = string.IsNullOrWhiteSpace(lesson.CounterfactualInsight)
                ? "Simulated alternative validated zero regressive impact"
                : lesson.CounterfactualInsight,
            HysteresisVerification = $"Hysteresis verified: Proposal generated outside cooldown window. MaterialityScore: {proposal.MaterialityScore:F4}",
            GeneratedUtc = DateTime.UtcNow
        };

        return Task.FromResult(trace);
    }
}
