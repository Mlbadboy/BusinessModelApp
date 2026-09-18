using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Growth;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Operations;

namespace BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Operations
{
    public interface IContinuousBusinessOperatingKernelStore
    {
        Task SaveCycleAsync(BusinessCycle cycle, CancellationToken cancellationToken = default);
        Task<BusinessCycle?> GetCycleAsync(string cycleId, CancellationToken cancellationToken = default);
        Task<BusinessCycle?> GetActiveCycleForObjectiveAsync(string tenantId, string businessObjectiveId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<BusinessCycle>> ListCyclesAsync(string tenantId, CancellationToken cancellationToken = default);
    }

    public interface IContinuousBusinessOperatingKernelService
    {
        Task<BusinessCycle> StartNewCycleAsync(
            string tenantId,
            string businessObjectiveId,
            string growthObjectiveId,
            TimeSpan? maxDuration = null,
            decimal? maxBudgetINR = null,
            CancellationToken cancellationToken = default);

        Task<BusinessCycle> AdvanceStageAsync(
            string cycleId,
            BusinessCycleStage stage,
            CancellationToken cancellationToken = default);

        Task<BusinessCycle> CheckpointCycleAsync(
            string cycleId,
            string stateDigest,
            CancellationToken cancellationToken = default);

        Task<BusinessCycle> RecoverCycleAfterCrashAsync(
            string cycleId,
            string recoveryNotes,
            CancellationToken cancellationToken = default);

        Task<BusinessCycle> ConcludeCycleAsync(
            string cycleId,
            BusinessCycleOutcome outcome,
            CancellationToken cancellationToken = default);
    }

    public interface IProductionRealityLedgerStore
    {
        Task AppendEventAsync(ProductionRealityEvent evt, CancellationToken cancellationToken = default);
        Task<ProductionRealityEvent?> GetEventAsync(string eventId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ProductionRealityEvent>> ListEventsAsync(string tenantId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ProductionRealityEvent>> ListEventsForObjectAsync(string businessObjectId, CancellationToken cancellationToken = default);
    }

    public interface IProductionRealityLedgerService
    {
        Task<ProductionRealityEvent> RecordExternalEventAsync(
            string tenantId,
            string businessObjectiveId,
            string growthObjectiveId,
            string businessObjectId,
            ProductionRealityEventType eventType,
            EpistemicEvidenceLevel evidenceLevel,
            string source,
            string connectorName,
            string externalProvider,
            string externalReferenceId,
            string rawPayloadForDigest,
            decimal revenueImpact = 0m,
            decimal costImpact = 0m,
            string counterpartyReference = "",
            CancellationToken cancellationToken = default);

        Task<ProductionRealityEvent> ReconcileEventAsync(
            string eventId,
            string authority,
            ExternalReconciliationStatus status,
            string auditNotes = "",
            CancellationToken cancellationToken = default);

        Task<bool> VerifyRealityEvidenceIntegrityAsync(
            string eventId,
            CancellationToken cancellationToken = default);
    }
}
