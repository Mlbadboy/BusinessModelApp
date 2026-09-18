using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.ControlTower;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Growth;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Recovery;
using BusinessModelApp.Infrastructure.Runtime.Enterprise.ControlTower;
using BusinessModelApp.Infrastructure.Runtime.Enterprise.Finance;
using BusinessModelApp.Infrastructure.Runtime.Enterprise.Lifecycle;
using BusinessModelApp.Infrastructure.Runtime.Enterprise.Production;
using BusinessModelApp.Infrastructure.Runtime.Enterprise.Recovery;
using Xunit;

namespace BusinessModelApp.Tests.Domain
{
    /// <summary>
    /// Phase 4.18 Batch 4181: Business Control Tower Red Team Tests (TOWER01 - TOWER03).
    /// </summary>
    public class Phase4Batch4181ControlTowerRedTeamTests
    {
        [Fact]
        public async Task TOWER01_KillSwitchEngagement_ImmediatelyReflectsCriticalHealth()
        {
            var incidentRecoveryService = new ProductionIncidentRecoveryService();
            await incidentRecoveryService.TriggerKillSwitchAsync("tenant-alpha", "PRG-1", "Emergency Red Team Drill");

            var service = new BusinessControlTowerService(incidentRecoveryService: incidentRecoveryService);
            var dashboard = await service.GetExecutiveDashboardAsync("tenant-alpha");

            Assert.True(dashboard.KillSwitchEngaged);
            Assert.Equal(GrowthHealthGrade.Critical, dashboard.OverallHealthGrade);
        }

        [Fact]
        public async Task TOWER02_MultipleOpenIncidents_ProjectsCriticalGrade()
        {
            var recoveryStore = new InMemoryAutonomousBusinessRecoveryStore();
            var recoveryService = new AutonomousBusinessRecoveryService(recoveryStore);

            await recoveryService.ReportIncidentAsync("tenant-alpha", "C1", "ERR1", IncidentSeverity.Major, "R1");
            await recoveryService.ReportIncidentAsync("tenant-alpha", "C2", "ERR2", IncidentSeverity.Major, "R2");
            await recoveryService.ReportIncidentAsync("tenant-alpha", "C3", "ERR3", IncidentSeverity.Major, "R3");
            await recoveryService.ReportIncidentAsync("tenant-alpha", "C4", "ERR4", IncidentSeverity.Major, "R4");

            var service = new BusinessControlTowerService(recoveryStore: recoveryStore);
            var dashboard = await service.GetExecutiveDashboardAsync("tenant-alpha");

            Assert.Equal(4, dashboard.OpenIncidentsCount);
            Assert.Equal(GrowthHealthGrade.Critical, dashboard.OverallHealthGrade);
        }

        [Fact]
        public async Task TOWER03_EmptyTenant_DoesNotFabricateMetrics()
        {
            var service = new BusinessControlTowerService();
            var dashboard = await service.GetExecutiveDashboardAsync("tenant-empty");

            Assert.Equal(0m, dashboard.TotalRealizedRevenueINR);
            Assert.Equal(0m, dashboard.TotalBankCashBalanceINR);
            Assert.Equal(0, dashboard.TotalActiveCustomers);
            Assert.Equal(0, dashboard.OpenIncidentsCount);
        }
    }
}
