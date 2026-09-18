using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Strategy;
using BusinessModelApp.Infrastructure.Runtime.Enterprise.Strategy;
using Xunit;

namespace BusinessModelApp.Tests.Domain
{
    /// <summary>
    /// Phase 4.15 Batch 4150: Autonomous Business Strategy Domain Tests.
    /// </summary>
    public class Phase4Batch4150StrategyTests
    {
        private static string Sha256(string input)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        [Fact]
        public async Task StrategicInitiative_Propose_ApprovePRG1_AndLaunchExecution()
        {
            var store = new InMemoryAutonomousBusinessStrategyStore();
            var service = new AutonomousBusinessStrategyService(store);

            // 1. Create annual strategic objective
            var objective = await service.CreateStrategicObjectiveAsync(
                "tenant-alpha",
                "Expand Enterprise FinTech Autonomous Reconciliation Footprint in APAC",
                StrategicTimeHorizon.Annual,
                targetRevenueINR: 50_000_000m,
                allocatedCapitalINR: 10_000_000m,
                maxRiskToleranceScore: 0.25m);

            Assert.Equal("tenant-alpha", objective.TenantId);
            Assert.Equal(50_000_000m, objective.TargetRevenueINR);

            // 2. Propose initiative
            var initiative = await service.ProposeInitiativeAsync(
                "tenant-alpha",
                objective.ObjectiveId,
                "Automated SWIFT/Fedwire Autonomous Connector Module",
                "Integrate direct bank API gateways for real-time L5 cash reconciliation.",
                strategicValueScore: 92m,
                requiredCapitalINR: 2_500_000m);

            Assert.Equal(StrategicInitiativeStatus.Proposed, initiative.Status);

            // 3. Human PRG-1 Strategic Signoff
            var prg1Hash = Sha256("prg1-ceo-signoff-strategic-initiative-swift-fedwire");
            var approved = await service.ApproveInitiativeWithPRG1Async(initiative.InitiativeId, prg1Hash);

            Assert.Equal(StrategicInitiativeStatus.ApprovedPRG1, approved.Status);
            Assert.Equal(prg1Hash, approved.PRG1SignoffSha256);
            Assert.NotNull(approved.ApprovedAtUtc);

            // 4. Launch Execution
            var launched = await service.LaunchInitiativeExecutionAsync(initiative.InitiativeId);
            Assert.Equal(StrategicInitiativeStatus.InExecution, launched.Status);
        }
    }
}
