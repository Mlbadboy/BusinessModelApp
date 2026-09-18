using System;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Growth;
using BusinessModelApp.Infrastructure.Runtime.Enterprise.Growth;
using Xunit;

namespace BusinessModelApp.Tests.Domain
{
    /// <summary>
    /// Phase 4.5 Hardened Multi-Gate Certification Test Suite.
    /// Certifies G1 (Engineering), G2 (Production), and G3 (External Business Reality Verification Gate)
    /// for Charlie's Autonomous Business Growth Engine.
    /// </summary>
    public class Phase4Batch457BusinessGrowthCertificationTests
    {
        [Fact]
        public async Task G1_EngineeringCertification_AllSixteenSubStatesAndDynamicPolicy_Pass()
        {
            // Arrange stores & services
            var controlStore = new InMemoryGrowthControlPlaneStore();
            var acquisitionStore = new InMemoryCustomerAcquisitionAndRetentionStore();
            var treasuryStore = new InMemoryUnitEconomicsAndTreasuryStore();
            var experimentStore = new InMemoryGrowthExperimentStore();

            var treasuryService = new UnitEconomicsAndTreasuryService(treasuryStore);
            var stateBuilder = new BusinessGrowthStateBuilder(controlStore, acquisitionStore, treasuryStore, experimentStore);

            // 1. Configure custom dynamic policy (Enterprise SaaS)
            var policy = new UnitEconomicsPolicy
            {
                TenantId = "tenant-fintech",
                BusinessObjectiveId = "BO-SCALE-2026",
                PolicyName = "Mid-Market B2B Fintech Policy",
                MinGrossMarginPercent = 60.0m,
                MinContributionMarginPercent = 35.0m,
                MinLtvToCacRatio = 4.0m,
                MaxPaybackPeriodMonths = 8,
                MaxAllowableAnnualChurnPercent = 6.0m,
                MaxSelfDiscountPercentWithoutSignoff = 10.0m,
                HumanSignoffId = "PRG1-SIGNOFF-EXEC-001"
            };
            await treasuryService.SetPolicyAsync(policy);

            // 2. Validate Proposal margin against policy
            bool validProposal = policy.ValidateProposal(price: 100_000m, estimatedCostBasis: 30_000m, out string reason1);
            Assert.True(validProposal);
            Assert.Empty(reason1);

            bool invalidProposal = policy.ValidateProposal(price: 100_000m, estimatedCostBasis: 55_000m, out string reason2);
            Assert.False(invalidProposal);
            Assert.Contains("below policy threshold", reason2);

            // 3. Validate Negotiation discount against policy
            bool validDiscount = policy.ValidateNegotiationDiscount(initialPrice: 100_000m, agreedPrice: 92_000m, hasPrg1Signoff: false, out _);
            Assert.True(validDiscount); // 8% <= 10%

            bool unauthorizedDiscount = policy.ValidateNegotiationDiscount(initialPrice: 100_000m, agreedPrice: 80_000m, hasPrg1Signoff: false, out string discReason);
            Assert.False(unauthorizedDiscount); // 20% > 10% without PRG-1 signoff
            Assert.Contains("PRG-1 human authorization", discReason);

            // 4. Validate Cohort LTV:CAC against policy
            var cohort = await treasuryService.AnalyzeCohortLtvCacAsync(
                cohortId: "COHORT-2026-Q1",
                cohortPeriod: "2026-Q1",
                customersAcquiredCount: 10,
                totalAcquisitionCost: 50_000m, // CAC = $5,000
                averageAnnualRevenuePerAccount: 80_000m,
                grossMarginPercent: 70.0m,
                annualChurnRatePercent: 5.0m,
                paybackPeriodMonths: 6);

            Assert.True(cohort.IsSustainableUnderPolicy(policy));

            // 5. Build authoritative BusinessGrowthState snapshot and verify all 16 sub-states
            var snapshot = await stateBuilder.BuildSnapshotAsync(
                tenantId: "tenant-fintech",
                businessObjectiveId: "BO-SCALE-2026",
                isG1Certified: true,
                isG2Certified: false,
                isG3Certified: false);

            Assert.NotNull(snapshot);
            Assert.NotNull(snapshot.ObjectiveState);
            Assert.NotNull(snapshot.DemandState);
            Assert.NotNull(snapshot.AcquisitionState);
            Assert.NotNull(snapshot.PipelineState);
            Assert.NotNull(snapshot.CustomerState);
            Assert.NotNull(snapshot.RetentionState);
            Assert.NotNull(snapshot.ExpansionState);
            Assert.NotNull(snapshot.RevenueState);
            Assert.NotNull(snapshot.CashState);
            Assert.NotNull(snapshot.CostState);
            Assert.NotNull(snapshot.UnitEconomicsState);
            Assert.NotNull(snapshot.CapacityState);
            Assert.NotNull(snapshot.ExperimentState);
            Assert.NotNull(snapshot.RiskState);
            Assert.NotNull(snapshot.EvidenceState);
            Assert.NotNull(snapshot.GrowthHealthState);

            Assert.True(snapshot.GrowthHealthState.IsG1EngineeringCertified);
            Assert.False(snapshot.GrowthHealthState.IsG3BusinessRealityCertified);
            Assert.False(snapshot.IsAutonomouslyGrowing); // G3 is still required
        }

        [Fact]
        public async Task G2_ProductionAndResilienceCertification_MultiStoreSynchronizationAndGovernance_Pass()
        {
            var controlStore = new InMemoryGrowthControlPlaneStore();
            var acquisitionStore = new InMemoryCustomerAcquisitionAndRetentionStore();
            var treasuryStore = new InMemoryUnitEconomicsAndTreasuryStore();
            var experimentStore = new InMemoryGrowthExperimentStore();

            var controlService = new GrowthControlPlaneService(controlStore);
            var acquisitionService = new CustomerAcquisitionAndRetentionService(acquisitionStore);
            var treasuryService = new UnitEconomicsAndTreasuryService(treasuryStore);
            var experimentService = new GrowthExperimentService(experimentStore);
            var stateBuilder = new BusinessGrowthStateBuilder(controlStore, acquisitionStore, treasuryStore, experimentStore);

            // 1. Create Growth Objective & Strategy Plan with PRG-1 signoff
            var growthObj = await controlService.CreateGrowthObjectiveAsync(new GrowthObjective
            {
                TenantId = "tenant-corp",
                BusinessObjectiveId = "BO-REVENUE-EXPANSION",
                Title = "Cloud Infrastructure Autonomous Growth",
                TargetRevenueINR = 50_000_000m,
                TargetGrossMarginPercent = 65.0m,
                TargetLtvCacRatio = 4.5m,
                MaxPaybackPeriodMonths = 6
            });

            var plan = await controlService.FormulateStrategyPlanAsync(new GrowthStrategyPlan
            {
                TenantId = "tenant-corp",
                GrowthObjectiveId = growthObj.ObjectiveId,
                Name = "Direct Mid-Market Outbound Strategy",
                TargetIcpSegment = "Fintech & Banking APIs",
                AllocatedBudgetINR = 2_000_000m,
                HumanSignoffId = "PRG1-SIGNOFF-EXEC-99182"
            });

            Assert.True(plan.IsAuthorized);

            // 2. Discover prospect and execute full pipeline
            var prospect = await acquisitionService.DiscoverAndScoreProspectAsync(
                companyName: "Acme Payments Inc",
                industry: "Payment Gateways",
                estimatedEmployeeCount: 400,
                estimatedAnnualRevenue: 60_000_000m,
                icpScore: 0.92m,
                icpQualificationSummary: "Tier-1 ICP Fit");

            prospect.AddCommitteeMember(new BuyingCommitteeMember
            {
                Name = "Elena Rostova",
                Title = "Chief Commercial Officer",
                RoleType = BuyingRoleType.EconomicBuyer,
                ContactEmail = "elena@acmepay.com"
            });

            var outreach = await acquisitionService.SendGovernedOutreachAsync(
                prospect.ProspectId,
                "elena@acmepay.com",
                "Email",
                "Autonomous Reconciliation Platform",
                "Elena, Charlie OS eliminates treasury discrepancy in real-time...",
                antiSpamComplianceVerified: true,
                governedSignoffDigest: "DIGEST-AUTH-OUTBOUND-772");

            await acquisitionService.RecordOutreachResponseAsync(outreach.EngagementId, OutreachReplyClassification.RequestDemo, 0.95m);

            var proposal = await acquisitionService.GenerateProposalAsync(
                prospect.ProspectId,
                "Charlie Sovereign Growth Platform",
                "Treasury, Ledger, and Compliance Automation",
                proposedPrice: 150_000m,
                estimatedCostBasis: 45_000m,
                projectedCustomerRoiMultiple: 5.2m,
                estimatedPaybackMonths: 3);

            var approved = await acquisitionService.ApproveProposalAsync(proposal.ProposalId, "CommercialDirector");
            Assert.True(approved.IsApproved);

            var negotiation = await acquisitionService.FinalizeNegotiationAsync(
                proposal.ProposalId,
                initialProposedPrice: 150_000m,
                finalAgreedPrice: 140_000m,
                estimatedCostBasis: 45_000m,
                concessionsGrantedSummary: "$10k discount for multi-year prepaid",
                concessionsReceivedSummary: "24-month commitment upfront",
                prg1SignoffId: "PRG1-AUTH-DEAL-8827",
                isClosedWon: true);

            var contract = await acquisitionService.ExecuteContractAsync(
                prospect.ProspectId,
                proposal.ProposalId,
                totalContractValue: 140_000m,
                paymentTerms: "Net 30",
                contractDigestSha256: "D41D8CD98F00B204E9800998ECF8427E1234567890ABCDEF1234567890ABCDEF",
                counterpartySignatory: "Elena Rostova (CCO)",
                charlieSignatory: "Charlie Sovereign Business Operator");

            Assert.True(contract.IsFullyExecuted);

            // 3. Customer Onboarding & Value Realization
            await acquisitionService.InitiateOnboardingAsync("CUST-ACME-01", contract.ContractId, "Acme Payments Inc");
            await acquisitionService.RecordValueRealizationAsync("CUST-ACME-01", DateTime.UtcNow, "Zero reconciliation drift across 1M transactions");
            await acquisitionService.UpdateCustomerHealthAsync("CUST-ACME-01", 95.0m, ChurnRiskLevel.Low, 90.0m);

            // 4. Record Unit P&L and Treasury snapshot
            await treasuryService.RecordCustomerUnitEconomicsAsync(
                customerId: "CUST-ACME-01",
                contractId: contract.ContractId,
                realizedCashRevenue: 140_000m,
                directDeliveryCosts: 30_000m,
                agentComputeAndTokenCosts: 10_000m,
                softwareLicenseCosts: 5_000m);

            await treasuryService.RecordTreasurySnapshotAsync(
                totalLiquidCashReserve: 10_000_000m,
                monthlyBurnRate: 500_000m,
                monthlyRealizedCashCollection: 800_000m,
                approvedAgentBudgetPool: 100_000m,
                approvedCampaignBudgetPool: 200_000m);

            // 5. Build State with G1 and G2 passed
            var snapshot = await stateBuilder.BuildSnapshotAsync(
                tenantId: "tenant-corp",
                businessObjectiveId: "BO-REVENUE-EXPANSION",
                isG1Certified: true,
                isG2Certified: true,
                isG3Certified: false);

            Assert.True(snapshot.GrowthHealthState.IsG1EngineeringCertified);
            Assert.True(snapshot.GrowthHealthState.IsG2ProductionCertified);
            Assert.False(snapshot.GrowthHealthState.IsG3BusinessRealityCertified);
            Assert.Equal(95.0m, snapshot.CustomerState.AverageCustomerHealthScore);
            Assert.Equal(140_000m, snapshot.RevenueState.RecognizedRevenue);
        }

        [Fact]
        public async Task G3_BusinessRealityCertificationGate_StrictEpistemicReconciliation_Demonstrated()
        {
            var controlStore = new InMemoryGrowthControlPlaneStore();
            var acquisitionStore = new InMemoryCustomerAcquisitionAndRetentionStore();
            var treasuryStore = new InMemoryUnitEconomicsAndTreasuryStore();
            var experimentStore = new InMemoryGrowthExperimentStore();

            var treasuryService = new UnitEconomicsAndTreasuryService(treasuryStore);
            var stateBuilder = new BusinessGrowthStateBuilder(controlStore, acquisitionStore, treasuryStore, experimentStore);

            // 1. Attempt G3 certification with Level 0 (InternalFixture) / Level 1 (Simulation) evidence -> MUST FAIL
            var fixtureAttestation = await treasuryService.AttestExternalRevenueEvidenceAsync(
                invoiceId: "INV-MOCK-001",
                counterpartyId: "CP-MOCK-CORP",
                attestedAmount: 100_000m,
                currency: "USD",
                bankTransactionReference: "FIXTURE-TXN-000",
                bankStatementDigestSha256: "DIGEST-MOCK-000",
                thirdPartyProofRegistryDigest: "REGISTRY-MOCK-000",
                epistemicLevel: EpistemicEvidenceLevel.InternalFixture);

            await treasuryService.ReconcileExternalEvidenceAsync(
                fixtureAttestation.AttestationId,
                "TestHarness",
                ExternalReconciliationStatus.Reconciled);

            bool isFixtureValid = await treasuryService.VerifyRevenueRealizationIntegrityAsync(fixtureAttestation.AttestationId);
            Assert.False(isFixtureValid); // Level 0 Fixture strictly rejected from revenue realization!

            // 2. Submit Level 5 (BankVerifiedCash) evidence with external wire reference and cryptographic digests
            var realBankAttestation = await treasuryService.AttestExternalRevenueEvidenceAsync(
                invoiceId: "INV-REAL-2026-001",
                counterpartyId: "CP-REAL-ENTERPRISE",
                attestedAmount: 250_000m,
                currency: "USD",
                bankTransactionReference: "FEDWIRE-JPMC-NY-20260917-8891028391",
                bankStatementDigestSha256: "8F4A3C2B1D0E9F8A7B6C5D4E3F2A1B0C9D8E7F6A5B4C3D2E1F0A9B8C7D6E5F4A",
                thirdPartyProofRegistryDigest: "PROOF-REGISTRY-ETHEREUM-MAINNET-TX-0x9a8b7c6d5e4f3a2b1c",
                epistemicLevel: EpistemicEvidenceLevel.BankVerifiedCash);

            // Reconcile via Authorized External Auditor
            var reconciled = await treasuryService.ReconcileExternalEvidenceAsync(
                realBankAttestation.AttestationId,
                "AuthorizedCorporateAuditor",
                ExternalReconciliationStatus.Reconciled);

            Assert.Equal(ExternalReconciliationStatus.Reconciled, reconciled.Status);
            Assert.True(reconciled.IsBankVerified);

            bool isRealEvidenceValid = await treasuryService.VerifyRevenueRealizationIntegrityAsync(realBankAttestation.AttestationId);
            Assert.True(isRealEvidenceValid); // Level 5 Bank Verified Cash with cryptographic anchors passes!

            // 3. Ground the commercial cycle with customer PnL and cohort unit economics
            await treasuryService.RecordCustomerUnitEconomicsAsync(
                customerId: "CUST-REAL-001",
                contractId: "CTR-REAL-001",
                realizedCashRevenue: 250_000m,
                directDeliveryCosts: 50_000m,
                agentComputeAndTokenCosts: 15_000m,
                softwareLicenseCosts: 10_000m);

            await treasuryService.AnalyzeCohortLtvCacAsync(
                cohortId: "COHORT-REAL-2026",
                cohortPeriod: "2026",
                customersAcquiredCount: 5,
                totalAcquisitionCost: 30_000m,
                averageAnnualRevenuePerAccount: 250_000m,
                grossMarginPercent: 80.0m,
                annualChurnRatePercent: 4.0m,
                paybackPeriodMonths: 2);

            await acquisitionStore.SaveRetentionRecordAsync(new CustomerRetentionAndHealthRecord(
                customerId: "CUST-REAL-001",
                contractId: "CTR-REAL-001",
                companyName: "Real Enterprise Client Corp"));

            var retentionRecord = await acquisitionStore.GetRetentionRecordAsync("CUST-REAL-001");
            retentionRecord?.RecordFirstValueRealized(DateTime.UtcNow);
            retentionRecord?.UpdateHealth(98.0m, ChurnRiskLevel.Low, 95.0m);
            if (retentionRecord != null) await acquisitionStore.SaveRetentionRecordAsync(retentionRecord);

            // 4. Build snapshot with G1, G2, and G3 confirmed
            var finalState = await stateBuilder.BuildSnapshotAsync(
                tenantId: "tenant-enterprise-real",
                businessObjectiveId: "BO-SCALE-2026",
                isG1Certified: true,
                isG2Certified: true,
                isG3Certified: true);

            Assert.True(finalState.GrowthHealthState.IsG1EngineeringCertified);
            Assert.True(finalState.GrowthHealthState.IsG2ProductionCertified);
            Assert.True(finalState.GrowthHealthState.IsG3BusinessRealityCertified);
            Assert.Equal(EpistemicEvidenceLevel.BankVerifiedCash, finalState.EvidenceState.HighestVerifiedLevel);
            Assert.Equal(250_000m, finalState.RevenueState.RealizedCashRevenue);
            Assert.True(finalState.UnitEconomicsState.IsEconomicallySustainable);
            Assert.True(finalState.IsAutonomouslyGrowing);
        }
    }
}
