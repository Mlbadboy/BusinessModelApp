using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessModelApp.Core.Agents;
using BusinessModelApp.Core.AI.Governance;
using BusinessModelApp.Core.Domain.Commercial;
using BusinessModelApp.Core.Interfaces;
using BusinessModelApp.Infrastructure.Data;
using BusinessModelApp.Infrastructure.Repositories;
using BusinessModelApp.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BusinessModelApp.Tests.Domain
{
    public class VoiceTelephonyGovernanceTests
    {
        private AppDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public async Task InitiateOutboundCall_ShouldPersistRecord_WithDispatchAcceptedState()
        {
            using var context = CreateInMemoryDbContext();
            var repo = new CommercialRepository(context);
            var minimization = new AIDataMinimizationService();
            var voiceService = new SimulatedVoiceService(context, repo, minimization, new NullLogger<SimulatedVoiceService>());

            var request = new OutboundCallRequest
            {
                WorkspaceId = Guid.NewGuid(),
                PhoneNumber = "+91 98230 11223",
                ContactName = "Rajesh Gupta",
                CompanyName = "Apex Retail",
                GoalPrompt = "Qualify POS automation interest",
                IsTestCall = true,
                MaxBudgetINR = 10.0m
            };

            var result = await voiceService.InitiateOutboundCallAsync(request);

            result.Success.Should().BeTrue();
            result.Status.Should().Be(VoiceCallStatus.DispatchAccepted);
            result.ProviderCallId.Should().StartWith("SIM-CALL-");

            var savedRecord = await context.VoiceCallRecords.FirstOrDefaultAsync(r => r.ProviderCallId == result.ProviderCallId);
            savedRecord.Should().NotBeNull();
            savedRecord!.ContactName.Should().Be("Rajesh Gupta");
            savedRecord.EstimatedCostINR.Should().Be(10.0m);
            savedRecord.PhoneNumberHash.Should().NotBeEmpty();
        }

        [Fact]
        public async Task WebhookIngestion_ShouldBeIdempotent_OnDuplicateEvent()
        {
            using var context = CreateInMemoryDbContext();
            var repo = new CommercialRepository(context);
            var minimization = new AIDataMinimizationService();
            var voiceService = new SimulatedVoiceService(context, repo, minimization, new NullLogger<SimulatedVoiceService>());

            var wsId = Guid.NewGuid();
            var lead = new Lead
            {
                WorkspaceId = wsId,
                ContactName = "Rajesh Gupta",
                CompanyName = "Apex Retail",
                Phone = "+91 98230 11223",
                Status = LeadStatus.New
            };
            context.Leads.Add(lead);
            await context.SaveChangesAsync();

            var dispatch = await voiceService.InitiateOutboundCallAsync(new OutboundCallRequest
            {
                WorkspaceId = wsId,
                LeadId = lead.Id,
                PhoneNumber = "+91 98230 11223",
                ContactName = "Rajesh Gupta"
            });

            var rawPayload = $"{{\"providerCallId\":\"{dispatch.ProviderCallId}\",\"durationSeconds\":167,\"actualCostINR\":2.40,\"intent\":\"High Intent\",\"qualityScore\":93.0}}";
            var headers = new Dictionary<string, string> { { "x-event-id", "EVT-UNIQUE-001" } };

            // First Webhook Delivery
            var result1 = await voiceService.ProcessWebhookAsync(rawPayload, headers);
            result1.Success.Should().BeTrue();
            result1.Status.Should().Be(VoiceWebhookProcessingStatus.Processed);
            result1.ResultingCallStatus.Should().Be(VoiceCallStatus.TranscriptPersisted);
            result1.ActualCostINR.Should().Be(2.40m);

            // Duplicate Webhook Delivery (Retry by provider)
            var result2 = await voiceService.ProcessWebhookAsync(rawPayload, headers);
            result2.Success.Should().BeTrue();
            result2.Status.Should().Be(VoiceWebhookProcessingStatus.DuplicateIgnored);

            // Verify Lead Updated
            var updatedLead = await context.Leads.FindAsync(lead.Id);
            updatedLead!.Status.Should().Be(LeadStatus.Qualified);
            updatedLead.QualityScore.Should().Be(93.0);
        }

        [Fact]
        public async Task GovernedToolRegistry_ShouldExecuteVoiceCallAdapter_AndReconcileHoldCorrectly()
        {
            using var context = CreateInMemoryDbContext();
            var repo = new CommercialRepository(context);
            var minimization = new AIDataMinimizationService();
            var voiceService = new SimulatedVoiceService(context, repo, minimization, new NullLogger<SimulatedVoiceService>());
            var voiceAdapter = new VoiceCallAdapter(voiceService);

            var registry = new GovernedToolRegistry(repo, new[] { voiceAdapter });

            var mission = new AgentMission
            {
                WorkspaceId = Guid.NewGuid(),
                OrganizationId = Guid.NewGuid(),
                Mode = MissionMode.HybridPilot,
                AutonomyLevel = AutonomyLevel.Level3_ControlledAutonomy,
                Wallet = MissionWallet.CreateDefault(5000m)
            };

            var agent = AgentIdentity.Create(AgentRole.Outreach);

            var parameters = new Dictionary<string, object>
            {
                { "phoneNumber", "+91 76200 65818" },
                { "contactName", "Mayur Prabhune" },
                { "companyName", "Bitbloom Enterprise" },
                { "isTestCall", true }
            };

            var result = await registry.ExecuteToolAsync(agent, AgentActionType.DispatchVoiceCall, mission, parameters);

            result.Success.Should().BeTrue();
            result.CostINR.Should().Be(10.00m);
            mission.Wallet.ConsumedSpendINR.Should().Be(10.00m);
            mission.Wallet.ReservedSpendINR.Should().Be(0.00m);
        }
    }
}
