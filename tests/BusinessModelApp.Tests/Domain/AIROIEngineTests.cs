using System;
using System.Collections.Generic;
using BusinessModelApp.Core.AI;
using BusinessModelApp.Core.Domain.Commercial;
using BusinessModelApp.Core.Services;
using FluentAssertions;
using Xunit;

namespace BusinessModelApp.Tests.Domain
{
    public class AIROIEngineTests
    {
        private readonly AIROIService _roiService = new AIROIService();

        [Fact]
        public void CalculateDeterministicRoi_ShouldComputeExactAttributedRoi()
        {
            // Arrange
            var workspaceId = Guid.NewGuid();
            var leadId = Guid.NewGuid();
            var oppId = Guid.NewGuid();

            var wonOpp = new Opportunity
            {
                Id = oppId,
                WorkspaceId = workspaceId,
                LeadId = leadId,
                Title = "Acme Enterprise License",
                EstimatedValue = 1000000m,
                Stage = OpportunityStage.ClosedWon
            };

            var aiCallLead = new AICallRecord
            {
                Id = Guid.NewGuid(),
                WorkspaceId = workspaceId,
                LeadId = leadId,
                TaskType = AITaskType.LeadQualification,
                EstimatedCost = 0.10m
            };

            var aiCallOpp = new AICallRecord
            {
                Id = Guid.NewGuid(),
                WorkspaceId = workspaceId,
                OpportunityId = oppId,
                TaskType = AITaskType.OpportunityAnalysis,
                EstimatedCost = 0.40m
            };

            var aiCallUnrelated = new AICallRecord
            {
                Id = Guid.NewGuid(),
                WorkspaceId = workspaceId,
                TaskType = AITaskType.GeneralAssistant,
                EstimatedCost = 0.50m
            };

            var calls = new List<AICallRecord> { aiCallLead, aiCallOpp, aiCallUnrelated };
            var opps = new List<Opportunity> { wonOpp };
            var leads = new List<Lead> { new Lead { Id = leadId, WorkspaceId = workspaceId } };

            // Act
            var result = _roiService.CalculateDeterministicRoi(workspaceId, calls, opps, leads);

            // Assert: Exact math
            result.TotalAISpend.Should().Be(1.00m);
            result.AttributedAISpend.Should().Be(0.50m); // 0.10 + 0.40
            result.UnattributedAISpend.Should().Be(0.50m);
            result.AttributedClosedWonRevenue.Should().Be(1000000m);
            result.AttributionStatus.Should().Be(AIAttributionStatus.VerifiedAttribution);

            // ROI = (1,000,000 - 0.50) / 0.50 = 1,999,999.0
            result.NetAIRoiRatio.Should().BeApproximately(1999999.0, 0.1);
        }

        [Fact]
        public void CalculateDeterministicRoi_ShouldReportIncompleteWhenNoAiLinkageExists()
        {
            var workspaceId = Guid.NewGuid();
            var oppId = Guid.NewGuid();

            var wonOpp = new Opportunity
            {
                Id = oppId,
                WorkspaceId = workspaceId,
                Title = "Organic Word of Mouth Deal",
                EstimatedValue = 500000m,
                Stage = OpportunityStage.ClosedWon
            };

            // AI calls exist in workspace, but none linked to this opportunity
            var calls = new List<AICallRecord>
            {
                new AICallRecord { Id = Guid.NewGuid(), WorkspaceId = workspaceId, EstimatedCost = 0.20m }
            };

            var result = _roiService.CalculateDeterministicRoi(workspaceId, calls, new List<Opportunity> { wonOpp }, new List<Lead>());

            result.AttributionStatus.Should().Be(AIAttributionStatus.IncompleteAttribution);
            result.AttributedClosedWonRevenue.Should().Be(0m);
            result.NetAIRoiRatio.Should().Be(0.0);
        }
    }
}
