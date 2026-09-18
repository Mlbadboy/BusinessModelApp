using System;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Commercial;
using BusinessModelApp.Infrastructure.Runtime.Enterprise.Commercial;
using FluentAssertions;
using Xunit;

namespace BusinessModelApp.Tests.Domain
{
    public sealed class Phase4Batch440CommercialKernelTests
    {
        private readonly InMemoryCommercialKernelStore _store;
        private readonly CommercialKernelService _service;

        public Phase4Batch440CommercialKernelTests()
        {
            _store = new InMemoryCommercialKernelStore();
            _service = new CommercialKernelService(_store);
        }

        [Fact]
        public void KERN440_01_ConstitutionalInvariants_LawI40_FullCoverage()
        {
            CommercialConstitutionalInvariants.Axiom.Should().Contain("OPPORTUNITY != LEAD != QUALIFIED PROSPECT");
            CommercialConstitutionalInvariants.AllLaws.Should().HaveCount(26);
            CommercialConstitutionalInvariants.LawI40A_OpportunityNotCustomer.Should().StartWith("I40-A");
            CommercialConstitutionalInvariants.LawI40C_AgentBeliefNotTruth.Should().StartWith("I40-C");
            CommercialConstitutionalInvariants.LawI40P_FinancialActionsUnderBatch6.Should().StartWith("I40-P");
            CommercialConstitutionalInvariants.LawI40Z_SubordinateToBatch6.Should().StartWith("I40-Z");
        }

        [Fact]
        public async Task KERN440_02_InitializeEntity_SetsLeadStageAndGenesisHash()
        {
            var record = await _service.InitializeCommercialEntityAsync("tenant-440", "opp-100", "acc-200");

            record.Should().NotBeNull();
            record.CurrentStage.Should().Be(CommercialStage.LEAD);
            record.History.Should().HaveCount(1);
            record.History[0].PreviousBlockHash.Should().Be("GENESIS");
            record.LatestBlockHash.Should().NotBeNullOrWhiteSpace();

            var isValid = await _service.VerifyAuditTrailIntegrityAsync("tenant-440", record.CommercialEntityId);
            isValid.Should().BeTrue();
        }

        [Fact]
        public async Task KERN440_03_TransitionWithoutEvidence_IsRejectedUnderLawI40C()
        {
            var record = await _service.InitializeCommercialEntityAsync("tenant-440", "opp-101", "acc-201");

            var result = await _service.AttemptTransitionAsync(new CommercialTransitionRequest
            {
                TenantId = "tenant-440",
                CommercialEntityId = record.CommercialEntityId,
                TargetStage = CommercialStage.QUALIFYING,
                InitiatingAgentId = "agent-scout-01",
                AuthoritativeEvidenceId = "" // Missing evidence!
            });

            result.IsSuccess.Should().BeFalse();
            result.ConstitutionalViolations.Should().Contain(v => v.Contains("I40-C"));
        }

        [Fact]
        public async Task KERN440_04_ArbitraryStageSkip_IsRejectedUnderLawI40K()
        {
            var record = await _service.InitializeCommercialEntityAsync("tenant-440", "opp-102", "acc-202");

            // Attempt jumping directly from LEAD (0) to MEETING_REQUESTED (4)
            var result = await _service.AttemptTransitionAsync(new CommercialTransitionRequest
            {
                TenantId = "tenant-440",
                CommercialEntityId = record.CommercialEntityId,
                TargetStage = CommercialStage.MEETING_REQUESTED,
                InitiatingAgentId = "agent-scout-01",
                AuthoritativeEvidenceId = "ev-skip-01"
            });

            result.IsSuccess.Should().BeFalse();
            result.ConstitutionalViolations.Should().Contain(v => v.Contains("I40-K"));
        }

        [Fact]
        public async Task KERN440_05_DealWonWithoutHumanSignoff_IsRejectedUnderLawI40W()
        {
            var record = await _service.InitializeCommercialEntityAsync("tenant-440", "opp-103", "acc-203");
            record.CurrentStage = CommercialStage.COMMERCIAL_APPROVAL;
            await _store.SaveEntityAsync(record);

            var result = await _service.AttemptTransitionAsync(new CommercialTransitionRequest
            {
                TenantId = "tenant-440",
                CommercialEntityId = record.CommercialEntityId,
                TargetStage = CommercialStage.DEAL_WON,
                InitiatingAgentId = "agent-sales-01",
                AuthoritativeEvidenceId = "ev-contract-draft",
                HumanSignoffId = null // Missing PRG-1 signoff!
            });

            result.IsSuccess.Should().BeFalse();
            result.ConstitutionalViolations.Should().Contain(v => v.Contains("I40-W"));
        }

        [Fact]
        public async Task KERN440_06_FinancialStageWithoutBatch6Permit_IsRejectedUnderLawI40P()
        {
            var record = await _service.InitializeCommercialEntityAsync("tenant-440", "opp-104", "acc-204");
            record.CurrentStage = CommercialStage.ACCEPTANCE;
            await _store.SaveEntityAsync(record);

            var result = await _service.AttemptTransitionAsync(new CommercialTransitionRequest
            {
                TenantId = "tenant-440",
                CommercialEntityId = record.CommercialEntityId,
                TargetStage = CommercialStage.INVOICED,
                InitiatingAgentId = "agent-finance-01",
                AuthoritativeEvidenceId = "ev-acceptance-signoff",
                IsBatch6Authorized = false // Direct unpermitted attempt!
            });

            result.IsSuccess.Should().BeFalse();
            result.ConstitutionalViolations.Should().Contain(v => v.Contains("I40-P"));
        }

        [Fact]
        public async Task KERN440_07_SequentialTransitionWithBatch6Permit_SucceedsAndChainsCryptographically()
        {
            var record = await _service.InitializeCommercialEntityAsync("tenant-440", "opp-105", "acc-205");

            // 1. LEAD -> QUALIFYING
            var step1 = await _service.AttemptTransitionAsync(new CommercialTransitionRequest
            {
                TenantId = "tenant-440",
                CommercialEntityId = record.CommercialEntityId,
                TargetStage = CommercialStage.QUALIFYING,
                InitiatingAgentId = "agent-scout-01",
                AuthoritativeEvidenceId = "ev-firmographic-match"
            });
            step1.IsSuccess.Should().BeTrue();
            step1.CurrentStage.Should().Be(CommercialStage.QUALIFYING);

            // 2. QUALIFYING -> QUALIFIED
            var step2 = await _service.AttemptTransitionAsync(new CommercialTransitionRequest
            {
                TenantId = "tenant-440",
                CommercialEntityId = record.CommercialEntityId,
                TargetStage = CommercialStage.QUALIFIED,
                InitiatingAgentId = "agent-scout-01",
                AuthoritativeEvidenceId = "ev-pain-point-verified"
            });
            step2.IsSuccess.Should().BeTrue();
            step2.CurrentStage.Should().Be(CommercialStage.QUALIFIED);

            // Verify integrity of the multi-block chain
            var isValid = await _service.VerifyAuditTrailIntegrityAsync("tenant-440", record.CommercialEntityId);
            isValid.Should().BeTrue();
        }
    }
}
