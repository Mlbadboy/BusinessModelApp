using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Responsibilities;
using BusinessModelApp.Core.Interfaces.Ambient;

namespace BusinessModelApp.Infrastructure.Runtime.Ambient
{
    public class ResponsibilityDetectionEngine : IResponsibilityDetector
    {
        private readonly IResponsibilityEvidenceGate _evidenceGate;
        private readonly IResponsibilityRegistry _registry;
        private readonly IResponsibilityDebouncer _debouncer;
        private readonly IResponsibilitySeverityScorer _severityScorer;
        private readonly IResponsibilityEscalator _escalator;
        private readonly IResponsibilityAuditLedger _auditLedger;

        public ResponsibilityDetectionEngine(
            IResponsibilityEvidenceGate evidenceGate,
            IResponsibilityRegistry registry,
            IResponsibilityDebouncer debouncer,
            IResponsibilitySeverityScorer severityScorer,
            IResponsibilityEscalator escalator,
            IResponsibilityAuditLedger auditLedger)
        {
            _evidenceGate = evidenceGate ?? throw new ArgumentNullException(nameof(evidenceGate));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _debouncer = debouncer ?? throw new ArgumentNullException(nameof(debouncer));
            _severityScorer = severityScorer ?? throw new ArgumentNullException(nameof(severityScorer));
            _escalator = escalator ?? throw new ArgumentNullException(nameof(escalator));
            _auditLedger = auditLedger ?? throw new ArgumentNullException(nameof(auditLedger));
        }

        public async Task<IReadOnlyList<ResponsibilityRecord>> DetectResponsibilitiesAsync(AmbientBusinessEvent businessEvent, CancellationToken cancellationToken = default)
        {
            if (businessEvent == null) throw new ArgumentNullException(nameof(businessEvent));

            // 1. Evidence / Truth Gate (Signal != Evidence != Truth)
            var assessment = await _evidenceGate.EvaluateSignalAsync(businessEvent, cancellationToken);
            if (!assessment.IsCorroborated)
            {
                // Signal is weak, contradicted, or poisoned; reject from responsibility promotion
                return Array.Empty<ResponsibilityRecord>();
            }

            var triggeredList = new List<ResponsibilityRecord>();
            var definitions = await _registry.GetRegisteredDefinitionsAsync(businessEvent.WorkspaceId, cancellationToken);

            foreach (var (template, trigger) in definitions)
            {
                // Check if trigger metric matches event
                if (!string.Equals(trigger.MetricName, businessEvent.EventType, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(trigger.MetricName, businessEvent.Source, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Check condition
                var isBreached = EvaluateBreach(trigger.Operator, businessEvent.MetricValue, trigger.ThresholdValue);
                var isRecovered = EvaluateBreach(InvertOperator(trigger.Operator), businessEvent.MetricValue, trigger.RecoveryThresholdValue);

                var deterministicId = _registry.ComputeDeterministicId(
                    businessEvent.WorkspaceId,
                    template.DefinitionId,
                    businessEvent.EntityType,
                    trigger.TriggerId);

                var activeRecord = await _registry.GetActiveResponsibilityAsync(deterministicId, cancellationToken);

                if (isBreached)
                {
                    if (activeRecord == null)
                    {
                        activeRecord = template with
                        {
                            Id = deterministicId,
                            EntityScope = businessEvent.EntityType,
                            State = ResponsibilityLifecycleState.Detected,
                            CorroboratedEvidenceIds = new List<Guid> { businessEvent.EventId }
                        };
                    }

                    // 2. Debounce, Cooldown & Suppression Check
                    var shouldSuppress = await _debouncer.ShouldSuppressAsync(activeRecord, businessEvent, cancellationToken);
                    if (shouldSuppress)
                    {
                        await _auditLedger.RecordDebounceDecisionAsync(activeRecord.Id, "Suppressed", "Within cooldown or active suppression", cancellationToken);
                        continue;
                    }

                    // 3. Record trigger & persistence breach
                    await _debouncer.RecordTriggerAsync(activeRecord, businessEvent, cancellationToken);

                    // 4. Severity Scoring (Deterministic formula)
                    activeRecord.SeverityScore = _severityScorer.CalculateSeverityScore(
                        businessImpact: (int)activeRecord.Priority + 1,
                        urgency: 2.0m,
                        confidence: assessment.TrustScore,
                        persistenceBreaches: activeRecord.ConsecutiveBreaches,
                        exposureValue: activeRecord.RiskCeiling);

                    // 5. Priority Escalation
                    activeRecord.Priority = await _escalator.EvaluateEscalationAsync(activeRecord, cancellationToken);

                    await _registry.SaveActiveResponsibilityAsync(activeRecord, cancellationToken);
                    await _auditLedger.RecordDetectionAsync(activeRecord, businessEvent, cancellationToken);

                    triggeredList.Add(activeRecord);
                }
                else if (isRecovered && activeRecord != null && activeRecord.State == ResponsibilityLifecycleState.Active)
                {
                    // Hysteresis recovery evaluation
                    await _debouncer.CheckRecoveryAsync(activeRecord, businessEvent.MetricValue, trigger.RecoveryThresholdValue, cancellationToken);
                }
            }

            return triggeredList;
        }

        private static bool EvaluateBreach(ComparisonOperator op, decimal metric, decimal threshold)
        {
            return op switch
            {
                ComparisonOperator.LessThan => metric < threshold,
                ComparisonOperator.GreaterThan => metric > threshold,
                ComparisonOperator.Equals => metric == threshold,
                ComparisonOperator.PercentageDrop => metric < threshold,
                ComparisonOperator.PercentageIncrease => metric > threshold,
                _ => false
            };
        }

        private static ComparisonOperator InvertOperator(ComparisonOperator op)
        {
            return op switch
            {
                ComparisonOperator.LessThan => ComparisonOperator.GreaterThan,
                ComparisonOperator.GreaterThan => ComparisonOperator.LessThan,
                ComparisonOperator.PercentageDrop => ComparisonOperator.GreaterThan,
                ComparisonOperator.PercentageIncrease => ComparisonOperator.LessThan,
                _ => ComparisonOperator.Equals
            };
        }
    }
}
