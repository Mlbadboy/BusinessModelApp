using System;
using BusinessModelApp.Core.Domain.Common;

namespace BusinessModelApp.Core.Domain.Commercial
{
    public enum InteractionChannel
    {
        VoiceCall = 0,
        WhatsApp = 1,
        Email = 2,
        WebChat = 3,
        Meeting = 4,
        Note = 5
    }

    public class Interaction : Entity
    {
        public Guid LeadId { get; set; }
        public InteractionChannel Channel { get; set; } = InteractionChannel.VoiceCall;
        public string Summary { get; set; } = string.Empty;
        public string TranscriptOrBody { get; set; } = string.Empty;
        public string Sentiment { get; set; } = "Neutral"; // Positive, Neutral, Negative
        public double AIQualificationConfidence { get; set; } = 0.0; // 0-100
        public string ExtractedIntent { get; set; } = string.Empty;
        public int DurationSeconds { get; set; } = 0;
        public Guid? HandledByAgentOrUserId { get; set; }

        // Navigation
        public virtual Lead Lead { get; set; }
    }
}
