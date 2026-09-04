using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusinessModelApp.Core.Domain.Commercial
{
    public enum VoiceCallStatus
    {
        PendingGovernance = 0,
        DispatchAccepted = 1,
        CallRinging = 2,
        CallInProgress = 3,
        CallCompleted = 4,
        WebhookVerified = 5,
        TranscriptPersisted = 6,
        Failed = 7
    }

    public enum VoiceProviderType
    {
        Simulation = 0,
        Vapi = 1,
        Retell = 2,
        Twilio = 3
    }

    public enum VoiceWebhookProcessingStatus
    {
        Received = 0,
        Processed = 1,
        DuplicateIgnored = 2,
        SignatureFailed = 3,
        Failed = 4
    }

    /// <summary>
    /// Immutable, durable ledger of all voice interactions and state transitions.
    /// </summary>
    public class VoiceCallRecord
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid? LeadId { get; set; }
        public Lead? Lead { get; set; }

        [Required]
        public Guid WorkspaceId { get; set; }

        public Guid? OrganizationId { get; set; }

        [Required]
        public VoiceProviderType Provider { get; set; } = VoiceProviderType.Simulation;

        /// <summary>
        /// Unique provider call SID / identifier. Must have a UNIQUE database constraint.
        /// </summary>
        [Required]
        [MaxLength(256)]
        public string ProviderCallId { get; set; } = string.Empty;

        [MaxLength(128)]
        public string PhoneNumberHash { get; set; } = string.Empty;

        [MaxLength(200)]
        public string ContactName { get; set; } = string.Empty;

        [MaxLength(200)]
        public string CompanyName { get; set; } = string.Empty;

        public VoiceCallStatus Status { get; set; } = VoiceCallStatus.PendingGovernance;

        [MaxLength(500)]
        public string? FailureReason { get; set; }

        public DateTime? StartedAt { get; set; }
        public DateTime? AnsweredAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        public int DurationSeconds { get; set; } = 0;

        /// <summary>
        /// Sanitized, PII-minimized transcript of the conversation.
        /// </summary>
        public string Transcript { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? ExtractedIntent { get; set; }

        public double? QualityScore { get; set; }

        [MaxLength(50)]
        public string? Sentiment { get; set; }

        [MaxLength(1000)]
        public string? RecordingUrl { get; set; }

        public bool RecordingAvailable { get; set; } = false;

        [Column(TypeName = "decimal(18,4)")]
        public decimal EstimatedCostINR { get; set; } = 0.0m;

        [Column(TypeName = "decimal(18,4)")]
        public decimal ActualCostINR { get; set; } = 0.0m;

        public bool WebhookVerified { get; set; } = false;
        public DateTime? WebhookReceivedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Webhook idempotency record to guarantee single-processing semantics on duplicate/retried events.
    /// </summary>
    public class VoiceWebhookEvent
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public VoiceProviderType Provider { get; set; }

        [Required]
        [MaxLength(256)]
        public string EventId { get; set; } = string.Empty;

        [Required]
        [MaxLength(256)]
        public string ProviderCallId { get; set; } = string.Empty;

        [MaxLength(100)]
        public string EventType { get; set; } = string.Empty;

        [MaxLength(128)]
        public string PayloadHash { get; set; } = string.Empty;

        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessedAt { get; set; }

        public VoiceWebhookProcessingStatus ProcessingStatus { get; set; } = VoiceWebhookProcessingStatus.Received;
    }
}
