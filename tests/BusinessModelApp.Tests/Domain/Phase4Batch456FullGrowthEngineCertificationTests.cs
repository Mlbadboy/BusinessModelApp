using System;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Growth;
using BusinessModelApp.Infrastructure.Runtime.Enterprise.Growth;
using Xunit;

namespace BusinessModelApp.Tests.Domain
{
    /// <summary>
    /// Phase 4.5 Sub-Batch 4.5.18 & 4.5.19: Complete Autonomous Growth Engine Lifecycle Certification.
    /// Certifies Charlie's transition into a continuously operating business that produces real value,
    /// acquires customers, delivers real solutions, realizes verifiable cash, and optimizes enterprise compound value.
    /// </summary>
    public class Phase4Batch456FullGrowthEngineCertificationTests
    {
        [Fact]
        public async Task CharlieAutonomousGrowthEngine_EndToEndLifecycle_FullyCertified()
        {
            // 1. Initialize Storage & Services
            var growthControlStore = new InMemoryGrowthControlPlaneStore();
            var growthControlService = new GrowthControlPlaneService(growthControlStore);

            var acquisitionStore = new InMemoryCustomerAcquisitionAndRetentionStore();
            var acquisitionService = new CustomerAcquisitionAndRetentionService(acquisitionStore);

            var economicsStore = new InMemoryUnitEconomicsAndTreasuryStore();
            var economicsService = new UnitEconomicsAndTreasuryService(economicsStore);

            var experimentStore = new InMemoryGrowthExperimentStore();
            var experimentService = new GrowthExperimentService(experimentStore);

            // 2. Control Plane: Create Sovereign Growth Objective & Strategy (Laws I41-A, I41-Y)
            var growthObj = await growthControlService.CreateGrowthObjectiveAsync(new GrowthObjective
            {
                TenantId = "tenant-enterprise",
                BusinessObjectiveId = "BO-ENTERPRISE-SCALE-2026",
                Title = "Automate Commercial Growth for FinTech Mid-Market",
                TargetRevenueINR = 10_000_000m,
                TargetGrossMarginPercent = 45.0m,
                TargetLtvCacRatio = 5.0m,
                MaxPaybackPeriodMonths = 4
            });

            Assert.NotNull(growthObj);
            Assert.Equal("BO-ENTERPRISE-SCALE-2026", growthObj.BusinessObjectiveId);

            var strategyPlan = await growthControlService.FormulateStrategyPlanAsync(new GrowthStrategyPlan
            {
                TenantId = "tenant-enterprise",
                GrowthObjectiveId = growthObj.ObjectiveId,
                Name = "Autonomous FinTech Acquisition Plan",
                TargetIcpSegment = "FinTech Infrastructure",
                HumanSignoffId = "PRG1-SIGNOFF-EXECUTIVE-BOARD-001"
            });

            Assert.True(strategyPlan.IsAuthorized);

            // 3. Customer Acquisition: ICP Discovery & Buying Committee
            var prospect = await acquisitionService.DiscoverAndScoreProspectAsync(
                companyName: "Horizon Payments Corp",
                industry: "Financial Infrastructure",
                estimatedEmployeeCount: 750,
                estimatedAnnualRevenue: 85_000_000m,
                icpScore: 0.94m,
                icpQualificationSummary: "Perfect ICP fit: High monthly reconciliation volume, manual audit overhead.");

            prospect.AddCommitteeMember(new BuyingCommitteeMember
            {
                Name = "David Vance",
                Title = "SVP Treasury Operations",
                RoleType = BuyingRoleType.EconomicBuyer,
                ContactEmail = "dvance@horizonpay.com"
            });
            prospect.AddCommitteeMember(new BuyingCommitteeMember
            {
                Name = "Sarah Chen",
                Title = "Head of Platform Architecture",
                RoleType = BuyingRoleType.TechnicalEvaluator,
                ContactEmail = "schen@horizonpay.com"
            });

            Assert.Equal(2, prospect.BuyingCommittee.Count);

            // 4. Governed Outbound Engagement (Anti-spam & PRG verified)
            var outreach = await acquisitionService.SendGovernedOutreachAsync(
                prospect.ProspectId,
                "dvance@horizonpay.com",
                "Email",
                "Continuous automated treasury reconciliation for Horizon Payments",
                "David, our autonomous business operator Charlie eliminates manual monthly close reconciliation...",
                antiSpamComplianceVerified: true,
                governedSignoffDigest: "DIGEST-PRG-OUTBOUND-AUTH-9921");

            Assert.True(outreach.AntiSpamComplianceVerified);

            // Inbound positive response
            await acquisitionService.RecordOutreachResponseAsync(
                outreach.EngagementId,
                OutreachReplyClassification.RequestDemo,
                sentimentScore: 0.92m);

            Assert.Equal(OutreachReplyClassification.RequestDemo, outreach.ReplyClassification);

            // 5. Qualification & Commercial Discovery Meeting
            var meeting = await acquisitionService.ScheduleMeetingAsync(
                prospect.ProspectId,
                "Charlie Sovereign Business OS Demonstration",
                DateTime.UtcNow.AddDays(1),
                "Walkthrough of real-time cash reconciliation and ledger synchronization.");

            await acquisitionService.ConcludeMeetingAsync(
                meeting.MeetingId,
                MeetingOutcomeState.CompletedAdvanceToProposal,
                discoveryNotes: "CFO and SVP Treasury confirmed $400k annual manual overhead. Need solution in 30 days.",
                meddpicQualified: true);

            // 6. Proposal Generation with Guaranteed Margin Floor (Law I41-G)
            // Price: $120,000, Cost Basis: $40,000 -> Gross Margin: 66.7% (>> 35%)
            var proposal = await acquisitionService.GenerateProposalAsync(
                prospect.ProspectId,
                "Charlie Enterprise Treasury Automation Deployment",
                "Automated bank feed reconciliation, ledger mutation verification, and continuous compliance audit.",
                proposedPrice: 120_000m,
                estimatedCostBasis: 40_000m,
                projectedCustomerRoiMultiple: 4.8m,
                estimatedPaybackMonths: 3);

            Assert.True(proposal.GrossMarginPercent >= 35.0m);

            var approvedProposal = await acquisitionService.ApproveProposalAsync(proposal.ProposalId, "VP-CommercialOperations");
            Assert.True(approvedProposal.IsApproved);

            // 7. Commercial Negotiation & Closing (Law I41-H)
            // Final price: $110,000, Cost Basis: $40,000 -> Margin: 63.6% (>> 35%)
            var negotiation = await acquisitionService.FinalizeNegotiationAsync(
                proposal.ProposalId,
                initialProposedPrice: 120_000m,
                finalAgreedPrice: 110_000m,
                estimatedCostBasis: 40_000m,
                concessionsGrantedSummary: "$10k discount for multi-year contract",
                concessionsReceivedSummary: "24-month upfront payment commitment",
                prg1SignoffId: "PRG1-CLOSING-AUTH-8871",
                isClosedWon: true);

            Assert.True(negotiation.IsClosedWon);
            Assert.True(negotiation.FinalGrossMarginPercent >= 35.0m);

            // 8. Contract Execution with Cryptographic Digest
            var contract = await acquisitionService.ExecuteContractAsync(
                prospect.ProspectId,
                proposal.ProposalId,
                totalContractValue: 110_000m,
                paymentTerms: "Net 30",
                contractDigestSha256: "E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855",
                counterpartySignatory: "David Vance (SVP Treasury)",
                charlieSignatory: "Charlie Sovereign AI Operator");

            Assert.True(contract.IsFullyExecuted);

            // 9. Customer Onboarding & Value Realization
            var customer = await acquisitionService.InitiateOnboardingAsync("CUST-HORIZON-01", contract.ContractId, "Horizon Payments Corp");
            await acquisitionService.RecordValueRealizationAsync(
                "CUST-HORIZON-01",
                DateTime.UtcNow.AddDays(5),
                "Autonomous Reconciliation of 500,000 Daily Ledger Entries Verified");

            await acquisitionService.UpdateCustomerHealthAsync("CUST-HORIZON-01", 98.0m, ChurnRiskLevel.Low, npsScore: 90m);
            Assert.Equal(98.0m, customer.HealthScore);

            // 10. Unit Economics & Contribution Margin (Law I41-E: Revenue != Profit)
            var pnl = await economicsService.RecordCustomerUnitEconomicsAsync(
                customerId: "CUST-HORIZON-01",
                contractId: contract.ContractId,
                realizedCashRevenue: 110_000m,
                directDeliveryCosts: 25_000m,
                agentComputeAndTokenCosts: 8_000m,
                softwareLicenseCosts: 4_000m);

            Assert.Equal(110_000m, pnl.RealizedCashRevenue);
            Assert.Equal(37_000m, pnl.TotalLoadedVariableCosts);
            Assert.Equal(73_000m, pnl.NetContributionMargin);
            Assert.True(pnl.ContributionMarginPercent > 60m);

            // 11. Cohort LTV/CAC & Payback Analysis (Law I41-N)
            var cohort = await economicsService.AnalyzeCohortLtvCacAsync(
                cohortId: "COHORT-2026-H1",
                cohortPeriod: "2026-H1",
                customersAcquiredCount: 8,
                totalAcquisitionCost: 32_000m, // CAC = $4,000
                averageAnnualRevenuePerAccount: 110_000m,
                grossMarginPercent: 65m,
                annualChurnRatePercent: 8m,
                paybackPeriodMonths: 2);

            Assert.True(cohort.IsEconomicallySustainable);
            Assert.True(cohort.LtvToCacRatio >= 3.0m);
            Assert.True(cohort.PaybackPeriodMonths <= 12);

            // 12. External Revenue Proof Attestation Anchor (Sub-Batch 4.5.19)
            // Reconciling verified external wire transfer with immutable digests
            var attestation = await economicsService.AttestExternalRevenueEvidenceAsync(
                invoiceId: "INV-HORIZON-2026-001",
                counterpartyId: "CP-HORIZON-PAYMENTS",
                attestedAmount: 110_000m,
                currency: "USD",
                bankTransactionReference: "WIRE-FED-JPMC-998273618",
                bankStatementDigestSha256: "A1B2C3D4E5F60718293A4B5C6D7E8F90123456789ABCDEF0123456789ABCDEF0",
                thirdPartyProofRegistryDigest: "REGISTRY-DIGEST-PROOF-CONFIRMATION-882");

            var reconciledAttestation = await economicsService.ReconcileExternalEvidenceAsync(
                attestation.AttestationId,
                "CorporateAuditorAuthority",
                ExternalReconciliationStatus.Reconciled);

            Assert.Equal(ExternalReconciliationStatus.Reconciled, reconciledAttestation.Status);
            Assert.NotNull(reconciledAttestation.ReconciledAtUtc);

            // 13. Growth Experiment & Autonomous Growth Mission
            var experiment = await experimentService.CreateExperimentAsync(
                hypothesisStatement: "Targeting VP Treasury rather than Finance Directors yields 3x higher contract value.",
                primaryMetricName: "AverageContractValue",
                baselineMetricValue: 50_000m,
                minimumDetectableEffectPercent: 50.0m,
                requiredSampleSize: 40,
                allocatedBudget: 10_000m,
                governanceSignoffId: "PRG1-EXP-SIGNOFF-001");

            await experimentService.RecordExperimentTelemetryAsync(
                experiment.ExperimentId,
                sampleCount: 50,
                controlValue: 52_000m,
                variantValue: 110_000m,
                pValue: 0.008m);

            Assert.True(experiment.IsStatisticallySignificant);

            var mission = await experimentService.CreateGrowthMissionAsync(
                growthObjectiveId: growthObj.ObjectiveId,
                missionName: "Mid-Market FinTech Autonomous Scale",
                targetIcpDescription: "FinTechs $50M-$200M ARR",
                budgetLimit: 75_000m,
                minTargetLtvToCac: 4.5m,
                minTargetMarginPercent: 45.0m);

            await experimentService.ActivateMissionAsync(mission.MissionId, new[] { "AcquisitionAgent", "ClosingAgent", "RetentionAgent" });
            await experimentService.RecordMissionProgressAsync(mission.MissionId, addedSpend: 20_000m, addedRealizedRevenue: 110_000m);

            Assert.Equal(GrowthMissionStatus.Active, mission.Status);
            Assert.Equal(110_000m, mission.CurrentRealizedRevenue);

            // 14. Constitutional Invariant Certification
            Assert.True(GrowthConstitutionalInvariants.ValidateAllAxioms());
            Assert.Equal(3.0m, GrowthConstitutionalInvariants.MinLtvToCacRatio);
            Assert.Equal(12, GrowthConstitutionalInvariants.MaxPaybackPeriodMonths);
            Assert.Equal(35.0m, GrowthConstitutionalInvariants.MinGrossMarginPercent);
        }
    }
}
