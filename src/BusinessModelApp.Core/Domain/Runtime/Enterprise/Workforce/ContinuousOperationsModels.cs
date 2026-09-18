using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.Domain.Runtime.Enterprise.Workforce
{
    public enum BusinessCycleTriggerType
    {
        SCHEDULED_TIMER = 1,
        CRM_EVENT = 2,
        INBOUND_EMAIL = 3,
        INBOUND_LEAD = 4,
        PAYMENT_RECEIVED = 5,
        INVOICE_DUE = 6,
        WATCHTOWER_ALERT = 7,
        MANUAL_USER_TRIGGER = 8
    }

    public class BusinessCycleTrigger
    {
        public string TriggerId { get; set; } = Guid.NewGuid().ToString("N");
        public string TenantId { get; set; } = string.Empty;
        public BusinessCycleTriggerType TriggerType { get; set; } = BusinessCycleTriggerType.SCHEDULED_TIMER;
        public string SourcePayloadJson { get; set; } = "{}";
        public DateTime TriggeredAt { get; set; } = DateTime.UtcNow;
    }

    public class ContinuousBusinessCycle
    {
        public string CycleId { get; set; } = Guid.NewGuid().ToString("N");
        public string TenantId { get; set; } = string.Empty;
        public string TriggerId { get; set; } = string.Empty;
        public long SequenceNumber { get; set; }
        public string BrainSnapshotHash { get; set; } = string.Empty;
        public string PolicySnapshotHash { get; set; } = string.Empty;
        public decimal AllocatedBudget { get; set; } = 100m;
        public decimal ActualSpent { get; set; } = 0m;
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime Deadline { get; set; } = DateTime.UtcNow.AddMinutes(15);
        public string CheckpointState { get; set; } = "INITIALIZED"; // INITIALIZED, REASONING, PROPOSING, EXECUTING, COMPLETED, FAILED
        public string OutcomeSummary { get; set; } = string.Empty;
        public string IdempotencyKey { get; set; } = string.Empty;
        public bool IsCompleted => CheckpointState == "COMPLETED" || CheckpointState == "FAILED";
        public bool IsTimedOut => DateTime.UtcNow > Deadline && !IsCompleted;
    }
}
