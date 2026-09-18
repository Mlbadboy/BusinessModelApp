using System;
using System.Collections.Generic;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Growth;

namespace BusinessModelApp.Core.Domain.Runtime.Enterprise.Acquisition
{
    public enum OutreachDeliveryStatus
    {
        Draft = 0,
        GovernedPermitPending = 1,
        PermitApproved = 2,
        Sent = 3,
        Delivered = 4,
        Failed = 5,
        SuppressedSpamGuard = 6,
        SuppressedFrequencyLimit = 7
    }

    public sealed class OutreachCampaignPolicy
    {
        public string PolicyId { get; init; } = Guid.NewGuid().ToString("N");
        public required string TenantId { get; init; }
        public int MaxTouchesPerStakeholderPerWeek { get; init; } = 1;
        public int MinDaysBetweenTouches { get; init; } = 7;
        public bool AntiSpamComplianceMandatory { get; init; } = true;
        public decimal MaxMonthlyAcquisitionBudgetINR { get; init; } = 200_000m;
        public decimal CurrentMonthSpendINR { get; private set; }

        public bool EvaluateOutreachAllowed(DateTime? lastContactedUtc, out string suppressionReason)
        {
            if (lastContactedUtc.HasValue)
            {
                var daysSince = (DateTime.UtcNow - lastContactedUtc.Value).TotalDays;
                if (daysSince < MinDaysBetweenTouches)
                {
                    suppressionReason = $"Suppressed: Minimum contact interval ({MinDaysBetweenTouches} days) not met (last contacted {daysSince:F1} days ago).";
                    return false;
                }
            }

            suppressionReason = string.Empty;
            return true;
        }

        public void RecordSpend(decimal spendINR)
        {
            CurrentMonthSpendINR += spendINR;
        }
    }

    public sealed class GovernedOutreachTask
    {
        public string TaskId { get; init; } = Guid.NewGuid().ToString("N");
        public required string TenantId { get; init; }
        public required string ProspectId { get; init; }
        public required string ContactEmail { get; init; }
        public required string SubjectLine { get; init; }
        public required string MessageContent { get; init; }
        public OutreachDeliveryStatus DeliveryStatus { get; private set; } = OutreachDeliveryStatus.Draft;
        public string Batch6PermitId { get; private set; } = string.Empty;
        public string ExternalProviderReference { get; private set; } = string.Empty;
        public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
        public DateTime? SentAtUtc { get; private set; }
        public string SuppressionReason { get; private set; } = string.Empty;

        public void AuthorizePermit(string permitId)
        {
            if (string.IsNullOrWhiteSpace(permitId)) throw new ArgumentException("Batch 6 permit ID required.", nameof(permitId));
            Batch6PermitId = permitId.Trim();
            DeliveryStatus = OutreachDeliveryStatus.PermitApproved;
        }

        public void MarkSent(string providerReference)
        {
            DeliveryStatus = OutreachDeliveryStatus.Sent;
            ExternalProviderReference = providerReference.Trim();
            SentAtUtc = DateTime.UtcNow;
        }

        public void Suppress(OutreachDeliveryStatus reason, string notes)
        {
            DeliveryStatus = reason;
            SuppressionReason = notes;
        }
    }
}
