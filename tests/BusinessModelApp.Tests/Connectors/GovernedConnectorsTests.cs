using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessModelApp.Core.Agents;
using BusinessModelApp.Core.Connectors;
using BusinessModelApp.Infrastructure.Connectors;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BusinessModelApp.Tests.Connectors
{
    public class GovernedConnectorsTests
    {
        [Fact]
        public void ConnectorConsent_EnforcesScopeAndBlocksDelete()
        {
            var consent = new ConnectorConsent
            {
                TenantId = Guid.NewGuid(),
                Connector = ConnectorType.CRM,
                GrantedScopes = ConnectorScope.ReadMetadata | ConnectorScope.CreateRecords | ConnectorScope.UpdateRecords,
                IsActive = true
            };

            Assert.True(consent.CanPerform(ConnectorScope.CreateRecords));
            Assert.True(consent.CanPerform(ConnectorScope.UpdateRecords));
            Assert.False(consent.CanPerform(ConnectorScope.Draft));
            
            // Hard security boundary: Delete is never permitted
            Assert.False(consent.CanPerform(ConnectorScope.DeleteRecords));
        }

        [Fact]
        public void ConnectorConsent_EnforcesDailyRateLimits()
        {
            var consent = new ConnectorConsent
            {
                TenantId = Guid.NewGuid(),
                DailyMaxOutbound = 5,
                OutboundSentToday = 4
            };

            Assert.True(consent.TryConsumeOutboundQuota(1));
            Assert.Equal(5, consent.OutboundSentToday);
            Assert.False(consent.TryConsumeOutboundQuota(1)); // Exceeded
        }

        [Fact]
        public async Task GovernedCompanyIntelligenceConnector_GeneratesAccountGraphWithValidEvidence()
        {
            var connector = new GovernedCompanyIntelligenceConnector(NullLogger<GovernedCompanyIntelligenceConnector>.Instance);
            var graph = await connector.BuildAccountGraphAsync("apexfintech.com");

            Assert.NotNull(graph);
            Assert.Equal("Apex Financial Technologies", graph.CompanyName);
            Assert.True(graph.Headcount >= 500);
            Assert.NotEmpty(graph.DecisionMakers);
            Assert.StartsWith("EVD-ACCOUNT-", graph.GroundingEvidenceId);
            Assert.True(graph.OverallICPScore >= 85m);
        }

        [Fact]
        public async Task GovernedCalendarSchedulingConnector_BooksMeetingAndProducesMeetingBrief()
        {
            var connector = new GovernedCalendarSchedulingConnector();
            var slots = await connector.GetAvailableSlotsAsync(Guid.NewGuid());
            Assert.NotEmpty(slots);

            var brief = await connector.BookMeetingAsync(
                Guid.NewGuid(),
                "Apex Financial Technologies",
                "rajesh.sharma@apexfintech.example.com",
                slots[0],
                new List<string> { "EVD-ACCOUNT-01", "EVD-DM-01" });

            Assert.NotNull(brief);
            Assert.Equal("Apex Financial Technologies", brief.CompanyName);
            Assert.NotEmpty(brief.DetectedPainPoints);
            Assert.NotEmpty(brief.RecommendedTalkingPoints);
            Assert.True(brief.PotentialDealValueINR > 0);
        }
    }
}
