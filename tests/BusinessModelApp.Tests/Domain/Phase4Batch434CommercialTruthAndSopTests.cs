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
    public sealed class Phase4Batch434CommercialTruthAndSopTests
    {
        private readonly InMemoryCommercialTruthAndSopStore _store;
        private readonly CommercialTruthAndSopService _service;
        private readonly CommercialTruthAndSopController _controller;

        public Phase4Batch434CommercialTruthAndSopTests()
        {
            _store = new InMemoryCommercialTruthAndSopStore();
            _service = new CommercialTruthAndSopService(_store);
            _controller = new CommercialTruthAndSopController(_service, _service);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            _controller.ControllerContext.HttpContext.Request.Headers["X-Tenant-ID"] = "tenant-truth-434";
        }

        // =========================================================================
        // Family 1: Commercial Truth & Epistemic Separation (Law I39-Q)
        // =========================================================================

        [Fact]
        public async Task TRUTH434_01_ClaimedIsNotVerified_RequiresCorroboratingEvidence()
        {
            var oppId = "opp-fintech-01";

            // Agent claims customer signed contract
            var fact = await _service.RecordCommercialClaimAsync(
                "tenant-truth-434",
                oppId,
                CommercialTruthState.CONTRACT_VERIFIED,
                "Agent claims customer agreed and signed contract in meeting");

            fact.State.Should().Be(CommercialTruthState.CONTRACT_VERIFIED);
            fact.IsVerified.Should().BeFalse(); // Law I39-Q: CLAIMED != VERIFIED
            fact.CorroboratingEvidence.Should().BeEmpty();

            // Provide authoritative corroborating evidence (Signed PDF with SHA-256 hash)
            var evidence = new CommercialEvidenceItem
            {
                EvidenceType = "SignedPdf",
                SourceSystem = "DocuSignConnector",
                PayloadHash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
                CorroborationConfidence = 0.98m,
                VerifiedBy = "DocuSignWebhook"
            };

            var verifiedFact = await _service.CorroborateCommercialFactAsync("tenant-truth-434", oppId, evidence);

            verifiedFact.IsVerified.Should().BeTrue();
            verifiedFact.CorroboratingEvidence.Should().HaveCount(1);
            verifiedFact.CorroboratingEvidence[0].SourceSystem.Should().Be("DocuSignConnector");
        }

        // =========================================================================
        // Family 2: Canonical Business SOP Compilation
        // =========================================================================

        [Fact]
        public async Task SOP434_02_CanonicalSops_AreRegisteredByDefault()
        {
            var sops = await _service.ListSopDefinitionsAsync("tenant-truth-434");

            sops.Should().Contain(s => s.SopId == "LEAD_QUALIFICATION_SOP");
            sops.Should().Contain(s => s.SopId == "SALES_SOP");
            sops.Should().Contain(s => s.SopId == "DELIVERY_SOP");
            sops.Should().Contain(s => s.SopId == "COLLECTION_SOP");
        }

        [Fact]
        public async Task SOP434_03_CompileSopToWorkProposal_GeneratesStructuredProposal()
        {
            var proposal = await _service.CompileSopToWorkProposalAsync(
                "tenant-truth-434",
                "SALES_SOP",
                "opp-healthtech-99",
                "Execute Sales Flow for HealthTech Corp");

            proposal.Should().NotBeNull();
            proposal.SopId.Should().Be("SALES_SOP");
            proposal.PlannedMissionTitles.Should().HaveCount(3);
            proposal.PlannedMissionTitles[0].Should().Contain("OutreachPreparation");
            proposal.EstimatedTotalCost.Should().Be(75.0m);
            proposal.MaxRiskLevel.Should().Be(2);
            proposal.RequiresHumanApproval.Should().BeFalse(); // Risk 2 does not mandate PRG-1 blocking
        }

        // =========================================================================
        // Family 3: Controller API Endpoints
        // =========================================================================

        [Fact]
        public async Task TRUTH434_04_ControllerEndpoints_RecordAndCorroborateClaims()
        {
            var claimResult = await _controller.RecordClaim(new RecordClaimRequest
            {
                OpportunityId = "opp-api-test",
                ClaimedState = CommercialTruthState.MEETING_CONFIRMED,
                Description = "Client booked calendar meeting"
            }) as OkObjectResult;
            claimResult.Should().NotBeNull();

            var getResult = await _controller.GetFact("opp-api-test") as OkObjectResult;
            getResult.Should().NotBeNull();
        }
    }
}
