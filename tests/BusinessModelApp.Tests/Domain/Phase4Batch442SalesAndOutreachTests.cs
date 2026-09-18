using System;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Commercial;
using BusinessModelApp.Infrastructure.Runtime.Enterprise.Commercial;
using FluentAssertions;
using Xunit;

namespace BusinessModelApp.Tests.Domain
{
    public sealed class Phase4Batch442SalesAndOutreachTests
    {
        private readonly InMemorySalesIntelligenceStore _salesStore;
        private readonly SalesIntelligenceService _salesService;

        private readonly InMemoryOutreachEngineStore _outreachStore;
        private readonly OutreachEngineService _outreachService;

        public Phase4Batch442SalesAndOutreachTests()
        {
            _salesStore = new InMemorySalesIntelligenceStore();
            _salesService = new SalesIntelligenceService(_salesStore);

            _outreachStore = new InMemoryOutreachEngineStore();
            _outreachService = new OutreachEngineService(_outreachStore);
        }

        [Fact]
        public async Task SALES443_01_StrategyFormulation_RecommendsWithoutSelfAuthorization()
        {
            var strategy = await _salesService.FormulateAccountStrategyAsync(
                "tenant-442",
                "acc-acme-corp",
                "opp-automation-01",
                "agent-sales-lead-01"
            );

            strategy.Should().NotBeNull();
            strategy.ValueHypothesis.Should().NotBeNullOrWhiteSpace();
            strategy.AnticipatedObjections.Should().NotBeEmpty();
            strategy.RecommendedNextAction.Should().NotBeNullOrWhiteSpace();
            strategy.IsAgentSelfAuthorized.Should().BeFalse(); // Law I40: Agent cannot self-authorize
        }

        [Fact]
        public async Task OUT444_01_LowRiskOutreach_AutoApprovesForAutonomousDispatch()
        {
            var intent = new CommercialCommunicationIntent
            {
                TenantId = "tenant-442",
                OpportunityId = "opp-101",
                InitiatingAgentId = "agent-scout-01",
                Channel = CommercialOutreachChannel.EMAIL,
                RecipientAddress = "prospect@example.com",
                Subject = "Introduction to Autonomous Operations",
                BodyContent = "Sharing our latest whitepaper on autonomous operations.",
                RiskTier = 1
            };

            var submitted = await _outreachService.SubmitOutboundIntentAsync(intent);
            submitted.RequiresHumanApproval.Should().BeFalse();
            submitted.IsHumanApproved.Should().BeTrue();

            var dispatched = await _outreachService.DispatchOutboundCommunicationAsync("tenant-442", submitted.IntentId);
            dispatched.Should().BeTrue();
            submitted.IsDispatched.Should().BeTrue();
            submitted.ExternalEffectId.Should().StartWith("eff-outreach-EMAIL");
        }

        [Fact]
        public async Task OUT444_02_HighRiskOutreach_BlocksWithoutHumanSignoff_AndSucceedsWithPRG1()
        {
            var intent = new CommercialCommunicationIntent
            {
                TenantId = "tenant-442",
                OpportunityId = "opp-102",
                InitiatingAgentId = "agent-closer-01",
                Channel = CommercialOutreachChannel.EMAIL,
                RecipientAddress = "cfo@enterprise.com",
                Subject = "Binding Master Service Agreement & Enterprise Pricing",
                BodyContent = "Enclosed is our binding commercial agreement of $120,000...",
                RiskTier = 4 // Consequential commercial binding commitment
            };

            var submitted = await _outreachService.SubmitOutboundIntentAsync(intent);
            submitted.RequiresHumanApproval.Should().BeTrue();
            submitted.IsHumanApproved.Should().BeFalse();

            // Attempt unapproved dispatch -> Must fail
            var act = () => _outreachService.DispatchOutboundCommunicationAsync("tenant-442", submitted.IntentId);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Governance Violation*PRG-1 human authorization*");

            // Human Executive signs off
            var approved = await _outreachService.ApproveOutboundIntentAsync("tenant-442", submitted.IntentId, "Exec-ChiefCommercialOfficer");
            approved.Should().BeTrue();

            // Dispatch now succeeds
            var dispatched = await _outreachService.DispatchOutboundCommunicationAsync("tenant-442", submitted.IntentId);
            dispatched.Should().BeTrue();
        }
    }
}
