using System;
using System.Collections.Generic;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Growth;

namespace BusinessModelApp.Core.Domain.Runtime.Enterprise.Delivery
{
    public enum DeliveryProjectStatus
    {
        Initiated = 0,
        Onboarding = 1,
        InProgress = 2,
        DeliveredPendingAcceptance = 3,
        DeliveryAccepted = 4,
        ValueRealizationUnderway = 5,
        ValueRealized = 6,
        Failed = 7
    }

    public enum MilestoneStatus
    {
        Pending = 0,
        InProgress = 1,
        Completed = 2,
        Verified = 3,
        Blocked = 4
    }

    public sealed class DeliveryMilestone
    {
        public string MilestoneId { get; init; } = Guid.NewGuid().ToString("N");
        public required string Title { get; init; }
        public DateTime TargetDateUtc { get; init; }
        public DateTime? CompletedDateUtc { get; private set; }
        public MilestoneStatus Status { get; private set; } = MilestoneStatus.Pending;
        public string DeliverableArtifactSha256 { get; private set; } = string.Empty;

        public void Complete(string artifactSha256)
        {
            if (string.IsNullOrWhiteSpace(artifactSha256))
                throw new ArgumentException("Artifact SHA256 digest is required for milestone completion.", nameof(artifactSha256));

            DeliverableArtifactSha256 = artifactSha256.Trim().ToLowerInvariant();
            CompletedDateUtc = DateTime.UtcNow;
            Status = MilestoneStatus.Completed;
        }
    }

    public sealed class ValueRealizationMetric
    {
        public string MetricId { get; init; } = Guid.NewGuid().ToString("N");
        public required string ProjectId { get; init; }
        public required string MetricName { get; init; }
        public decimal BaselineValue { get; init; }
        public decimal TargetValue { get; init; }
        public decimal MeasuredValue { get; private set; }
        public required string Unit { get; init; }
        public EpistemicEvidenceLevel EvidenceLevel { get; private set; } = EpistemicEvidenceLevel.InternalFixture;
        public string EvidenceProofSha256 { get; private set; } = string.Empty;
        public DateTime? RealizedAtUtc { get; private set; }

        public void RecordMeasurement(decimal measuredValue, EpistemicEvidenceLevel level, string proofSha256)
        {
            if (level < EpistemicEvidenceLevel.ConnectorObserved)
            {
                throw new InvalidOperationException("Value realization requires at least ConnectorObserved or CounterpartyAttested evidence (Law I40 & I41).");
            }

            if (string.IsNullOrWhiteSpace(proofSha256) || proofSha256.Length != 64)
            {
                throw new ArgumentException("Proof SHA256 must be a 64-character hex string.", nameof(proofSha256));
            }

            MeasuredValue = measuredValue;
            EvidenceLevel = level;
            EvidenceProofSha256 = proofSha256.Trim().ToLowerInvariant();
            RealizedAtUtc = DateTime.UtcNow;
        }
    }

    public sealed class DeliveryProject
    {
        public string ProjectId { get; init; } = Guid.NewGuid().ToString("N");
        public required string TenantId { get; init; }
        public required string CustomerId { get; init; }
        public required string ContractId { get; init; }
        public DateTime StartDateUtc { get; init; } = DateTime.UtcNow;
        public DateTime TargetCompletionUtc { get; init; }
        public DateTime? ActualCompletionUtc { get; private set; }
        public DeliveryProjectStatus Status { get; private set; } = DeliveryProjectStatus.Initiated;
        public List<DeliveryMilestone> Milestones { get; init; } = new();
        public List<ValueRealizationMetric> ValueMetrics { get; init; } = new();
        public string CounterpartySignoffSha256 { get; private set; } = string.Empty;
        public double? TimeToValueDays => ActualCompletionUtc.HasValue ? (ActualCompletionUtc.Value - StartDateUtc).TotalDays : null;

        public void AdvanceToOnboarding()
        {
            if (Status != DeliveryProjectStatus.Initiated)
                throw new InvalidOperationException($"Cannot advance to Onboarding from status '{Status}'.");
            Status = DeliveryProjectStatus.Onboarding;
        }

        public void AdvanceToInProgress()
        {
            if (Status != DeliveryProjectStatus.Onboarding)
                throw new InvalidOperationException($"Cannot advance to InProgress from status '{Status}'.");
            Status = DeliveryProjectStatus.InProgress;
        }

        public void RequestAcceptance()
        {
            if (Status != DeliveryProjectStatus.InProgress)
                throw new InvalidOperationException($"Cannot request acceptance from status '{Status}'.");

            // Verify all milestones are completed
            if (Milestones.Count == 0 || Milestones.Exists(m => m.Status != MilestoneStatus.Completed))
                throw new InvalidOperationException("All milestones must be completed before requesting delivery acceptance.");

            Status = DeliveryProjectStatus.DeliveredPendingAcceptance;
        }

        public void RecordAcceptance(string signoffSha256)
        {
            if (Status != DeliveryProjectStatus.DeliveredPendingAcceptance)
                throw new InvalidOperationException($"Cannot record acceptance from status '{Status}'.");

            if (string.IsNullOrWhiteSpace(signoffSha256) || signoffSha256.Length != 64)
                throw new ArgumentException("Signoff SHA256 must be a valid 64-character hash.", nameof(signoffSha256));

            CounterpartySignoffSha256 = signoffSha256.Trim().ToLowerInvariant();
            Status = DeliveryProjectStatus.DeliveryAccepted;
        }

        public void BeginValueRealization()
        {
            if (Status != DeliveryProjectStatus.DeliveryAccepted)
                throw new InvalidOperationException($"Value realization requires prior DeliveryAccepted status. Current: '{Status}'.");

            Status = DeliveryProjectStatus.ValueRealizationUnderway;
        }

        public void ConfirmValueRealization()
        {
            if (Status != DeliveryProjectStatus.ValueRealizationUnderway)
                throw new InvalidOperationException($"Cannot confirm value realization without active ValueRealizationUnderway status. Current: '{Status}'.");

            if (ValueMetrics.Count == 0 || ValueMetrics.Exists(v => v.RealizedAtUtc == null))
                throw new InvalidOperationException("All required value metrics must have verified L3+ measurements before certifying ValueRealized.");

            ActualCompletionUtc = DateTime.UtcNow;
            Status = DeliveryProjectStatus.ValueRealized;
        }
    }
}
