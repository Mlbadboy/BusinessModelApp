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
    /// Phase 4.15 Batch 4151: Autonomous Strategy Red Team Tests (STRAT01 - STRAT04).
    /// </summary>
    public class Phase4Batch4151StrategyRedTeamTests
    {
        private static string Sha256(string input)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        [Fact]
        public async Task STRAT01_LaunchExecutionWithoutPRG1Approval_FailsClosed()
        {
            var store = new InMemoryAutonomousBusinessStrategyStore();
            var service = new AutonomousBusinessStrategyService(store);

            var obj = await service.CreateStrategicObjectiveAsync("tenant-alpha", "Objective A", StrategicTimeHorizon.CurrentQuarter, 10_000_000m, 2_000_000m);
            var init = await service.ProposeInitiativeAsync("tenant-alpha", obj.ObjectiveId, "Unapproved Init", "Desc", 80m, 500_000m);

            // Attempt launch directly while in Proposed status
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.LaunchInitiativeExecutionAsync(init.InitiativeId));

            Assert.Contains("PRG-1 human strategic approval", ex.Message);
        }

        [Fact]
        public async Task STRAT02_ApprovePRG1WithInvalidHash_FailsClosed()
        {
            var store = new InMemoryAutonomousBusinessStrategyStore();
            var service = new AutonomousBusinessStrategyService(store);

            var obj = await service.CreateStrategicObjectiveAsync("tenant-alpha", "Objective A", StrategicTimeHorizon.CurrentQuarter, 10_000_000m, 2_000_000m);
            var init = await service.ProposeInitiativeAsync("tenant-alpha", obj.ObjectiveId, "Initiative", "Desc", 80m, 500_000m);

            var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
                service.ApproveInitiativeWithPRG1Async(init.InitiativeId, "invalid-short-hash"));

            Assert.Contains("valid 64-character SHA-256", ex.Message);
        }

        [Fact]
        public async Task STRAT03_ExceedingObjectiveCapital_FailsClosed()
        {
            var store = new InMemoryAutonomousBusinessStrategyStore();
            var service = new AutonomousBusinessStrategyService(store);

            // Objective capital = 1,000,000 INR
            var obj = await service.CreateStrategicObjectiveAsync("tenant-alpha", "Objective Budget Bound", StrategicTimeHorizon.CurrentQuarter, 5_000_000m, 1_000_000m);

            // Request 1,500,000 INR (> 1,000,000)
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.ProposeInitiativeAsync("tenant-alpha", obj.ObjectiveId, "Excessive Capital Init", "Desc", 90m, 1_500_000m));

            Assert.Contains("exceeds available objective budget", ex.Message);
        }

        [Fact]
        public async Task STRAT04_NegativeCapital_ThrowsOutOfRange()
        {
            var store = new InMemoryAutonomousBusinessStrategyStore();
            var service = new AutonomousBusinessStrategyService(store);

            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
                service.CreateStrategicObjectiveAsync("tenant-alpha", "Obj", StrategicTimeHorizon.Annual, 1_000_000m, -500_000m));
        }
    }
}
