using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.Domain.Runtime.Enterprise.Commercial
{
    public enum InboundMessageClassification
    {
        UNKNOWN = 0,
        INTERESTED = 1,
        MEETING_REQUEST = 2,
        PRICING_QUESTION = 3,
        OBJECTION = 4,
        NEED_INFORMATION = 5,
        NEGOTIATION = 6,
        PROCUREMENT = 7,
        CONTRACT = 8,
        NOT_INTERESTED = 9,
        SPAM = 10,
        PROMPT_INJECTION = 11
    }

    public class InboundMessageEvent
    {
        public string MessageId { get; set; } = Guid.NewGuid().ToString("N");
        public string TenantId { get; set; } = string.Empty;
        public string OpportunityId { get; set; } = string.Empty;
        public string SenderAddress { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string RawBody { get; set; } = string.Empty;
        public string SanitizedBody { get; set; } = string.Empty;
        public bool IsPromptInjectionDetected { get; set; } = false;
        public List<string> InjectionThreatIndicators { get; set; } = new();
        public InboundMessageClassification Classification { get; set; } = InboundMessageClassification.UNKNOWN;
        public List<string> ExtractedRequirements { get; set; } = new();
        public decimal ClassificationConfidence { get; set; } = 0.5m;
        public DateTime ReceivedAtUtc { get; set; } = DateTime.UtcNow;
    }

    public class MeetingBrief
    {
        public string BriefId { get; set; } = Guid.NewGuid().ToString("N");
        public string TenantId { get; set; } = string.Empty;
        public string OpportunityId { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string AccountSummary { get; set; } = string.Empty;
        public List<string> BuyingCenterAttendees { get; set; } = new();
        public List<string> KeyPainPoints { get; set; } = new();
        public List<string> AnticipatedObjections { get; set; } = new();
        public string ProposedCommercialStrategy { get; set; } = string.Empty;
        public DateTime MeetingScheduledAtUtc { get; set; } = DateTime.UtcNow;
    }

    public class MeetingTranscriptAnalysis
    {
        public string AnalysisId { get; set; } = Guid.NewGuid().ToString("N");
        public string TenantId { get; set; } = string.Empty;
        public string MeetingId { get; set; } = string.Empty;
        public string OpportunityId { get; set; } = string.Empty;
        public string RawTranscript { get; set; } = string.Empty;
        public List<string> DiscoveredRequirements { get; set; } = new();
        public List<string> ExplicitCustomerCommitments { get; set; } = new();
        public List<string> ActionItems { get; set; } = new();
        public bool HasCommercialOptimism { get; set; } = false;
        public bool IsLegallyBindingCommitment => false; // Conversational optimism != won deal (Law I40-D)
        public DateTime AnalyzedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
