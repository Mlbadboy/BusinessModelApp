using System;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Growth;
using BusinessModelApp.Infrastructure.Runtime.Enterprise.Growth;
using Xunit;

namespace BusinessModelApp.Tests.Domain
{
    public class Phase4Batch451AcquisitionAndRetentionTests
    {
        [Fact]
        public async Task DiscoverAndScoreProspect_ValidInput_CreatesScoredProspectWithCommittee()
        {
            var store = new InMemoryCustomerAcquisitionAndRetentionStore();
            var service = new CustomerAcquisitionAndRetentionService(store);

            var prospect = await service.DiscoverAndScoreProspectAsync(
                "Acme Global",
                "FinTech",
                500,
                50_000_000m,
                0.92m,
                "Strong fit for autonomous business operations and reconciliation automation.");

            Assert.NotNull(prospect);
            Assert.Equal("Acme Global", prospect.CompanyName);
            Assert.Equal(0.92m, prospect.IcpScore);

            prospect.AddCommitteeMember(new BuyingCommitteeMember
            {
                Name = "Jane Doe",
                Title = "Chief Financial Officer",
                RoleType = BuyingRoleType.EconomicBuyer,
                ContactEmail = "jane@acme.com"
            });

            Assert.Single(prospect.BuyingCommittee);
            Assert.Equal(BuyingRoleType.EconomicBuyer, prospect.BuyingCommittee[0].RoleType);
        }

        [Fact]
        public async Task SendGovernedOutreach_ValidGovernance_SavesEngagement()
        {
            var store = new InMemoryCustomerAcquisitionAndRetentionStore();
            var service = new CustomerAcquisitionAndRetentionService(store);

            var prospect = await service.DiscoverAndScoreProspectAsync("Acme Global", "FinTech", 500, 50_000_000m, 0.92m, "Good");

            var engagement = await service.SendGovernedOutreachAsync(
                prospect.ProspectId,
                "jane@acme.com",
                "Email",
                "Autonomous Business Operations for Acme",
                "Hello Jane, exploring how Charlie can automate continuous operations...",
                antiSpamComplianceVerified: true,
                governedSignoffDigest: "SHA256_GOVERNANCE_DIGEST_XYZ");

            Assert.NotNull(engagement);
            Assert.True(engagement.AntiSpamComplianceVerified);
            Assert.Equal("SHA256_GOVERNANCE_DIGEST_XYZ", engagement.GovernedSignoffDigest);

            await service.RecordOutreachResponseAsync(engagement.EngagementId, OutreachReplyClassification.RequestDemo, 0.85m);
            Assert.Equal(OutreachReplyClassification.RequestDemo, engagement.ReplyClassification);
            Assert.Equal(0.85m, engagement.SentimentScore);
        }

        [Fact]
        public async Task SendGovernedOutreach_MissingAntiSpamOrSignoff_ThrowsInvalidOperationException()
        {
            var store = new InMemoryCustomerAcquisitionAndRetentionStore();
            var service = new CustomerAcquisitionAndRetentionService(store);

            var prospect = await service.DiscoverAndScoreProspectAsync("Acme Global", "FinTech", 500, 50_000_000m, 0.92m, "Good");

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await service.SendGovernedOutreachAsync(
                    prospect.ProspectId,
                    "jane@acme.com",
                    "Email",
                    "Spam Subject",
                    "Spam Body",
                    antiSpamComplianceVerified: false,
                    governedSignoffDigest: "DIGEST");
            });

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await service.SendGovernedOutreachAsync(
                    prospect.ProspectId,
                    "jane@acme.com",
                    "Email",
                    "Subject",
                    "Body",
                    antiSpamComplianceVerified: true,
                    governedSignoffDigest: "");
            });
        }

        [Fact]
        public async Task ScheduleAndConcludeMeeting_Valid_TransitionsOutcome()
        {
            var store = new InMemoryCustomerAcquisitionAndRetentionStore();
            var service = new CustomerAcquisitionAndRetentionService(store);

            var prospect = await service.DiscoverAndScoreProspectAsync("Acme Global", "FinTech", 500, 50_000_000m, 0.92m, "Good");

            var meeting = await service.ScheduleMeetingAsync(
                prospect.ProspectId,
                "Discovery Call with CFO",
                DateTime.UtcNow.AddDays(2),
                "Agenda: Unpack finance automation bottlenecks.");

            Assert.Equal(MeetingOutcomeState.Scheduled, meeting.Outcome);

            await service.ConcludeMeetingAsync(
                meeting.MeetingId,
                MeetingOutcomeState.CompletedAdvanceToProposal,
                "Strong pain point identified in monthly close reconciliation.",
                meddpicQualified: true);

            var fetched = await store.GetMeetingAsync(meeting.MeetingId);
            Assert.NotNull(fetched);
            Assert.Equal(MeetingOutcomeState.CompletedAdvanceToProposal, fetched!.Outcome);
            Assert.True(fetched.MeddpicQualified);
        }

        [Fact]
        public async Task GenerateProposal_MarginFloorEnforced_RejectsBelow35Percent()
        {
            var store = new InMemoryCustomerAcquisitionAndRetentionStore();
            var service = new CustomerAcquisitionAndRetentionService(store);

            var prospect = await service.DiscoverAndScoreProspectAsync("Acme Global", "FinTech", 500, 50_000_000m, 0.92m, "Good");

            // Price 100k, Cost Basis 70k -> Margin = 30% (< 35%) -> MUST throw
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await service.GenerateProposalAsync(
                    prospect.ProspectId,
                    "Charlie Business OS Deployment",
                    "Enterprise automated reconciliation",
                    100_000m,
                    70_000m, // 30% margin
                    4.5m,
                    4);
            });

            // Price 100k, Cost Basis 50k -> Margin = 50% (>= 35%) -> MUST PASS
            var proposal = await service.GenerateProposalAsync(
                prospect.ProspectId,
                "Charlie Business OS Deployment",
                "Enterprise automated reconciliation",
                100_000m,
                50_000m, // 50% margin
                4.5m,
                4);

            Assert.NotNull(proposal);
            Assert.Equal(50.0m, proposal.GrossMarginPercent);

            var approved = await service.ApproveProposalAsync(proposal.ProposalId, "HeadOfGrowth");
            Assert.True(approved.IsApproved);
            Assert.Equal("HeadOfGrowth", approved.ApprovalAuthority);
        }

        [Fact]
        public async Task FinalizeNegotiation_RequiresPrg1SignoffAndMarginFloor()
        {
            var store = new InMemoryCustomerAcquisitionAndRetentionStore();
            var service = new CustomerAcquisitionAndRetentionService(store);

            var prospect = await service.DiscoverAndScoreProspectAsync("Acme Global", "FinTech", 500, 50_000_000m, 0.92m, "Good");
            var proposal = await service.GenerateProposalAsync(
                prospect.ProspectId,
                "Solution",
                "Scope",
                100_000m,
                50_000m,
                4.0m,
                4);

            // Missing PRG-1 signoff -> Throws
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await service.FinalizeNegotiationAsync(
                    proposal.ProposalId,
                    100_000m,
                    90_000m,
                    50_000m,
                    "Payment terms Net 45",
                    "Multi-year commitment",
                    prg1SignoffId: "",
                    isClosedWon: true);
            });

            // Concession dropping margin below 35% on Closed-Won:
            // Agreed price 70k, Cost basis 50k -> Margin = 20k/70k = 28.57% -> MUST THROW
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await service.FinalizeNegotiationAsync(
                    proposal.ProposalId,
                    100_000m,
                    70_000m,
                    50_000m,
                    "Heavy discount",
                    "None",
                    prg1SignoffId: "PRG1_SIGNOFF_123",
                    isClosedWon: true);
            });

            // Valid negotiation: Agreed price 85k, Cost basis 50k -> Margin = 35k/85k = 41.17% -> PASS
            var neg = await service.FinalizeNegotiationAsync(
                proposal.ProposalId,
                100_000m,
                85_000m,
                50_000m,
                "5% volume discount",
                "2-year commitment upfront",
                prg1SignoffId: "PRG1_SIGNOFF_123",
                isClosedWon: true);

            Assert.NotNull(neg);
            Assert.True(neg.IsClosedWon);
            Assert.True(neg.FinalGrossMarginPercent >= 35.0m);
        }

        [Fact]
        public async Task ExecuteContractAndTrackRetention_FullLifecycle()
        {
            var store = new InMemoryCustomerAcquisitionAndRetentionStore();
            var service = new CustomerAcquisitionAndRetentionService(store);

            var contract = await service.ExecuteContractAsync(
                "prospect-1",
                "proposal-1",
                85_000m,
                "Net 30",
                "SHA256_CONTRACT_DIGEST_ABC",
                "Jane Doe (CFO)",
                "Charlie Sovereign AI Operator");

            Assert.NotNull(contract);
            Assert.True(contract.IsFullyExecuted);

            var retention = await service.InitiateOnboardingAsync("cust-1", contract.ContractId, "Acme Global");
            Assert.NotNull(retention);
            Assert.Equal(100.0m, retention.HealthScore);
            Assert.Equal(ChurnRiskLevel.Low, retention.ChurnRisk);

            // Record value realization
            var valueDate = DateTime.UtcNow.AddDays(7);
            await service.RecordValueRealizationAsync("cust-1", valueDate, "Automated First Month-End Close");

            Assert.NotNull(retention.FirstValueRealizedUtc);
            Assert.Single(retention.MilestonesAchieved);

            // Update health and detect expansion
            await service.UpdateCustomerHealthAsync("cust-1", 95.0m, ChurnRiskLevel.Low, 85m);
            retention.FlagExpansionOpportunity("Customer requested adding AP automation module in Q3.");

            Assert.Equal(95.0m, retention.HealthScore);
            Assert.Equal(85m, retention.LatestNpsScore);
            Assert.True(retention.ExpansionIdentified);
        }
    }
}
