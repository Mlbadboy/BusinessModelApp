using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Production;

namespace BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Production
{
    public interface IProductionEnvironmentService
    {
        Task<ProductionReadinessCheck> RunReadinessCheckAsync(CancellationToken cancellationToken = default);
        Task<SecretBrokerToken> IssueScopedSecretTokenAsync(string tenantId, string connectorId, string capabilityScope, TimeSpan validity, CancellationToken cancellationToken = default);
        Task<bool> ValidateSecretTokenAsync(string tokenId, CancellationToken cancellationToken = default);
        Task<ProductionActivationRecord> ActivateProductionAsync(string tenantId, string humanSignoffId, CancellationToken cancellationToken = default);
    }

    public interface IProductionEvidenceVerifier
    {
        Task<bool> VerifyBankStatementDigestAsync(string bankTransactionRef, string statementDigestSha256, CancellationToken cancellationToken = default);
        Task<bool> VerifyContractDigestAsync(string contractId, string contractDigestSha256, CancellationToken cancellationToken = default);
        Task<ProductionLineageRecord> RecordLineageAsync(ProductionLineageRecord lineage, CancellationToken cancellationToken = default);
        Task<ProductionLineageRecord?> GetLineageAsync(string lineageId, CancellationToken cancellationToken = default);
    }

    public interface IProductionIncidentRecoveryService
    {
        Task<ProductionKillSwitchState> TriggerKillSwitchAsync(string tenantId, string authority, string reason, CancellationToken cancellationToken = default);
        Task<ProductionKillSwitchState> ResetKillSwitchAsync(string tenantId, string authority, CancellationToken cancellationToken = default);
        Task<ProductionKillSwitchState> GetKillSwitchStateAsync(string tenantId, CancellationToken cancellationToken = default);
        Task<ExternalEffectRecord> RecordExternalEffectAsync(ExternalEffectRecord effect, CancellationToken cancellationToken = default);
        Task<ExternalEffectRecord> ReconcileUnknownEffectAsync(string effectId, ExternalEffectState resolvedState, string notes, CancellationToken cancellationToken = default);
    }

    public interface IProductionCertificationService
    {
        Task<ProductionCertificationReport> EvaluateCertificationAsync(string tenantId, string businessObjectiveId, CancellationToken cancellationToken = default);
    }
}
