using System;
using System.Threading.Tasks;
using BusinessModelApp.Core.AI;
using BusinessModelApp.Core.AI.Governance;
using BusinessModelApp.Infrastructure.Data;
using BusinessModelApp.Infrastructure.Interceptors;
using BusinessModelApp.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BusinessModelApp.Tests.AI
{
    public class ApprovalEngineTests
    {
        private AppDbContext CreateDbContext()
        {
            var interceptor = new AppendOnlyAuditInterceptor();
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .AddInterceptors(interceptor)
                .Options;

            return new AppDbContext(options);
        }

        [Fact]
        public void ApprovalService_ShouldEvaluateRiskLevelsCorrectly()
        {
            var service = new ApprovalService(CreateDbContext(), NullLogger<ApprovalService>.Instance);

            service.EvaluateTaskRiskLevel(AITaskType.ExecutiveBrief).Should().Be(ApprovalRiskLevel.Low);
            service.EvaluateTaskRiskLevel(AITaskType.BusinessHealthExplanation).Should().Be(ApprovalRiskLevel.Low);
            service.EvaluateTaskRiskLevel(AITaskType.OpportunityAnalysis).Should().Be(ApprovalRiskLevel.Medium);
            service.EvaluateTaskRiskLevel(AITaskType.LeadQualification).Should().Be(ApprovalRiskLevel.Medium);
            service.EvaluateTaskRiskLevel(AITaskType.VoiceQualification).Should().Be(ApprovalRiskLevel.High);
        }

        [Fact]
        public async Task ApprovalService_ShouldSubmitAndDecideApprovalWorkflow()
        {
            // Arrange
            using var context = CreateDbContext();
            var service = new ApprovalService(context, NullLogger<ApprovalService>.Instance);
            var orgId = Guid.NewGuid();
            var wsId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            // Act 1: Submit Medium Risk Request
            var request = await service.SubmitApprovalRequestAsync(
                orgId,
                wsId,
                userId,
                "Commercial Agent",
                "DealRiskAnalysis",
                "Review Opportunity Next Steps",
                "{\"oppId\": \"123\", \"risk\": \"High\"}",
                ApprovalRiskLevel.Medium);

            request.Should().NotBeNull();
            request.Status.Should().Be(ApprovalStatus.Pending);

            // Act 2: Decide Approval
            var deciderId = Guid.NewGuid();
            var decided = await service.DecideApprovalRequestAsync(
                request.Id,
                deciderId,
                "Executive Manager",
                true,
                "Approved after customer review");

            // Assert
            decided.Status.Should().Be(ApprovalStatus.Approved);
            decided.DecidedByName.Should().Be("Executive Manager");
            decided.DecidedAt.Should().NotBeNull();
            decided.DecisionNote.Should().Be("Approved after customer review");
        }
    }
}
