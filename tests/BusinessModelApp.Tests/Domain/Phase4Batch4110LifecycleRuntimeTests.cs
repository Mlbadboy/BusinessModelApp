using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Lifecycle;
using BusinessModelApp.Infrastructure.Runtime.Enterprise.Lifecycle;
using Xunit;

namespace BusinessModelApp.Tests.Domain
{
    /// <summary>
    /// Phase 4.11 Batch 4110: Customer Lifecycle, Retention & Expansion Domain Tests.
    /// </summary>
    public class Phase4Batch4110LifecycleRuntimeTests
    {
        private static string Sha256(string input)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        [Fact]
        public async Task ChurnRiskDetection_TriggersIntervention_AndUpdatesHealth()
        {
            var store = new InMemoryCustomerLifecycleRuntimeStore();
            var service = new CustomerLifecycleRuntimeService(store);

            var account = await service.RegisterAccountAsync("tenant-saas", "Fintech Innovations Ltd", 1_200_000m);
            Assert.Equal(CustomerAccountStatus.Onboarding, account.Status);
            Assert.Equal(1_200_000m, account.ContractedARR_INR);

            // Assess high churn risk (75% probability due to champion departure)
            var assessment = await service.AssessChurnRiskAsync(
                "tenant-saas",
                account.CustomerId,
                0.75m,
                "ChampionDeparture",
                "Primary executive sponsor moved to another company; API usage dropped 40%.",
                new List<string> { "Executive Outreach by CEO", "Offer 30-day workflow optimization support" });

            Assert.True(assessment.InterventionTriggered);
            Assert.Equal(CustomerAccountStatus.AtRisk, account.Status);
            Assert.Equal(0.25m, account.HealthScore);
        }

        [Fact]
        public async Task ExpansionOpportunity_WonWithAttestedContract_ExpandsARRAndCalculatesNRR()
        {
            var store = new InMemoryCustomerLifecycleRuntimeStore();
            var service = new CustomerLifecycleRuntimeService(store);

            var account = await service.RegisterAccountAsync("tenant-saas", "Global Logistics Corp", 2_000_000m);

            // Identify expansion opportunity
            var expansion = await service.IdentifyExpansionAsync(
                "tenant-saas",
                account.CustomerId,
                "AutonomousMultiTenantCustomOrchestration",
                800_000m,
                0.90m);

            Assert.Equal(ExpansionStatus.Identified, expansion.Status);

            // Close Won with SHA-256 counterparty contract
            var contractHash = Sha256("signed-expansion-contract-tier-2-enterprise");
            var wonExp = await service.CloseWonExpansionAsync(expansion.ExpansionId, contractHash);

            Assert.Equal(ExpansionStatus.Won, wonExp.Status);
            Assert.Equal(contractHash, wonExp.SignedContractRefSha256);
            Assert.Equal(2_800_000m, account.ContractedARR_INR);
            Assert.Equal(CustomerAccountStatus.Expanded, account.Status);

            // Calculate NRR: Starting 2.0M, Expansion 0.8M, Contraction 0, Churn 0 -> 140% NRR
            var nrrCalc = await service.CalculateRetentionMetricsAsync("tenant-saas", 2_000_000m, 0m, 0m);
            Assert.Equal(2_800_000m, nrrCalc.EndingARR_INR);
            Assert.Equal(140m, nrrCalc.NetRevenueRetentionPct);
            Assert.Equal(100m, nrrCalc.GrossRevenueRetentionPct);
        }
    }
}
