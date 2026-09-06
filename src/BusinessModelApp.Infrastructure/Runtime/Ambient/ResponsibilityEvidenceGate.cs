using System;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Responsibilities;
using BusinessModelApp.Core.Interfaces.Ambient;

namespace BusinessModelApp.Infrastructure.Runtime.Ambient
{
    public class ResponsibilityEvidenceGate : IResponsibilityEvidenceGate
    {
        private static readonly TimeSpan MaxEventAge = TimeSpan.FromHours(24);

        public Task<SignalEvidenceAssessment> EvaluateSignalAsync(AmbientBusinessEvent businessEvent, CancellationToken cancellationToken = default)
        {
            if (businessEvent == null) throw new ArgumentNullException(nameof(businessEvent));

            // 1. Poisoned Signal Detection
            if (businessEvent.IsPoisoned)
            {
                return Task.FromResult(new SignalEvidenceAssessment
                {
                    SignalEventId = businessEvent.EventId,
                    WorkspaceId = businessEvent.WorkspaceId,
                    IsCorroborated = false,
                    IsPoisonedSignal = true,
                    Status = EvidenceStatus.Poisoned,
                    Explanation = "Signal rejected: Adversarial or poisoned payload detected."
                });
            }

            // 2. Contradiction Detection
            if (businessEvent.IsContradictorySignal)
            {
                return Task.FromResult(new SignalEvidenceAssessment
                {
                    SignalEventId = businessEvent.EventId,
                    WorkspaceId = businessEvent.WorkspaceId,
                    IsCorroborated = false,
                    HasContradiction = true,
                    Status = EvidenceStatus.Contradicted,
                    Explanation = "Signal contradicted: Conflicting baseline telemetry exists; preserved as UNKNOWN."
                });
            }

            // 3. Freshness Assessment
            var age = DateTime.UtcNow - businessEvent.OccurredAtUtc;
            var freshnessScore = age > MaxEventAge ? 0.2 : Math.Max(0.5, 1.0 - (age.TotalHours / 24.0));
            if (age > MaxEventAge)
            {
                return Task.FromResult(new SignalEvidenceAssessment
                {
                    SignalEventId = businessEvent.EventId,
                    WorkspaceId = businessEvent.WorkspaceId,
                    IsCorroborated = false,
                    FreshnessScore = freshnessScore,
                    Status = EvidenceStatus.Weak,
                    Explanation = "Signal stale: Event occurred outside active observation window (>24h)."
                });
            }

            // 4. Source Trust Assessment
            if (businessEvent.SourceTrustLevel < 0.6)
            {
                return Task.FromResult(new SignalEvidenceAssessment
                {
                    SignalEventId = businessEvent.EventId,
                    WorkspaceId = businessEvent.WorkspaceId,
                    IsCorroborated = false,
                    TrustScore = businessEvent.SourceTrustLevel,
                    Status = EvidenceStatus.Weak,
                    Explanation = "Signal uncorroborated: Source trust level below minimum threshold (0.6)."
                });
            }

            // Passed all gates: Promoted to strong corroborated evidence
            return Task.FromResult(new SignalEvidenceAssessment
            {
                SignalEventId = businessEvent.EventId,
                WorkspaceId = businessEvent.WorkspaceId,
                IsCorroborated = true,
                TrustScore = businessEvent.SourceTrustLevel,
                FreshnessScore = freshnessScore,
                Status = EvidenceStatus.Strong,
                Explanation = "Signal validated and corroborated: Freshness, trust, and integrity verified."
            });
        }
    }
}
