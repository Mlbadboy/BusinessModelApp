using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Responsibilities;
using BusinessModelApp.Core.Domain.Runtime;

namespace BusinessModelApp.Core.Interfaces.Ambient
{
    public interface IAmbientEventNormalizer
    {
        AmbientBusinessEvent NormalizeEvent(
            Guid workspaceId,
            string source,
            string eventType,
            string entityType,
            string entityId,
            decimal metricValue,
            string unit,
            Dictionary<string, object>? payload = null,
            double sourceTrust = 1.0);
    }

    public interface IAmbientEventSource
    {
        Task<bool> IngestEventAsync(AmbientBusinessEvent businessEvent, CancellationToken cancellationToken = default);
    }

    public interface IResponsibilityEvidenceGate
    {
        Task<SignalEvidenceAssessment> EvaluateSignalAsync(AmbientBusinessEvent businessEvent, CancellationToken cancellationToken = default);
    }

    public interface IResponsibilityRegistry
    {
        ResponsibilityId ComputeDeterministicId(Guid workspaceId, string definitionId, string entityScope, string triggerId);
        Task RegisterDefinitionAsync(ResponsibilityRecord template, AmbientTriggerCondition trigger, CancellationToken cancellationToken = default);
        Task<ResponsibilityRecord?> GetActiveResponsibilityAsync(ResponsibilityId id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ResponsibilityRecord>> GetActiveResponsibilitiesAsync(Guid workspaceId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<(ResponsibilityRecord Template, AmbientTriggerCondition Trigger)>> GetRegisteredDefinitionsAsync(Guid workspaceId, CancellationToken cancellationToken = default);
        Task SaveActiveResponsibilityAsync(ResponsibilityRecord record, CancellationToken cancellationToken = default);
    }

    public interface IResponsibilityCorrelationEngine
    {
        Task<IReadOnlyList<AmbientBusinessEvent>> CorrelateEventsAsync(Guid workspaceId, string entityScope, TimeSpan correlationWindow, CancellationToken cancellationToken = default);
        Task RecordEventForCorrelationAsync(AmbientBusinessEvent businessEvent, CancellationToken cancellationToken = default);
    }

    public interface IResponsibilityDebouncer
    {
        Task<bool> ShouldSuppressAsync(ResponsibilityRecord responsibility, AmbientBusinessEvent businessEvent, CancellationToken cancellationToken = default);
        Task RecordTriggerAsync(ResponsibilityRecord responsibility, AmbientBusinessEvent businessEvent, CancellationToken cancellationToken = default);
        Task<bool> CheckRecoveryAsync(ResponsibilityRecord responsibility, decimal currentValue, decimal recoveryThreshold, CancellationToken cancellationToken = default);
        Task SuppressResponsibilityAsync(ResponsibilityId responsibilityId, string reason, string actor, TimeSpan duration, CancellationToken cancellationToken = default);
    }

    public interface IResponsibilitySeverityScorer
    {
        decimal CalculateSeverityScore(
            decimal businessImpact,
            decimal urgency,
            double confidence,
            int persistenceBreaches,
            decimal exposureValue);
    }

    public interface IResponsibilityEscalator
    {
        Task<ResponsibilityPriority> EvaluateEscalationAsync(ResponsibilityRecord record, CancellationToken cancellationToken = default);
    }

    public interface IResponsibilityDetector
    {
        Task<IReadOnlyList<ResponsibilityRecord>> DetectResponsibilitiesAsync(AmbientBusinessEvent businessEvent, CancellationToken cancellationToken = default);
    }

    public interface IResponsibilityMissionFactory
    {
        Task<ResponsibilityMissionProposal> CreateProposalAsync(
            ResponsibilityRecord responsibility,
            IReadOnlyList<AmbientBusinessEvent> triggeringEvents,
            AutonomyTier maxTenantAllowedAutonomy,
            bool allowBrainHypothesis = true,
            CancellationToken cancellationToken = default);
    }

    public interface IAmbientWatchdogScheduler
    {
        Task TriggerWatchdogEvaluationAsync(Guid workspaceId, CancellationToken cancellationToken = default);
    }

    public interface IResponsibilityAuditLedger
    {
        Task RecordDetectionAsync(ResponsibilityRecord record, AmbientBusinessEvent businessEvent, CancellationToken cancellationToken = default);
        Task RecordDebounceDecisionAsync(ResponsibilityId responsibilityId, string decision, string reason, CancellationToken cancellationToken = default);
        Task RecordProposalAsync(ResponsibilityMissionProposal proposal, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ResponsibilityMissionProposal>> GetProposalsAsync(Guid workspaceId, CancellationToken cancellationToken = default);
    }
}
