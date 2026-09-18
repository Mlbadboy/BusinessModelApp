using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace BusinessModelApp.Core.Domain.Runtime.Enterprise.Multimodal
{
    public enum MultimodalTrustLevel
    {
        Untrusted = 0,
        PartiallyTrusted = 1,
        TrustedInternal = 2
    }

    public enum InstructionalStatus
    {
        Data = 0,               // Always data, never actionable instruction
        GovernedInstruction = 1 // Only from authenticated internal policy/governance
    }

    public class InjectionDetectionResult
    {
        public bool IsInjectionAttempt { get; set; }
        public MultimodalTrustLevel TrustLevel { get; set; } = MultimodalTrustLevel.Untrusted;
        public InstructionalStatus Status { get; set; } = InstructionalStatus.Data;
        public List<string> MatchedPatterns { get; set; } = new();
        public string SanitizedText { get; set; } = string.Empty;
        public string OriginalText { get; set; } = string.Empty;
    }

    public record RedactedContentResult(string OriginalText, string SanitizedText, int RedactionsCount, List<string> RedactedTypes);

    public enum FileSafetyStatus
    {
        Clean = 0,
        Quarantined = 1,
        RejectedUnsafeMime = 2,
        ExceededSizeLimit = 3
    }

    public class FileSecurityAssessment
    {
        public string FileId { get; set; } = Guid.NewGuid().ToString("N");
        public string FileName { get; set; } = string.Empty;
        public string Sha256Hash { get; set; } = string.Empty;
        public string MimeType { get; set; } = string.Empty;
        public long ByteLength { get; set; }
        public FileSafetyStatus SafetyStatus { get; set; } = FileSafetyStatus.Clean;
        public bool IsQuarantined => SafetyStatus != FileSafetyStatus.Clean;
        public string QuarantinedReason { get; set; } = string.Empty;
    }

    public static class ComputerSafetyPatterns
    {
        private static readonly Regex[] InjectionRegexes = new[]
        {
            new Regex(@"ignore\s+(all\s+)?(previous\s+)?(instructions|rules|policies)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(@"system\s+override", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(@"transfer\s+(\$|₹|rs\.?|inr|usd)?\s*\d+", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(@"email\s+(this\s+)?(document|credentials|passwords?)\s+to", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(@"grant\s+.*?(admin|root|superuser|privilege)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(@"bypass\s+(firewall|governance|policy|auth)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(@"send\s+credentials", RegexOptions.IgnoreCase | RegexOptions.Compiled)
        };

        private static readonly Regex CreditCardRegex = new Regex(@"\b(?:\d{4}[ -]?){3}\d{4}\b", RegexOptions.Compiled);
        private static readonly Regex PasswordKeywordRegex = new Regex(@"(password|passwd|pwd|secret|api_key|token)\s*[:=]\s*([^\s,;]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static InjectionDetectionResult AnalyzePromptInjection(string text)
        {
            var result = new InjectionDetectionResult
            {
                OriginalText = text ?? string.Empty,
                SanitizedText = text ?? string.Empty,
                TrustLevel = MultimodalTrustLevel.Untrusted,
                Status = InstructionalStatus.Data
            };

            if (string.IsNullOrWhiteSpace(text)) return result;

            foreach (var regex in InjectionRegexes)
            {
                if (regex.IsMatch(text))
                {
                    result.IsInjectionAttempt = true;
                    result.MatchedPatterns.Add(regex.ToString());
                }
            }

            if (result.IsInjectionAttempt)
            {
                // Neutralize malicious instruction into inert data
                result.SanitizedText = $"[DATA_UNTRUSTED_CONTENT: {text}]";
            }

            return result;
        }

        public static RedactedContentResult RedactSensitiveContent(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new RedactedContentResult(string.Empty, string.Empty, 0, new List<string>());

            var redacted = text;
            var types = new List<string>();
            var count = 0;

            if (CreditCardRegex.IsMatch(redacted))
            {
                redacted = CreditCardRegex.Replace(redacted, "[REDACTED_PAYMENT_CARD]");
                types.Add("CreditCard");
                count++;
            }

            if (PasswordKeywordRegex.IsMatch(redacted))
            {
                redacted = PasswordKeywordRegex.Replace(redacted, "$1: [REDACTED_SECRET]");
                types.Add("Credential");
                count++;
            }

            return new RedactedContentResult(text, redacted, count, types);
        }
    }
}
