using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessModelApp.Core.AI;
using BusinessModelApp.Core.Prospecting;
using BusinessModelApp.Infrastructure.AI.OmniRoute;
using BusinessModelApp.Infrastructure.Prospecting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BusinessModelApp.Tests.Domain
{
    public class ProspectDiscoveryEngineTests
    {
        private readonly ICPScoringEngine _scoringEngine = new();

        [Fact]
        public void ICPScoringEngine_ShouldAwardHighScores_ForEnterpriseAccountsWithTransformationSignals()
        {
            // Arrange
            var enterpriseAccount = new DiscoveredCandidateAccount
            {
                CompanyName = "HDFC Capital Financial Services",
                Domain = "hdfccapital.com",
                Industry = "Enterprise BFSI",
                Headcount = 4200,
                EstimatedAnnualRevenueINR = 1850000000m,
                Signals = new List<ProspectSignal>
                {
                    new ProspectSignal
                    {
                        Type = SignalType.AITransformation,
                        Headline = "RBI Algorithmic Credit Governance Framework Announced",
                        EvidenceHash = "HASH-12345"
                    },
                    new ProspectSignal
                    {
                        Type = SignalType.RegulatoryCompliance,
                        Headline = "Digital Lending Security and SLA Compliance Mandate",
                        EvidenceHash = "HASH-67890"
                    }
                }
            };

            // Act
            decimal score = _scoringEngine.ScoreAccountICP(enterpriseAccount, "Enterprise BFSI", 2500000m, out var breakdown);

            // Assert
            Assert.True(score >= 80m, $"Expected enterprise account to score >= 80, but got {score}");
            Assert.Equal(25m, breakdown["IndustryFit"]);
            Assert.Equal(18m, breakdown["CompanyScale"]); // 4200 headcount >= 1000
            Assert.Equal(20m, breakdown["TransformationSignals"]);
            Assert.Equal(20m, breakdown["BudgetViability"]);
            Assert.True(breakdown["UrgencyMandate"] >= 12m);
        }

        [Fact]
        public void ICPScoringEngine_ShouldReject_SmallCompaniesWithoutSignals()
        {
            // Arrange
            var smallAccount = new DiscoveredCandidateAccount
            {
                CompanyName = "Local Accounting Boutique",
                Domain = "localaccount.com",
                Industry = "Accounting",
                Headcount = 15,
                EstimatedAnnualRevenueINR = 5000000m,
                Signals = new List<ProspectSignal>()
            };

            // Act
            decimal score = _scoringEngine.ScoreAccountICP(smallAccount, "Enterprise BFSI", 2500000m, out var breakdown);

            // Assert
            Assert.True(score < 50m, $"Expected small account to score < 50, but got {score}");
            Assert.True(score < 70m, "Candidate must fail the 70-point qualification threshold");
        }

        [Fact]
        public async Task CompanyIntelligenceProvider_ShouldReturnDiscoveredAccounts_WithEvidenceHashes()
        {
            // Arrange
            var provider = new CompanyIntelligenceProvider();

            // Act
            var accounts = await provider.DiscoverCandidateAccountsAsync("Enterprise BFSI", "India", 500);

            // Assert
            Assert.NotEmpty(accounts);
            foreach (var account in accounts)
            {
                Assert.False(string.IsNullOrWhiteSpace(account.CompanyName));
                Assert.False(string.IsNullOrWhiteSpace(account.Domain));
                Assert.True(account.Headcount >= 500);
                Assert.NotEmpty(account.Signals);
                foreach (var sig in account.Signals)
                {
                    Assert.False(string.IsNullOrWhiteSpace(sig.EvidenceHash));
                }
            }
        }

        [Fact]
        public async Task DecisionMakerDiscoveryProvider_ShouldReturnExecutiveBuyer_WithMatchingDomain()
        {
            // Arrange
            var provider = new DecisionMakerDiscoveryProvider();
            var account = new DiscoveredCandidateAccount
            {
                CompanyName = "HDFC Capital Financial Services",
                Domain = "hdfccapital.com",
                Industry = "Enterprise BFSI"
            };

            // Act
            var decisionMakers = await provider.DiscoverDecisionMakersAsync(account);

            // Assert
            Assert.NotEmpty(decisionMakers);
            var topBuyer = decisionMakers[0];
            Assert.False(string.IsNullOrWhiteSpace(topBuyer.Name));
            Assert.False(string.IsNullOrWhiteSpace(topBuyer.Title));
            Assert.Contains("hdfccapital.com", topBuyer.CorporateEmail);
            Assert.True(topBuyer.AuthorityScore >= 0.85m);
        }

        [Fact]
        public async Task ProspectDiscoveryService_ShouldProduceVerifiedLead_WithProvenanceToken()
        {
            // Arrange
            var companyProvider = new CompanyIntelligenceProvider();
            var scoringEngine = new ICPScoringEngine();
            var dmProvider = new DecisionMakerDiscoveryProvider();
            var mockAiClient = new Mock<IOmniRouteClient>();

            mockAiClient
                .Setup(a => a.SendChatCompletionAsync(It.IsAny<AIRequest>(), It.IsAny<AIRoutingPolicy>(), default))
                .ReturnsAsync(new AIResponse
                {
                    Content = "{\"score\": 92.0, \"tier\": \"Tier-1 Enterprise\", \"rationale\": \"Strong institutional fit.\"}",
                    FinishReason = "stop"
                });

            var service = new ProspectDiscoveryService(
                companyProvider, 
                scoringEngine, 
                dmProvider, 
                mockAiClient.Object, 
                NullLogger<ProspectDiscoveryService>.Instance);

            var account = new DiscoveredCandidateAccount
            {
                CompanyName = "ICICI Securities Digital Solutions",
                Domain = "icicisecurities.com",
                Industry = "Enterprise BFSI",
                Headcount = 6800,
                EstimatedAnnualRevenueINR = 3200000000m,
                Signals = new List<ProspectSignal>
                {
                    new ProspectSignal { Type = SignalType.AITransformation, Headline = "Wealth Management Modernization" }
                }
            };

            var dm = new CandidateDecisionMaker
            {
                Name = "Rohan Chawla",
                Title = "Head of Institutional Transformation",
                CorporateEmail = "rohan.c@icicisecurities.com"
            };

            // Act
            var verifiedLead = await service.QualifyAndVerifyProspectAsync(account, dm, "Enterprise BFSI", 2500000m);

            // Assert
            Assert.NotNull(verifiedLead);
            Assert.StartsWith("PRV-", verifiedLead!.ProvenanceToken);
            Assert.True(verifiedLead.ICPScore >= 70m);
            Assert.Equal(92.0m, verifiedLead.AIQualificationScore);
            Assert.Equal("Rohan Chawla", verifiedLead.DecisionMaker.Name);
            Assert.Equal("ICICI Securities Digital Solutions", verifiedLead.Account.CompanyName);
        }
    }
}
