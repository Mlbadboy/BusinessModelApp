using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Commercial;
using BusinessModelApp.Infrastructure.Runtime.Enterprise.Commercial;
using Xunit;

namespace BusinessModelApp.Tests.Domain
{
    public class Phase4Batch444ProposalAndDealTests
    {
        private readonly InMemoryProposalAndDealStore _store;
        private readonly ProposalAndDealService _service;

        public Phase4Batch444ProposalAndDealTests()
        {
            _store = new InMemoryProposalAndDealStore();
            _service = new ProposalAndDealService(_store);
        }

        [Fact]
        public async Task CreateProposal_Valid_CalculatesNetPriceAndGrossMarginCorrectly()
        {
            var proposal = new CommercialProposal
            {
                TenantId = "tenant-comm-deal",
                OpportunityId = "opp-deal-1",
                Title = "Autonomous Marketing Operations Automation",
                BasePriceINR = 1000000m,
                DiscountPercent = 10m,
                EstimatedDeliveryCostINR = 450000m
            };

            var created = await _service.CreateProposalAsync(proposal);

            Assert.Equal(900000m, created.NetPriceINR);
            Assert.Equal(50.0m, created.ExpectedGrossMarginPercent);
            Assert.True(created.IsMarginCompliant);
            Assert.False(created.IsApprovedForSubmission);
        }

        [Fact]
        public async Task ApproveProposal_BelowMarginThreshold_ThrowsInvalidOperationException()
        {
            var proposal = new CommercialProposal
            {
                TenantId = "tenant-comm-deal",
                OpportunityId = "opp-deal-2",
                Title = "Unprofitable Custom Integration",
                BasePriceINR = 1000000m,
                DiscountPercent = 20m, // net = 800,000
                EstimatedDeliveryCostINR = 600000m // margin = 200k / 800k = 25% < 35%
            };

            var created = await _service.CreateProposalAsync(proposal);
            Assert.False(created.IsMarginCompliant);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.ApproveProposalForSubmissionAsync("tenant-comm-deal", created.ProposalId, "human-approver-prg1"));

            Assert.Contains("margin threshold", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ApproveProposal_MissingHumanSignoff_ThrowsInvalidOperationException()
        {
            var proposal = new CommercialProposal
            {
                TenantId = "tenant-comm-deal",
                OpportunityId = "opp-deal-3",
                Title = "High Margin AI Pipeline",
                BasePriceINR = 500000m,
                DiscountPercent = 0m,
                EstimatedDeliveryCostINR = 200000m // margin = 60%
            };

            var created = await _service.CreateProposalAsync(proposal);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.ApproveProposalForSubmissionAsync("tenant-comm-deal", created.ProposalId, "   "));

            Assert.Contains("Human signoff ID", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ApproveProposal_ValidWithSignoffAndMargin_Succeeds()
        {
            var proposal = new CommercialProposal
            {
                TenantId = "tenant-comm-deal",
                OpportunityId = "opp-deal-4",
                Title = "Enterprise Operating System",
                BasePriceINR = 1200000m,
                DiscountPercent = 5m, // net = 1,140,000
                EstimatedDeliveryCostINR = 500000m // margin > 56%
            };

            var created = await _service.CreateProposalAsync(proposal);
            var approved = await _service.ApproveProposalForSubmissionAsync("tenant-comm-deal", created.ProposalId, "human-prg1-ceo");

            Assert.True(approved);
            var updated = await _store.GetProposalAsync("tenant-comm-deal", created.ProposalId);
            Assert.NotNull(updated);
            Assert.True(updated.IsApprovedForSubmission);
        }

        [Fact]
        public async Task AnalyzeNegotiation_BelowFloor_EnforcesMarginFloorAndLacksExecutiveAuthority()
        {
            var proposal = new CommercialProposal
            {
                TenantId = "tenant-comm-deal",
                OpportunityId = "opp-deal-5",
                Title = "Cloud Infrastructure Autonomous Maintenance",
                BasePriceINR = 1000000m,
                DiscountPercent = 0m,
                EstimatedDeliveryCostINR = 520000m // floor = 520,000 / 0.65 = 800,000
            };

            await _service.CreateProposalAsync(proposal);

            // Customer low-balls at 600,000 INR
            var analysis = await _service.AnalyzeNegotiationCounterOfferAsync("tenant-comm-deal", proposal.ProposalId, 600000m);

            Assert.False(analysis.HasExecutiveAuthorityToCommit); // Law I40: Negotiation Intelligence != Negotiation Authority
            Assert.Equal(800000m, analysis.FloorPriceINR);
            Assert.Equal(800000m, analysis.RecommendedCounterOfferINR);
            Assert.Contains("violates 35% gross margin floor", analysis.ConcessionStrategy, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task RecordContract_MissingCryptographicDigest_ThrowsArgumentException()
        {
            var contract = new AuthoritativeDealContract
            {
                TenantId = "tenant-comm-deal",
                OpportunityId = "opp-deal-6",
                ProposalId = "prop-deal-6",
                CustomerSignerName = "Jane Doe",
                CustomerSignerEmail = "jane@enterprise.com",
                SignatureDigestSha256 = "", // missing
                VerificationSourceSystem = "DocuSignConnector",
                BindingDealValueINR = 1500000m
            };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.RecordAuthoritativeContractAsync(contract));
        }

        [Fact]
        public async Task RecordContract_ValidCryptographicSignature_VerifiesIntegritySuccessfully()
        {
            var contract = new AuthoritativeDealContract
            {
                TenantId = "tenant-comm-deal",
                OpportunityId = "opp-deal-7",
                ProposalId = "prop-deal-7",
                CustomerSignerName = "John Smith, CTO",
                CustomerSignerEmail = "john.smith@acme.corp",
                SignatureDigestSha256 = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
                VerificationSourceSystem = "DocuSignConnector_v2",
                BindingDealValueINR = 2500000m
            };

            var recorded = await _service.RecordAuthoritativeContractAsync(contract);
            Assert.True(recorded.IsCryptographicallyVerified);

            var isVerified = await _service.VerifyContractIntegrityAsync("tenant-comm-deal", recorded.ContractId);
            Assert.True(isVerified);
        }
    }
}
