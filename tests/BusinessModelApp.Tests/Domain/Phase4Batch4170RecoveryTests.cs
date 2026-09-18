using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Recovery;
using BusinessModelApp.Infrastructure.Runtime.Enterprise.Recovery;
using Xunit;

namespace BusinessModelApp.Tests.Domain
{
    /// <summary>
    /// Phase 4.17 Batch 4170: Autonomous Business Recovery Domain Tests.
    /// </summary>
    public class Phase4Batch4170RecoveryTests
    {
        [Fact]
        public async Task UnknownEffectIncident_Reconciliation_PreventsDuplicateExecution()
        {
            var store = new InMemoryAutonomousBusinessRecoveryStore();
            var service = new AutonomousBusinessRecoveryService(store);

            // Report network disconnection during bank transfer payout
            var incident = await service.ReportIncidentAsync(
                "tenant-alpha",
                "BankPayoutConnector",
                "NET_SOCKET_TIMEOUT_MID_PAYMENT",
                IncidentSeverity.Major,
                "TCP socket reset after payload transmission to bank gateway",
                unknownEffectToken: "UE-TOKEN-WIRE-99218");

            Assert.Equal(RecoveryStrategy.ReconcileExternalEffect, incident.RecommendedStrategy);
            Assert.Equal(IncidentStatus.Investigating, incident.Status);

            // Reconcile with external provider: Confirmed payment actually succeeded
            var reconciled = await service.ReconcileUnknownEffectAsync(
                incident.IncidentId,
                externalEffectConfirmedExecuted: true,
                providerConfirmationRef: "BANK-UTR-9918274619");

            Assert.Equal(IncidentStatus.Reconciled, reconciled.Status);
            Assert.Contains("Replay aborted to prevent duplicate transaction", reconciled.ResolutionNotes);

            // Close incident
            var closed = await service.CloseIncidentAsync(incident.IncidentId, "Audit confirmed bank ledger balance matches internal record.");
            Assert.Equal(IncidentStatus.Resolved, closed.Status);
            Assert.NotNull(closed.ResolvedAtUtc);
        }
    }
}
