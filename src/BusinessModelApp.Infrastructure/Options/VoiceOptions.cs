namespace BusinessModelApp.Infrastructure.Options
{
    public class VoiceOptions
    {
        public const string SectionName = "Voice";

        public string Provider { get; set; } = "Simulation"; // Simulation | Vapi | Retell | Twilio
        public VapiOptions Vapi { get; set; } = new();
        public RetellOptions Retell { get; set; } = new();
        public TwilioOptions Twilio { get; set; } = new();
        public bool EnablePiiMinimization { get; set; } = true;
        public decimal MaxTestCallBudgetINR { get; set; } = 10.0m;
    }

    public class VapiOptions
    {
        public string ApiKey { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "https://api.vapi.ai";
        public string PhoneNumberId { get; set; } = string.Empty;
        public string AssistantId { get; set; } = string.Empty;
        public string WebhookSecret { get; set; } = string.Empty;
    }

    public class RetellOptions
    {
        public string ApiKey { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "https://api.retellai.com";
        public string AgentId { get; set; } = string.Empty;
        public string FromNumber { get; set; } = string.Empty;
        public string WebhookSecret { get; set; } = string.Empty;
    }

    public class TwilioOptions
    {
        public string AccountSid { get; set; } = string.Empty;
        public string AuthToken { get; set; } = string.Empty;
        public string FromNumber { get; set; } = string.Empty;
        public string TwiMLAppSid { get; set; } = string.Empty;
        public string WebhookSecret { get; set; } = string.Empty;
    }
}
