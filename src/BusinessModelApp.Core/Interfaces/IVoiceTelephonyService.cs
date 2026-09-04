using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Commercial;

namespace BusinessModelApp.Core.Interfaces
{
    public class OutboundCallRequest
    {
        public Guid? LeadId { get; set; }
        public Guid WorkspaceId { get; set; }
        public Guid? OrganizationId { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public string ContactName { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string GoalPrompt { get; set; } = string.Empty;
        public bool IsTestCall { get; set; } = false;
        public decimal MaxBudgetINR { get; set; } = 10.0m;
    }

    public class VoiceCallDispatchResult
    {
        public bool Success { get; set; }
        public string ProviderCallId { get; set; } = string.Empty;
        public VoiceCallStatus Status { get; set; } = VoiceCallStatus.PendingGovernance;
        public VoiceProviderType Provider { get; set; }
        public decimal EstimatedCostINR { get; set; } = 0.0m;
        public string? ErrorMessage { get; set; }
        public Guid CallRecordId { get; set; }
    }

    public class VoiceCallStateResult
    {
        public string ProviderCallId { get; set; } = string.Empty;
        public VoiceCallStatus Status { get; set; }
        public int DurationSeconds { get; set; }
        public string? Transcript { get; set; }
        public double? QualityScore { get; set; }
        public string? ExtractedIntent { get; set; }
        public decimal ActualCostINR { get; set; }
        public string? RecordingUrl { get; set; }
        public string? FailureReason { get; set; }
    }

    public class VoiceWebhookProcessingResult
    {
        public bool Success { get; set; }
        public VoiceWebhookProcessingStatus Status { get; set; }
        public string ProviderCallId { get; set; } = string.Empty;
        public VoiceCallStatus ResultingCallStatus { get; set; }
        public decimal ActualCostINR { get; set; }
        public string? SanitizedTranscript { get; set; }
        public double? QualityScore { get; set; }
        public string? IntentSummary { get; set; }
        public string? Message { get; set; }
    }

    public interface IVoiceTelephonyService
    {
        VoiceProviderType ProviderType { get; }

        /// <summary>
        /// Places the outbound call via provider API. Returns DispatchAccepted strictly if provider returns a confirmed ProviderCallId.
        /// </summary>
        Task<VoiceCallDispatchResult> InitiateOutboundCallAsync(OutboundCallRequest request, CancellationToken ct = default);

        /// <summary>
        /// Fetches the latest status of an in-flight or completed call from the provider.
        /// </summary>
        Task<VoiceCallStateResult> GetCallStatusAsync(string providerCallId, CancellationToken ct = default);

        /// <summary>
        /// Processes inbound webhook events idempotently with signature verification and PII sanitization.
        /// </summary>
        Task<VoiceWebhookProcessingResult> ProcessWebhookAsync(string rawPayloadJson, IDictionary<string, string> headers, CancellationToken ct = default);
    }
}
