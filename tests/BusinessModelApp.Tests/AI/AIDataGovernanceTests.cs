using BusinessModelApp.Core.AI;
using BusinessModelApp.Core.AI.Governance;
using FluentAssertions;
using Xunit;

namespace BusinessModelApp.Tests.AI
{
    public class AIDataGovernanceTests
    {
        private readonly AIDataMinimizationService _minimizer = new AIDataMinimizationService();

        [Fact]
        public void DataMinimizer_ShouldScrubSecretsAndCredentialsAcrossAllTasks()
        {
            // Arrange
            var input = "Connect with api_key: 'sk-proj-1234567890abcdef123456' and password: 'SecretPassword123' token eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.dozjgN54.";

            // Act
            var sanitizedBrief = _minimizer.SanitizeContent(input, AITaskType.ExecutiveBrief);
            var sanitizedLead = _minimizer.SanitizeContent(input, AITaskType.LeadQualification);
            var sanitizedGeneral = _minimizer.SanitizeContent(input, AITaskType.GeneralAssistant);

            // Assert
            sanitizedBrief.Should().NotContain("sk-proj-1234567890abcdef123456");
            sanitizedBrief.Should().NotContain("SecretPassword123");
            sanitizedBrief.Should().Contain("[REDACTED_API_KEY]");
            sanitizedBrief.Should().Contain("[REDACTED_PASSWORD]");
            sanitizedBrief.Should().Contain("[REDACTED_JWT_TOKEN]");

            sanitizedLead.Should().NotContain("sk-proj-1234567890abcdef123456");
            sanitizedGeneral.Should().NotContain("SecretPassword123");
        }

        [Fact]
        public void DataMinimizer_ShouldApplyTaskSpecificPiiPolicies()
        {
            // Arrange: Commercial lead inquiry with email and phone
            var prompt = "Contact john.doe@enterprise.com at +1-555-123-4567 regarding enterprise contract. Financial evidence: EVD-PIPE-01 coverage 2.4x.";

            // Act 1: LeadQualification allows contact details for qualification
            var leadSanitized = _minimizer.SanitizeContent(prompt, AITaskType.LeadQualification);
            leadSanitized.Should().Contain("john.doe@enterprise.com");
            leadSanitized.Should().Contain("+1-555-123-4567");
            leadSanitized.Should().Contain("EVD-PIPE-01");

            // Act 2: GeneralAssistant redacts emails and phone numbers
            var generalSanitized = _minimizer.SanitizeContent(prompt, AITaskType.GeneralAssistant);
            generalSanitized.Should().NotContain("john.doe@enterprise.com");
            generalSanitized.Should().Contain("[REDACTED_EMAIL]");
            generalSanitized.Should().Contain("[REDACTED_PHONE]");
            generalSanitized.Should().Contain("EVD-PIPE-01"); // Metric evidence preserved
        }
    }
}
