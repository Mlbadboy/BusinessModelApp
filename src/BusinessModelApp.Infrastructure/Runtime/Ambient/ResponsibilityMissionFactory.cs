using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Responsibilities;
using BusinessModelApp.Core.Domain.Runtime;
using BusinessModelApp.Core.Interfaces.Ambient;
using BusinessModelApp.Core.Interfaces.Runtime;

namespace BusinessModelApp.Infrastructure.Runtime.Ambient
{
    public class ResponsibilityMissionFactory : IResponsibilityMissionFactory
    {
        private readonly IBrainDirector? _brainDirector;
        private readonly IResponsibilityAuditLedger _auditLedger;

        public ResponsibilityMissionFactory(
            IResponsibilityAuditLedger auditLedger,
            IBrainDirector? brainDirector = null)
        {
            _auditLedger = auditLedger ?? throw new ArgumentNullException(nameof(auditLedger));
            _brainDirector = brainDirector;
        }

        public async Task<ResponsibilityMissionProposal> CreateProposalAsync(
            ResponsibilityRecord responsibility,
            IReadOnlyList<AmbientBusinessEvent> triggeringEvents,
            AutonomyTier maxTenantAllowedAutonomy,
            bool allowBrainHypothesis = true,
            CancellationToken cancellationToken = default)
        {
            if (responsibility == null) throw new ArgumentNullException(nameof(responsibility));

            // Invariant: Mathematical Autonomy Tier Ceiling
            // EffectiveAutonomyTier = MIN(ResponsibilityConfiguredTier, TenantPolicyTier)
            var effectiveAutonomy = (AutonomyTier)Math.Min(
                (int)responsibility.AllowedAutonomyTier,
                (int)maxTenantAllowedAutonomy);

            var eventIds = triggeringEvents?.Select(e => e.EventId).ToList() ?? new List<Guid>();
            var summaries = triggeringEvents?.Select(e => $"{e.Source}: {e.EventType} ({e.MetricValue} {e.Unit})").ToList() ?? new List<string>();

            // Check if this anomaly warrants an active mission or "No Mission / Monitor" disposition
            if (responsibility.SeverityScore < 0.5m || summaries.Count == 0)
            {
                var noMission = new ResponsibilityMissionProposal
                {
                    ProposalId = Guid.NewGuid(),
                    ResponsibilityId = responsibility.Id,
                    WorkspaceId = responsibility.WorkspaceId,
                    MissionType = "Investigation.Dismissed",
                    Title = $"[Monitor] {responsibility.Title}",
                    Rationale = "Anomaly evaluated: Metric deviation insufficient to warrant autonomous mission initiation.",
                    TriggerEventIds = eventIds,
                    EvidenceSummaries = summaries,
                    Priority = responsibility.Priority,
                    SeverityScore = responsibility.SeverityScore,
                    EffectiveAutonomyTier = AutonomyTier.L0_Observe,
                    RequiresHumanApproval = false,
                    IsNoMissionOutcome = true,
                    DispositionNotes = "Monitored without creating actionable mission."
                };

                await _auditLedger.RecordProposalAsync(noMission, cancellationToken);
                return noMission;
            }

            // Brain Fabric Integration (Optional Cognitive Hypothesis)
            BrainRequestId? brainReqId = null;
            var rationale = $"Deterministic trigger for responsibility '{responsibility.Title}'. Breach count: {responsibility.ConsecutiveBreaches}.";

            if (allowBrainHypothesis && _brainDirector != null)
            {
                try
                {
                    var brainRequest = new BrainInferenceRequest
                    {
                        WorkspaceId = responsibility.WorkspaceId,
                        Prompt = $"Analyze root cause hypothesis for {responsibility.Title}. Evidence: {string.Join(", ", summaries)}",
                        SystemPrompt = "Synthesize root-cause hypothesis and investigation objectives for executive review."
                    };

                    var brainResult = await _brainDirector.RequestInferenceAsync(brainRequest, cancellationToken);
                    if (brainResult.Status == InferenceLifecycleState.Completed && !string.IsNullOrWhiteSpace(brainResult.RawOutput))
                    {
                        brainReqId = brainResult.RequestId;
                        rationale = $"{rationale}\nBrain Hypothesis: {brainResult.RawOutput}";
                    }
                }
                catch
                {
                    // Brain Director is optional - fallback to deterministic hypothesis
                }
            }

            var proposal = new ResponsibilityMissionProposal
            {
                ProposalId = Guid.NewGuid(),
                ResponsibilityId = responsibility.Id,
                WorkspaceId = responsibility.WorkspaceId,
                MissionType = $"Investigate.{responsibility.Domain}",
                Title = $"Investigate {responsibility.Title}",
                Rationale = rationale,
                TriggerEventIds = eventIds,
                EvidenceSummaries = summaries,
                Priority = responsibility.Priority,
                SeverityScore = responsibility.SeverityScore,
                EffectiveAutonomyTier = effectiveAutonomy,
                RequiredCapabilities = new List<string> { $"{responsibility.Domain.ToString().ToLowerInvariant()}.analyze:v1" },
                RequiresHumanApproval = effectiveAutonomy <= AutonomyTier.L4_ExecuteWithApproval,
                BrainRequestId = brainReqId,
                ProposedAtUtc = DateTime.UtcNow,
                DeadlineUtc = DateTime.UtcNow.AddHours(24),
                IsNoMissionOutcome = false,
                DispositionNotes = "Actionable mission proposed."
            };

            await _auditLedger.RecordProposalAsync(proposal, cancellationToken);
            return proposal;
        }
    }
}
