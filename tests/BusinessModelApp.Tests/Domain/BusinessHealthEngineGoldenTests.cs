using System;
using System.Collections.Generic;
using BusinessModelApp.Core.Domain.Commercial;
using BusinessModelApp.Core.Services;
using FluentAssertions;
using Xunit;

namespace BusinessModelApp.Tests.Domain
{
    public class BusinessHealthEngineGoldenTests
    {
        private readonly BusinessHealthEngine _engine = new BusinessHealthEngine();

        [Fact]
        public void ScenarioA_KnownPipelineAndTarget_ShouldYieldExactCoverageAndSubscores()
        {
            // Arrange
            var workspaceId = Guid.NewGuid();
            var target = 3000000m; // ₹30 Lakhs target
            var now = new DateTime(2026, 8, 25, 12, 0, 0, DateTimeKind.Utc);

            var opp1 = new Opportunity
            {
                Id = Guid.NewGuid(),
                WorkspaceId = workspaceId,
                Title = "Enterprise Deal A",
                EstimatedValue = 5000000m, // ₹50L
                Probability = 0.8,
                Stage = OpportunityStage.Proposal,
                CreatedAt = now.AddDays(-10),
                UpdatedAt = now.AddDays(-2)
            };

            var opp2 = new Opportunity
            {
                Id = Guid.NewGuid(),
                WorkspaceId = workspaceId,
                Title = "Enterprise Deal B",
                EstimatedValue = 5000000m, // ₹50L
                Probability = 0.4,
                Stage = OpportunityStage.Discovery,
                CreatedAt = now.AddDays(-10),
                UpdatedAt = now.AddDays(-2)
            };

            var leads = new List<Lead>
            {
                new Lead { Id = Guid.NewGuid(), WorkspaceId = workspaceId, Status = LeadStatus.Qualified },
                new Lead { Id = Guid.NewGuid(), WorkspaceId = workspaceId, Status = LeadStatus.Qualified },
                new Lead { Id = Guid.NewGuid(), WorkspaceId = workspaceId, Status = LeadStatus.New },
                new Lead { Id = Guid.NewGuid(), WorkspaceId = workspaceId, Status = LeadStatus.New }
            };

            // Act
            var result = _engine.CalculateHealth(leads, new[] { opp1, opp2 }, target, asOfDate: now);

            // Assert: Deterministic Financial Invariants
            result.TotalPipelineValue.Should().Be(10000000m); // ₹1 Cr
            result.WeightedForecastValue.Should().Be(6000000m); // ₹60L (50*0.8 + 50*0.4)
            result.PipelineCoverageRatio.Should().Be(2.0); // 60L / 30L = 2.0x
            result.SubScores.PipelineScore.Should().Be(100.0); // (2.0 / 2.0) * 100 = 100
            result.LeadQualificationRate.Should().Be(0.5); // 2 qualified / 4 total = 50%
            result.CalculationVersion.Should().Be("HealthEngine:v1.0");
            result.EvidenceRecords.Should().NotBeEmpty();
            result.EvidenceRecords.Should().Contain(e => e.MetricKey == "METRIC_WEIGHTED_FORECAST");
        }

        [Fact]
        public void ScenarioB_InsufficientData_ShouldProduceLowConfidence_WithoutPlungingHealthToZero()
        {
            // Arrange: Bare newly created workspace with only 1 lead & 1 opp
            var workspaceId = Guid.NewGuid();
            var now = new DateTime(2026, 8, 25, 12, 0, 0, DateTimeKind.Utc);

            var lead = new Lead { Id = Guid.NewGuid(), WorkspaceId = workspaceId, Status = LeadStatus.New };
            var opp = new Opportunity
            {
                Id = Guid.NewGuid(),
                WorkspaceId = workspaceId,
                EstimatedValue = 500000m,
                Probability = 0.2,
                Stage = OpportunityStage.Discovery,
                CreatedAt = now
            };

            // Act
            var result = _engine.CalculateHealth(new[] { lead }, new[] { opp }, 5000000m, asOfDate: now);

            // Assert: Confidence is flagged as Low, but Health Score retains baseline reasonableness (> 30)
            result.ConfidenceLevel.Should().Be("Low");
            result.ConfidenceScore.Should().BeLessThan(0.40);
            result.ConfidenceReason.Should().Contain("Insufficient historical data");
            result.OverallHealthScore.Should().BeGreaterThan(30.0);
        }

        [Fact]
        public void ScenarioC_StalledDeals_ShouldIncreaseRiskIndex()
        {
            // Arrange: 1 opportunity created 20 days ago with zero activities
            var workspaceId = Guid.NewGuid();
            var now = new DateTime(2026, 8, 25, 12, 0, 0, DateTimeKind.Utc);

            var stalledOpp = new Opportunity
            {
                Id = Guid.NewGuid(),
                WorkspaceId = workspaceId,
                EstimatedValue = 1000000m,
                Probability = 0.3,
                Stage = OpportunityStage.Discovery,
                CreatedAt = now.AddDays(-20), // 20 days ago > 14 days threshold
                Activities = new List<Activity>()
            };

            // Act
            var result = _engine.CalculateHealth(new List<Lead>(), new[] { stalledOpp }, 5000000m, asOfDate: now);

            // Assert
            result.StalledRiskIndex.Should().Be(100.0);
            result.SubScores.RiskScore.Should().Be(0.0);
            result.EvidenceRecords.Should().Contain(e => e.MetricKey == "METRIC_STALLED_RISK_INDEX" && e.NumericValue == 100.0);
        }
    }
}
