using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace BusinessModelApp.Core.AI.Governance
{
    public interface IAIDataMinimizationService
    {
        string SanitizeContent(string content, AITaskType taskType);
        List<AIMessage> SanitizeMessages(List<AIMessage> messages, AITaskType taskType);
    }

    public class AIDataMinimizationService : IAIDataMinimizationService
    {
        // Universal Secret Patterns (Never Allowed)
        private static readonly Regex ApiKeyPattern = new Regex(@"(?:api[_-]?key|bearer|token|secret)\s*[:=]\s*['""]?([a-zA-Z0-9_\-\.]{16,})['""]?", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex PasswordPattern = new Regex(@"(?:password|pwd|pass)\s*[:=]\s*['""]?([^\s'""]{4,})['""]?", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex JwtPattern = new Regex(@"eyJ[a-zA-Z0-9_\-]+\.eyJ[a-zA-Z0-9_\-]+\.[a-zA-Z0-9_\-]+", RegexOptions.Compiled);
        private static readonly Regex CreditCardPattern = new Regex(@"\b(?:\d{4}[-\s]?){3}\d{4}\b", RegexOptions.Compiled);

        // PII Patterns (Task-governed)
        private static readonly Regex EmailPattern = new Regex(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b", RegexOptions.Compiled);
        private static readonly Regex PhonePattern = new Regex(@"\b(?:\+?\d{1,3}[-.\s]?)?\(?\d{3}\)?[-.\s]?\d{3}[-.\s]?\d{4}\b", RegexOptions.Compiled);

        public List<AIMessage> SanitizeMessages(List<AIMessage> messages, AITaskType taskType)
        {
            if (messages == null) return new List<AIMessage>();

            var sanitizedList = new List<AIMessage>(messages.Count);
            foreach (var msg in messages)
            {
                sanitizedList.Add(new AIMessage
                {
                    Role = msg.Role,
                    Content = SanitizeContent(msg.Content, taskType)
                });
            }
            return sanitizedList;
        }

        public string SanitizeContent(string content, AITaskType taskType)
        {
            if (string.IsNullOrWhiteSpace(content)) return string.Empty;

            var sanitized = content;

            // 1. Mandatory Universal Redactions across all task types
            sanitized = ApiKeyPattern.Replace(sanitized, "[REDACTED_API_KEY]");
            sanitized = PasswordPattern.Replace(sanitized, "password: [REDACTED_PASSWORD]");
            sanitized = JwtPattern.Replace(sanitized, "[REDACTED_JWT_TOKEN]");
            sanitized = CreditCardPattern.Replace(sanitized, "[REDACTED_CREDIT_CARD]");

            // 2. Task-Specific PII Governance
            switch (taskType)
            {
                case AITaskType.GeneralAssistant:
                case AITaskType.Classification:
                    // Strict: Redact emails and phone numbers
                    sanitized = EmailPattern.Replace(sanitized, "[REDACTED_EMAIL]");
                    sanitized = PhonePattern.Replace(sanitized, "[REDACTED_PHONE]");
                    break;

                case AITaskType.ExecutiveBrief:
                case AITaskType.BusinessHealthExplanation:
                    // Executive layer: Preserve metric facts and evidence IDs; redact raw individual contact numbers
                    sanitized = PhonePattern.Replace(sanitized, "[REDACTED_PHONE]");
                    break;

                case AITaskType.LeadQualification:
                case AITaskType.VoiceQualification:
                case AITaskType.OpportunityAnalysis:
                    // Commercial touchpoints: Allow business contact info (Name, Email, Phone) for lead qualification
                    break;

                default:
                    break;
            }

            return sanitized;
        }
    }
}
