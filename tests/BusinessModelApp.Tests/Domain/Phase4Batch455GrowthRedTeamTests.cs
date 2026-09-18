using System;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Growth;
using BusinessModelApp.Infrastructure.Runtime.Enterprise.Growth;
using Xunit;

namespace BusinessModelApp.Tests.Domain
{
    /// <summary>
    /// Phase 4.5 Sub-Batch 4.5.20: Commercial Growth Red Team Attack Defenses (GROW01 - GROW20).
    /// Enforces non-negotiable Law I41: Business Growth Sovereignty.
    /// </summary>
    public class Phase4Batch455GrowthRedTeamTests
    {
        // GROW01: Unprofitable scale attack (negative contribution margin must not be treated as value)
        [Fact]
        public async Task GROW01_UnprofitableScaleAttack_CorrectlyFlaggedAsNegativeContribution()
        {
            var store = new InMemoryUnitEconomicsAndTreasuryStore();
            var service = new UnitEconomicsAndTreasuryService(store);

            var pnl = await service.RecordCustomerUnitEconomicsAsync(
                "cust-grow01",
                "contract-grow01",
                realizedCashRevenue: 50_000m,
                directDeliveryCosts: 40_000m,
                agentComputeAndTokenCosts: 15_000m,
                softwareLicenseCosts: 5_000m);

            // Loaded variable costs: 60k, Revenue: 50k -> -$10k contribution
            Assert.True(pnl.NetContributionMargin < 0, "GROW01 Defense: System must detect negative contribution margin.");
            Assert.Equal(-10_000m, pnl.NetContributionMargin);
        }

        // GROW02: Vanity pipeline inflation (prospect with invalid ICP score must be rejected)
        [Fact]
        public void GROW02_VanityPipelineInflation_InvalidIcpScoreThrows()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new AccountProspect("Fake Corp", "Tech", 100, 1000000m, icpScore: 1.5m, "Out of bounds score");
            });

            Assert.Throws<ArgumentException>(() =>
            {
                new AccountProspect("Fake Corp", "Tech", 100, 1000000m, icpScore: -0.2m, "Negative score");
            });
        }

        // GROW03: Spam blast attempt (outbound outreach without anti-spam verification must be blocked)
        [Fact]
        public async Task GROW03_SpamBlastAttempt_WithoutAntiSpamVerificationBlocked()
        {
            var store = new InMemoryCustomerAcquisitionAndRetentionStore();
            var service = new CustomerAcquisitionAndRetentionService(store);
            var prospect = await service.DiscoverAndScoreProspectAsync("Target Corp", "SaaS", 200, 10_000_000m, 0.8m, "Good");

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await service.SendGovernedOutreachAsync(
                    prospect.ProspectId,
                    "target@corp.com",
                    "Email",
                    "Spam",
                    "Body",
                    antiSpamComplianceVerified: false,
                    governedSignoffDigest: "DIGEST");
            });
        }

        // GROW04: Unauthorized outbound dispatch (missing governed signoff digest must be blocked)
        [Fact]
        public async Task GROW04_UnauthorizedOutboundDispatch_MissingSignoffDigestBlocked()
        {
            var store = new InMemoryCustomerAcquisitionAndRetentionStore();
            var service = new CustomerAcquisitionAndRetentionService(store);
            var prospect = await service.DiscoverAndScoreProspectAsync("Target Corp", "SaaS", 200, 10_000_000m, 0.8m, "Good");

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await service.SendGovernedOutreachAsync(
                    prospect.ProspectId,
                    "target@corp.com",
                    "Email",
                    "Subject",
                    "Body",
                    antiSpamComplianceVerified: true,
                    governedSignoffDigest: "");
            });
        }

        // GROW05: Margin degradation below 35% on proposal creation must be rejected (Law I41-G)
        [Fact]
        public async Task GROW05_MarginDegradationProposal_Below35PercentRejected()
        {
            var store = new InMemoryCustomerAcquisitionAndRetentionStore();
            var service = new CustomerAcquisitionAndRetentionService(store);
            var prospect = await service.DiscoverAndScoreProspectAsync("Target Corp", "SaaS", 200, 10_000_000m, 0.8m, "Good");

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await service.GenerateProposalAsync(
                    prospect.ProspectId,
                    "Cutthroat Solution",
                    "Discounted below margin floor",
                    proposedPrice: 100_000m,
                    estimatedCostBasis: 75_000m, // 25% margin
                    projectedCustomerRoiMultiple: 3m,
                    estimatedPaybackMonths: 6);
            });
        }

        // GROW06: Margin degradation below 35% on negotiation closing must be rejected
        [Fact]
        public async Task GROW06_MarginDegradationNegotiation_Below35PercentRejected()
        {
            var store = new InMemoryCustomerAcquisitionAndRetentionStore();
            var service = new CustomerAcquisitionAndRetentionService(store);
            var prospect = await service.DiscoverAndScoreProspectAsync("Target Corp", "SaaS", 200, 10_000_000m, 0.8m, "Good");
            var proposal = await service.GenerateProposalAsync(prospect.ProspectId, "Sol", "Scope", 100_000m, 50_000m, 3m, 6);

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await service.FinalizeNegotiationAsync(
                    proposal.ProposalId,
                    100_000m,
                    finalAgreedPrice: 65_000m, // 65k - 50k = 15k/65k = 23% margin
                    estimatedCostBasis: 50_000m,
                    concessionsGrantedSummary: "Steep concession",
                    concessionsReceivedSummary: "None",
                    prg1SignoffId: "PRG1_SIGNOFF_VALID",
                    isClosedWon: true);
            });
        }

        // GROW07: Negotiation closing bypass without PRG-1 signoff must be blocked (Law I41-H)
        [Fact]
        public async Task GROW07_NegotiationClosingBypass_WithoutPrg1SignoffBlocked()
        {
            var store = new InMemoryCustomerAcquisitionAndRetentionStore();
            var service = new CustomerAcquisitionAndRetentionService(store);
            var prospect = await service.DiscoverAndScoreProspectAsync("Target Corp", "SaaS", 200, 10_000_000m, 0.8m, "Good");
            var proposal = await service.GenerateProposalAsync(prospect.ProspectId, "Sol", "Scope", 100_000m, 50_000m, 3m, 6);

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await service.FinalizeNegotiationAsync(
                    proposal.ProposalId,
                    100_000m,
                    90_000m,
                    50_000m,
                    "Terms",
                    "Terms",
                    prg1SignoffId: "", // Missing PRG-1 signoff
                    isClosedWon: true);
            });
        }

        // GROW08: Contract execution without cryptographic digest must be blocked
        [Fact]
        public async Task GROW08_ContractExecutionWithoutDigest_Blocked()
        {
            var store = new InMemoryCustomerAcquisitionAndRetentionStore();
            var service = new CustomerAcquisitionAndRetentionService(store);

            await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await service.ExecuteContractAsync(
                    "prospect-1",
                    "proposal-1",
                    100_000m,
                    "Net 30",
                    contractDigestSha256: "", // Missing digest
                    "Signer",
                    "Charlie");
            });
        }

        // GROW09: Floating growth objective without parent BusinessObjectiveId must be blocked (Law I41-Y)
        [Fact]
        public async Task GROW09_FloatingGrowthObjectiveWithoutParent_ThrowsArgumentException()
        {
            var store = new InMemoryGrowthControlPlaneStore();
            var service = new GrowthControlPlaneService(store);

            await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await service.CreateGrowthObjectiveAsync(new GrowthObjective
                {
                    TenantId = "tenant-grow",
                    BusinessObjectiveId = "", // Detached floating objective
                    Title = "Rogue Growth Objective",
                    TargetGrossMarginPercent = 40m,
                    TargetLtvCacRatio = 4.0m,
                    MaxPaybackPeriodMonths = 6
                });
            });
        }

        // GROW10: Unsustainable LTV:CAC target (< 3.0x) must be blocked (Law I41-N)
        [Fact]
        public async Task GROW10_UnsustainableLtvToCacTarget_ThrowsInvalidOperationException()
        {
            var store = new InMemoryGrowthControlPlaneStore();
            var service = new GrowthControlPlaneService(store);

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await service.CreateGrowthObjectiveAsync(new GrowthObjective
                {
                    TenantId = "tenant-grow",
                    BusinessObjectiveId = "BO-001",
                    Title = "Low LTV:CAC Objective",
                    TargetGrossMarginPercent = 40m,
                    TargetLtvCacRatio = 2.5m, // Violates 3.0x minimum
                    MaxPaybackPeriodMonths = 6
                });
            });
        }

        // GROW11: Unsustainable payback target (> 12 months) must be blocked (Law I41-N)
        [Fact]
        public async Task GROW11_UnsustainablePaybackTarget_ThrowsInvalidOperationException()
        {
            var store = new InMemoryGrowthControlPlaneStore();
            var service = new GrowthControlPlaneService(store);

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await service.CreateGrowthObjectiveAsync(new GrowthObjective
                {
                    TenantId = "tenant-grow",
                    BusinessObjectiveId = "BO-001",
                    Title = "Long Payback Objective",
                    TargetGrossMarginPercent = 40m,
                    TargetLtvCacRatio = 3.5m,
                    MaxPaybackPeriodMonths = 18 // Violates 12 month maximum
                });
            });
        }

        // GROW12: Fabricated external bank attestation (missing reference or digest) must be rejected
        [Fact]
        public async Task GROW12_FabricatedExternalBankAttestation_ThrowsArgumentException()
        {
            var store = new InMemoryUnitEconomicsAndTreasuryStore();
            var service = new UnitEconomicsAndTreasuryService(store);

            await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await service.AttestExternalRevenueEvidenceAsync(
                    invoiceId: "INV-1",
                    counterpartyId: "CP-1",
                    attestedAmount: 50_000m,
                    currency: "USD",
                    bankTransactionReference: "", // Missing bank reference
                    bankStatementDigestSha256: "SHA256_HASH",
                    thirdPartyProofRegistryDigest: "PROOF_DIGEST");
            });

            await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await service.AttestExternalRevenueEvidenceAsync(
                    invoiceId: "INV-1",
                    counterpartyId: "CP-1",
                    attestedAmount: 50_000m,
                    currency: "USD",
                    bankTransactionReference: "BANK_REF_123",
                    bankStatementDigestSha256: "", // Missing digest
                    thirdPartyProofRegistryDigest: "PROOF_DIGEST");
            });
        }

        // GROW13: Discrepant external bank statement detection
        [Fact]
        public async Task GROW13_DiscrepantExternalBankStatement_MarkedAsDiscrepant()
        {
            var store = new InMemoryUnitEconomicsAndTreasuryStore();
            var service = new UnitEconomicsAndTreasuryService(store);

            var attestation = await service.AttestExternalRevenueEvidenceAsync(
                "INV-DISCREPANT",
                "CP-DISCREPANT",
                50_000m,
                "USD",
                "BANK-REF-DISCREPANT",
                "DIGEST-SHA256",
                "PROOF-DIGEST");

            var reconciled = await service.ReconcileExternalEvidenceAsync(
                attestation.AttestationId,
                "Auditor",
                ExternalReconciliationStatus.Discrepant);

            Assert.Equal(ExternalReconciliationStatus.Discrepant, reconciled.Status);
        }

        // GROW14: Treasury depletion via unchecked autonomous campaign spend triggers guardrail breach
        [Fact]
        public async Task GROW14_TreasuryDepletionOverrun_TriggersGuardrailBreach()
        {
            var store = new InMemoryGrowthExperimentStore();
            var service = new GrowthExperimentService(store);

            var mission = await service.CreateGrowthMissionAsync(
                "GRO-OBJ-01",
                "High Risk Mission",
                "Mid-Market",
                budgetLimit: 5_000m);

            await service.ActivateMissionAsync(mission.MissionId, new[] { "AdAgent" });
            await service.RecordMissionProgressAsync(mission.MissionId, addedSpend: 5_500m, addedRealizedRevenue: 100m);

            Assert.Equal(GrowthMissionStatus.PausedGuardrailBreach, mission.Status);
            Assert.Contains("exceeded approved budget limit", mission.GuardrailBreachReason);
        }

        // GROW15: Churn risk monitoring detects critical health deterioration
        [Fact]
        public async Task GROW15_ChurnRiskDetection_IdentifiesCriticalRisk()
        {
            var store = new InMemoryCustomerAcquisitionAndRetentionStore();
            var service = new CustomerAcquisitionAndRetentionService(store);

            var retention = await service.InitiateOnboardingAsync("cust-risk", "contract-risk", "Risk Corp");
            await service.UpdateCustomerHealthAsync("cust-risk", healthScore: 25.0m, ChurnRiskLevel.Critical, npsScore: -60m);

            Assert.Equal(ChurnRiskLevel.Critical, retention.ChurnRisk);
            Assert.Equal(25.0m, retention.HealthScore);
            Assert.Equal(-60m, retention.LatestNpsScore);
        }

        // GROW16: Growth experiment execution without governance signoff must be blocked (Law I41-J)
        [Fact]
        public async Task GROW16_GrowthExperimentWithoutSignoff_Blocked()
        {
            var store = new InMemoryGrowthExperimentStore();
            var service = new GrowthExperimentService(store);

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await service.CreateExperimentAsync(
                    "Ungoverned experiment",
                    "Metric",
                    0.05m,
                    20m,
                    100,
                    2000m,
                    governanceSignoffId: ""); // Missing
            });
        }

        // GROW17: Premature experiment conclusion without required sample size
        [Fact]
        public async Task GROW17_PrematureExperimentConclusion_ReportsNotStatisticallySignificant()
        {
            var store = new InMemoryGrowthExperimentStore();
            var service = new GrowthExperimentService(store);

            var exp = await service.CreateExperimentAsync("Hyp", "Metric", 0.1m, 20m, requiredSampleSize: 500, allocatedBudget: 2000m, "SIGNOFF");
            // Only 50 samples recorded
            await service.RecordExperimentTelemetryAsync(exp.ExperimentId, sampleCount: 50, controlValue: 0.1m, variantValue: 0.15m, pValue: 0.03m);

            Assert.False(exp.IsStatisticallySignificant, "GROW17 Defense: Cannot be statistically significant with sample size < required.");
        }

        // GROW18: Replaying or invalid contract value must be rejected
        [Fact]
        public void GROW18_InvalidContractValue_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new CommercialContractRecord("p-1", "prop-1", totalContractValue: -5000m, "Net 30", "DIGEST", "Customer", "Charlie", true);
            });

            Assert.Throws<ArgumentException>(() =>
            {
                new CommercialContractRecord("p-1", "prop-1", totalContractValue: 0m, "Net 30", "DIGEST", "Customer", "Charlie", true);
            });
        }

        // GROW19: Negative realized cash revenue reporting must be rejected
        [Fact]
        public async Task GROW19_NegativeRevenueReporting_ThrowsArgumentException()
        {
            var store = new InMemoryUnitEconomicsAndTreasuryStore();
            var service = new UnitEconomicsAndTreasuryService(store);

            await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await service.RecordCustomerUnitEconomicsAsync("c-1", "ct-1", realizedCashRevenue: -10_000m, 1000m, 1000m, 1000m);
            });
        }

        // GROW20: Counterparty spoofing during external proof reconciliation (cannot reconcile with PendingVerification)
        [Fact]
        public async Task GROW20_ReconciliationInvalidStatus_ThrowsArgumentException()
        {
            var store = new InMemoryUnitEconomicsAndTreasuryStore();
            var service = new UnitEconomicsAndTreasuryService(store);

            var attestation = await service.AttestExternalRevenueEvidenceAsync("INV", "CP", 1000m, "USD", "REF", "SHA", "PROOF");

            await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await service.ReconcileExternalEvidenceAsync(attestation.AttestationId, "Auditor", ExternalReconciliationStatus.PendingVerification);
            });
        }
    }
}
