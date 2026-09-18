using BusinessModelApp.Core.Domain.Runtime.Organizational.Learning;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational.Learning;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational.Learning;

public sealed class AdaptationEngine : IAdaptationEngine
{
    private readonly ILearningAuditRepository _repository;
    private readonly IAdaptationTargetRegistry _targetRegistry;

    public AdaptationEngine(
        ILearningAuditRepository repository,
        IAdaptationTargetRegistry targetRegistry)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _targetRegistry = targetRegistry ?? throw new ArgumentNullException(nameof(targetRegistry));
    }

    public async Task<AdaptationProposal?> ProposeAdaptationAsync(
        string tenantId,
        OrganizationalLesson lesson,
        string parameterKey,
        double proposedDelta)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));
        if (lesson == null) throw new ArgumentNullException(nameof(lesson));
        if (string.IsNullOrWhiteSpace(parameterKey)) throw new ArgumentNullException(nameof(parameterKey));

        // Invariant I35-I / I35-V: Inconclusive lessons cannot be used to propose adaptations
        if (lesson.State == LearningEpistemicState.InconclusiveEvidence)
        {
            throw new InvalidOperationException($"Cannot propose adaptation from lesson '{lesson.LessonId}' with inconclusive evidence. (I35-I, I35-V)");
        }

        // Validate target whitelist and step magnitude (I35-C, I35-K)
        if (!_targetRegistry.TryValidateTarget(parameterKey, proposedDelta, out var rejectionReason, out var targetDef) || targetDef == null)
        {
            throw new InvalidOperationException($"Adaptation target validation failed: {rejectionReason}");
        }

        // Check Adaptation Hysteresis (I35-Y): Cooldown window
        var hysteresis = _repository.GetOrCreateHysteresisState(tenantId, parameterKey);
        var now = DateTime.UtcNow;
        if (hysteresis.IsInCooldown(now))
        {
            throw new InvalidOperationException($"Adaptation for parameter '{parameterKey}' is blocked by hysteresis cooldown until {hysteresis.LastAdaptedUtc + hysteresis.CooldownWindow:u}. (I35-Y)");
        }

        // Materiality Threshold check (I35-Q)
        // Materiality score = |proposedDelta| / targetDef.MaxDeltaPerAdaptation
        var materialityScore = Math.Round(Math.Abs(proposedDelta) / (targetDef.MaxDeltaPerAdaptation > 0.0001 ? targetDef.MaxDeltaPerAdaptation : 1.0), 4);
        if (materialityScore < 0.15)
        {
            throw new InvalidOperationException($"Proposed adaptation delta {proposedDelta:F4} has a materiality score of {materialityScore:F4}, which is below the mandatory threshold of 0.15. Parameter thrashing rejected. (I35-Q)");
        }

        // Determine baseline value from hysteresis state or midpoint
        var currentValue = hysteresis.LastAdaptedValue > 0.0001 ? hysteresis.LastAdaptedValue : (targetDef.SafeMin + targetDef.SafeMax) / 2.0;
        var proposedValue = Math.Clamp(currentValue + proposedDelta, targetDef.SafeMin, targetDef.SafeMax);

        var proposal = new AdaptationProposal
        {
            ProposalId = Guid.NewGuid().ToString("N"),
            TenantId = tenantId,
            LessonId = lesson.LessonId,
            ParameterKey = parameterKey,
            TargetType = targetDef.TargetType,
            CurrentValue = Math.Round(currentValue, 4),
            ProposedValue = Math.Round(proposedValue, 4),
            MaterialityScore = materialityScore,
            JustificationRationale = $"Proposed adaptation of {parameterKey} by delta {proposedDelta:F4} based on distilled lesson '{lesson.Title}' with confidence {lesson.Confidence:F2}.",
            State = LearningEpistemicState.AdaptationProposed,
            RequiredGovernanceApprovalLevel = "PRG-1 Human Governance Required",
            ProposedUtc = now
        };

        // Note: OLMA NEVER approves or applies proposals directly (I35-E, I35-U)
        await _repository.SaveProposalAsync(proposal);
        return proposal;
    }
}
