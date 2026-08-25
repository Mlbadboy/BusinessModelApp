using System;
using System.Text.RegularExpressions;

namespace BusinessModelApp.Core.Observability
{
    public static class TelemetrySanitizer
    {
        private static readonly Regex ApiKeyPattern = new Regex(
            @"(?i)(bearer\s+[a-z0-9_\-\.]+|sk-[a-z0-9_\-]{16,}|ghp_[a-z0-9_\-]{16,}|key-[a-z0-9_\-]{16,}|secret[a-z0-9_\-]*\s*[:=]\s*['""][^'""]+['""])",
            RegexOptions.Compiled);

        private static readonly Regex PasswordPattern = new Regex(
            @"(?i)(password\s*[:=]\s*['""][^'""]+['""]|pwd\s*[:=]\s*['""][^'""]+['""])",
            RegexOptions.Compiled);

        private static readonly Regex ConnectionStringPattern = new Regex(
            @"(?i)(Server=[^;]+;|Data Source=[^;]+;|User Id=[^;]+;|Password=[^;]+;)",
            RegexOptions.Compiled);

        private static readonly Regex JwtPattern = new Regex(
            @"eyJ[a-zA-Z0-9_-]{10,}\.eyJ[a-zA-Z0-9_-]{10,}\.[a-zA-Z0-9_-]{10,}",
            RegexOptions.Compiled);

        /// <summary>
        /// Sanitizes arbitrary text before exposing to logs, metrics, or telemetry spans.
        /// </summary>
        public static string Sanitize(string? input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            var sanitized = input;
            sanitized = JwtPattern.Replace(sanitized, "[REDACTED_JWT]");
            sanitized = ApiKeyPattern.Replace(sanitized, "[REDACTED_API_KEY]");
            sanitized = PasswordPattern.Replace(sanitized, "password=[REDACTED]");
            sanitized = ConnectionStringPattern.Replace(sanitized, "[REDACTED_CONN_STR];");

            return sanitized;
        }

        /// <summary>
        /// Checks if a string contains any sensitive token or secret.
        /// </summary>
        public static bool ContainsSecret(string? input)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            return JwtPattern.IsMatch(input) ||
                   ApiKeyPattern.IsMatch(input) ||
                   PasswordPattern.IsMatch(input) ||
                   ConnectionStringPattern.IsMatch(input);
        }
    }
}
