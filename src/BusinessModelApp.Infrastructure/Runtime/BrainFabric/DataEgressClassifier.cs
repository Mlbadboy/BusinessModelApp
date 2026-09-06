using System;
using BusinessModelApp.Core.Domain.Runtime;
using BusinessModelApp.Core.Interfaces.Runtime;

namespace BusinessModelApp.Infrastructure.Runtime.BrainFabric
{
    public class DataEgressClassifier : IDataEgressClassifier
    {
        private static readonly string[] LocalOnlyKeywords = new[]
        {
            "internal_only", "confidential_financial", "pii", "secret_key", "password",
            "social_security", "credit_card", "restricted_local", "patient_health", "hipaa"
        };

        private static readonly string[] PrivateVpcKeywords = new[]
        {
            "enterprise_private", "proprietary_contract", "payroll", "internal_strategy"
        };

        public DataEgressTier ClassifyTaskDataSensitivity(string prompt, string systemPrompt)
        {
            var combined = $"{prompt} {systemPrompt}".ToLowerInvariant();

            foreach (var kw in LocalOnlyKeywords)
            {
                if (combined.Contains(kw))
                    return DataEgressTier.LocalOnly;
            }

            foreach (var kw in PrivateVpcKeywords)
            {
                if (combined.Contains(kw))
                    return DataEgressTier.PrivateVpc;
            }

            return DataEgressTier.PublicCommercialAllowed;
        }
    }
}
