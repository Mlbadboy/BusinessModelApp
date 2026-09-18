using System;
using System.Threading.Tasks;
using BusinessModelApp.Api.Controllers;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Workforce;
using BusinessModelApp.Infrastructure.Runtime.Enterprise.Workforce;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace BusinessModelApp.Tests.Domain
{
    public sealed class Phase4Batch435AccountAndLineageTests
    {
        private readonly InMemoryCommercialAccountAndLineageStore _store;
        private readonly GovernedCrmService _crmService;
        private readonly AccountGraphService _graphService;
        private readonly CommercialLineageService _lineageService;

        public Phase4Batch435AccountAndLineageTests()
        {
            _store = new InMemoryCommercialAccountAndLineageStore();
            _crmService = new GovernedCrmService(_store);
            _graphService = new AccountGraphService(_store);
            _lineageService = new CommercialLineageService(_store);
        }

        // =========================================================================
        // Family 1: Governed CRM Mutations (Batch 6 Sovereignty)
        // =========================================================================

        [Fact]
        public async Task CRM435_01_UnauthenticatedMutation_IsRejected()
        {
            var mutation = new GovernedCrmMutation
            {
                AgentId = "agent-rogue",
                EntityType = "Opportunity",
                MutationType = "UPDATE_STAGE",
                EntityId = "opp-123",
                PayloadJson = "{\"stage\":\"CLOSED_WON\"}",
                IsBatch6Authorized = false // Unauthorized direct attempt
            };

            var act = () => _crmService.SubmitGovernedMutationAsync("tenant-acc-435", mutation);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Governance Violation*Batch 6 Execution Firewall*");
        }

        [Fact]
        public async Task CRM435_02_Batch6AuthorizedMutation_Succeeds()
        {
            var mutation = new GovernedCrmMutation
            {
                AgentId = "agent-sales-01",
                EntityType = "Lead",
                MutationType = "CREATE",
                EntityId = "lead-456",
                PayloadJson = "{\"company\":\"NexusCorp\"}",
                IsBatch6Authorized = true // Formal ExecutionPermit issued by Batch 6
            };

            var success = await _crmService.SubmitGovernedMutationAsync("tenant-acc-435", mutation);
            success.Should().BeTrue();

            var list = await _crmService.ListMutationsAsync("tenant-acc-435");
            list.Should().Contain(m => m.EntityId == "lead-456");
        }

        // =========================================================================
        // Family 2: Account Graph & Buying Center Evidence Grounding (Law I39-X)
        // =========================================================================

        [Fact]
        public async Task GRAPH435_03_AccountGraph_PersonasEvidenceGrounding()
        {
            var graph = new AccountGraph
            {
                AccountId = "acc-cloudscale",
                CompanyName = "CloudScale Inc",
                Industry = "FinTech",
                Geography = "IN",
                Personas =
                {
                    new BuyingCenterPersona
                    {
                        Name = "Ananya Sharma",
                        Title = "VP of Infrastructure",
                        Role = BuyingCenterRole.DECISION_MAKER,
                        InfluenceScore = 9,
                        CorroboratingEvidenceIds = { "evidence-linkedin-verified-01" }
                    },
                    new BuyingCenterPersona
                    {
                        Name = "Rajesh Gupta",
                        Title = "Lead Architect",
                        Role = BuyingCenterRole.CHAMPION,
                        InfluenceScore = 8,
                        CorroboratingEvidenceIds = { } // Uncorroborated
                    }
                }
            };

            var saved = await _graphService.SaveAccountGraphAsync("tenant-acc-435", graph);

            saved.Personas[0].IsGroundedInEvidence.Should().BeTrue();
            saved.Personas[1].IsGroundedInEvidence.Should().BeFalse();
        }

        // =========================================================================
        // Family 3: Cryptographic Revenue Lineage & Tamper Detection (Law I39-Y)
        // =========================================================================

        [Fact]
        public async Task LINEAGE435_04_CryptographicLineage_ChainsAndVerifiesIntegrity()
        {
            var oppId = "opp-scale-2026";

            // Node 1: Objective & Opportunity Formulation
            var node1 = new CommercialLineageNode
            {
                OpportunityId = oppId,
                BusinessObjectiveId = "obj-q4",
                RevenueObjectiveId = "rev-obj-q4",
                ResponsibilityId = "resp-prospecting",
                WorkProposalId = "prop-01",
                MissionId = "mission-qualify"
            };
            var appended1 = await _lineageService.AppendLineageNodeAsync("tenant-acc-435", node1);
            appended1.PreviousBlockHash.Should().Be("GENESIS");
            appended1.CurrentBlockHash.Should().NotBeNullOrWhiteSpace();

            // Node 2: External Action & Outcome
            var node2 = new CommercialLineageNode
            {
                OpportunityId = oppId,
                MissionId = "mission-qualify",
                ActionId = "act-book-meeting",
                ExternalEffectId = "eff-email-send-01",
                OutcomeId = "out-meeting-booked"
            };
            var appended2 = await _lineageService.AppendLineageNodeAsync("tenant-acc-435", node2);
            appended2.PreviousBlockHash.Should().Be(appended1.CurrentBlockHash);

            // Node 3: Commercial Closing, Invoice & Payment
            var node3 = new CommercialLineageNode
            {
                OpportunityId = oppId,
                DealId = "deal-won-99",
                InvoiceId = "inv-2026-001",
                PaymentId = "pay-stripe-999",
                RevenueRecordId = "rev-rec-888"
            };
            var appended3 = await _lineageService.AppendLineageNodeAsync("tenant-acc-435", node3);
            appended3.PreviousBlockHash.Should().Be(appended2.CurrentBlockHash);

            // Verify integrity of valid chain
            var isValid = await _lineageService.VerifyLineageIntegrityAsync("tenant-acc-435", oppId);
            isValid.Should().BeTrue();

            // Tamper test: Modify a field in historical node 1
            node1.BusinessObjectiveId = "obj-tampered";
            var isStillValid = await _lineageService.VerifyLineageIntegrityAsync("tenant-acc-435", oppId);
            isStillValid.Should().BeFalse(); // Cryptographic mismatch detected!
        }

        // =========================================================================
        // Family 4: Immutable External Effect Recording
        // =========================================================================

        [Fact]
        public async Task EFFECT435_05_ImmutableExternalEffect_RecordsWithIdempotency()
        {
            var effect = new ImmutableExternalEffect
            {
                EffectId = "eff-stripe-charge-01",
                Connector = "StripeConnector",
                Provider = "Stripe",
                RequestHash = "sha256-req-charge-1000",
                IdempotencyKey = "idem-pay-1000-user42",
                ProviderReference = "ch_3M4k5L2eZvKYlo2C1g9abc",
                EffectState = "SUCCEEDED",
                VerificationEvidence = "StripeWebhookSignatureVerified"
            };

            var recorded = await _lineageService.RecordExternalEffectAsync("tenant-acc-435", effect);
            recorded.EffectId.Should().Be("eff-stripe-charge-01");

            var retrieved = await _lineageService.GetExternalEffectAsync("tenant-acc-435", "eff-stripe-charge-01");
            retrieved.Should().NotBeNull();
            retrieved!.ProviderReference.Should().Be("ch_3M4k5L2eZvKYlo2C1g9abc");
        }
    }
}
