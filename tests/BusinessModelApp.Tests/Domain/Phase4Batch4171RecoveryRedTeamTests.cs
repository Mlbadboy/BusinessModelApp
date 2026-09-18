using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Recovery;
using BusinessModelApp.Infrastructure.Runtime.Enterprise.Recovery;
using Xunit;

namespace BusinessModelApp.Tests.Domain
{
    /// <summary>
    /// Phase 4.17 Batch 4171: Autonomous Recovery Red Team Tests (REC01 - REC04).
    /// </summary>
    public class Phase4Batch4171RecoveryRedTeamTests
    {
        [Fact]
        public async Task REC01_ReconcileWithoutUnknownEffectToken_FailsClosed()
        {
            var store = new InMemoryAutonomousBusinessRecoveryStore();
            var service = new AutonomousBusinessRecoveryService(store);

            var incident = await service.ReportIncidentAsync(
                "tenant-alpha",
                "AgentWorker",
                "ERR_GENERIC",
                IncidentSeverity.Minor,
                "Worker crashed",
                unknownEffectToken: ""); // Empty token

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.ReconcileUnknownEffectAsync(incident.IncidentId, true, "REF123"));

            Assert.Contains("does not contain an UnknownEffect token", ex.Message);
        }

        [Fact]
        public async Task REC02_NonExistentIncident_FailsClosed()
        {
            var store = new InMemoryAutonomousBusinessRecoveryStore();
            var service = new AutonomousBusinessRecoveryService(store);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.ExecuteCompensationAsync("invalid-incident-id", "Rollback DB"));
        }

        [Fact]
        public async Task REC03_MajorSeverityWithoutUnknownEffect_DefaultsToCompensateAndRollback()
        {
            var store = new InMemoryAutonomousBusinessRecoveryStore();
            var service = new AutonomousBusinessRecoveryService(store);

            var incident = await service.ReportIncidentAsync(
                "tenant-alpha",
                "DatabaseMigrationKernel",
                "ERR_SCHEMA_CORRUPTION",
                IncidentSeverity.Critical,
                "Schema validation failed during live migration");

            Assert.Equal(RecoveryStrategy.CompensateAndRollback, incident.RecommendedStrategy);

            var comp = await service.ExecuteCompensationAsync(incident.IncidentId, "Restored PostgreSQL point-in-time snapshot.");
            Assert.Equal(IncidentStatus.Compensated, comp.Status);
        }

        [Fact]
        public async Task REC04_MinorSeverity_DefaultsToRetryWithBackoff()
        {
            var store = new InMemoryAutonomousBusinessRecoveryStore();
            var service = new AutonomousBusinessRecoveryService(store);

            var incident = await service.ReportIncidentAsync(
                "tenant-alpha",
                "TelemetryScraper",
                "ERR_HTTP_503",
                IncidentSeverity.Minor,
                "Transient 503 from analytics gateway");

            Assert.Equal(RecoveryStrategy.RetryWithBackoff, incident.RecommendedStrategy);
        }
    }
}
