using System;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Commercial;
using BusinessModelApp.Infrastructure.Runtime.Enterprise.Commercial;
using FluentAssertions;
using Xunit;

namespace BusinessModelApp.Tests.Domain
{
    public sealed class Phase4Batch441DiscoveryAndAccountTests
    {
        private readonly InMemoryOpportunityDiscoveryStore _discoveryStore;
        private readonly OpportunityDiscoveryService _discoveryService;

        private readonly InMemoryAccountIntelligenceStore _accountStore;
        private readonly AccountIntelligenceService _accountService;

        public Phase4Batch441DiscoveryAndAccountTests()
        {
            _discoveryStore = new InMemoryOpportunityDiscoveryStore();
            _discoveryService = new OpportunityDiscoveryService(_discoveryStore);

            _accountStore = new InMemoryAccountIntelligenceStore();
            _accountService = new AccountIntelligenceService(_accountStore);
        }

        [Fact]
        public async Task DISC441_01_SignalIngestionAndIcpMatching_GeneratesGroundedOpportunity()
        {
            var profile = await _discoveryService.CreateIcpProfileAsync(new ICPProfile
            {
                TenantId = "tenant-441",
                ProfileName = "Fintech High-Growth",
                TargetIndustries = { "Fintech", "Banking" },
                TargetGeographies = { "US", "IN" },
                MinTargetRevenueINR = 1000000m,
                RequiredKeywords = { "automation", "compliance" }
            });

            var signal = await _discoveryService.IngestMarketSignalAsync(new MarketSignalItem
            {
                TenantId = "tenant-441",
                Source = "FinancialTimes_Article",
                CompanyName = "NeoFin Technologies",
                Domain = "neofin.io",
                Industry = "Fintech",
                Geography = "IN",
                FreshnessScore = 0.95m,
                RawContent = "NeoFin Technologies accelerates regulatory compliance and enterprise automation modernization."
            });

            var opp = await _discoveryService.EvaluateSignalAgainstIcpAsync("tenant-441", signal.SignalId, profile.ProfileId);

            opp.Should().NotBeNull();
            opp.IsGrounded.Should().BeTrue();
            opp.CorroboratedEvidenceIds.Should().Contain(signal.SignalId);
            opp.ICPScore.Should().BeGreaterThan(0.7m);
            opp.EstimatedDealValueINR.Should().Be(1000000m);
            opp.EvaluationExplanation.Should().Contain("Fintech High-Growth");
        }

        [Fact]
        public async Task DISC441_02_NegativeKeywords_ElevatesRiskScore()
        {
            var profile = await _discoveryService.CreateIcpProfileAsync(new ICPProfile
            {
                TenantId = "tenant-441",
                ProfileName = "SaaS Standard",
                TargetIndustries = { "SaaS" },
                TargetGeographies = { "US" },
                NegativeKeywords = { "bankruptcy", "layoffs" }
            });

            var signal = await _discoveryService.IngestMarketSignalAsync(new MarketSignalItem
            {
                TenantId = "tenant-441",
                Source = "TechNews",
                CompanyName = "DistressedCorp",
                Domain = "distressed.com",
                Industry = "SaaS",
                Geography = "US",
                RawContent = "DistressedCorp announces deep layoffs amid debt restructuring."
            });

            var opp = await _discoveryService.EvaluateSignalAgainstIcpAsync("tenant-441", signal.SignalId, profile.ProfileId);
            opp.RiskScore.Should().Be(0.8m);
        }

        [Fact]
        public async Task ACCT442_01_BuyingCenter_UnverifiedPersonRemainsUnknown()
        {
            var graph = await _accountService.UpsertAccountGraphAsync(new EnterpriseAccountGraph
            {
                TenantId = "tenant-441",
                AccountId = "acc-neofin",
                CompanyName = "NeoFin Technologies",
                Domain = "neofin.io",
                Industry = "Fintech"
            });

            // Add ungrounded person (no authoritative evidence, low confidence)
            var contact = await _accountService.AddBuyingCenterContactAsync("tenant-441", graph.AccountId, new BuyingCenterContact
            {
                FullName = "Speculative Lead",
                Title = "Presumed VP",
                Email = "presumed@neofin.io",
                Role = BuyingCenterPersonaRole.DECISION_MAKER, // Claimed
                ConfidenceScore = 0.4m, // Low confidence
                AuthoritativeEvidenceId = "" // Law I40: Missing evidence
            });

            contact.Role.Should().Be(BuyingCenterPersonaRole.UNKNOWN);
            contact.IsVerified.Should().BeFalse();
        }

        [Fact]
        public async Task ACCT442_02_BuyingCenter_VerifiedPersonWithHighConfidencePreservesRole()
        {
            var graph = await _accountService.UpsertAccountGraphAsync(new EnterpriseAccountGraph
            {
                TenantId = "tenant-441",
                AccountId = "acc-verified",
                CompanyName = "Apex Capital",
                Domain = "apexcap.com"
            });

            var contact = await _accountService.AddBuyingCenterContactAsync("tenant-441", graph.AccountId, new BuyingCenterContact
            {
                FullName = "Jane Doe",
                Title = "Chief Technology Officer",
                Email = "jane.doe@apexcap.com",
                Role = BuyingCenterPersonaRole.TECHNICAL_BUYER,
                ConfidenceScore = 0.95m,
                AuthoritativeEvidenceId = "ev-linkedin-verified-org-chart"
            });

            contact.Role.Should().Be(BuyingCenterPersonaRole.TECHNICAL_BUYER);
            contact.IsVerified.Should().BeTrue();
        }
    }
}
