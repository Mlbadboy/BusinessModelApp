using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Growth;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Operations;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Production;

namespace BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Production
{
    public interface IProductionActivationKernelStore
    {
        Task SaveTenantActivationAsync(ProductionTenantActivation activation, CancellationToken cancellationToken = default);
        Task<ProductionTenantActivation?> GetTenantActivationAsync(string tenantId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ProductionTenantActivation>> ListTenantActivationsAsync(CancellationToken cancellationToken = default);

        Task SaveConnectorRegistrationAsync(ProductionConnectorRegistration registration, CancellationToken cancellationToken = default);
        Task<ProductionConnectorRegistration?> GetConnectorRegistrationAsync(string registrationId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ProductionConnectorRegistration>> ListConnectorsForTenantAsync(string tenantId, CancellationToken cancellationToken = default);

        Task SaveConfigVersionAsync(ProductionConfigVersion version, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ProductionConfigVersion>> ListConfigVersionsForTenantAsync(string tenantId, CancellationToken cancellationToken = default);
    }

    public interface IProductionActivationKernelService
    {
        Task<ProductionTenantActivation> RequestTenantActivationAsync(string tenantId, string businessObjectiveId, string legalBusinessName, CancellationToken cancellationToken = default);
        Task<ProductionTenantActivation> AuthorizeTenantActivationAsync(string tenantId, string humanSignoffId, CancellationToken cancellationToken = default);
        Task<ProductionConnectorRegistration> RegisterConnectorAsync(ProductionConnectorRegistration registration, CancellationToken cancellationToken = default);
        Task<ProductionConfigVersion> DeployConfigVersionAsync(string tenantId, string configPayload, string approvedBySignoffId, CancellationToken cancellationToken = default);
        Task<ProductionTenantActivation> RollbackTenantConfigAsync(string tenantId, int targetVersionNumber, string humanSignoffId, CancellationToken cancellationToken = default);
        Task<ProductionEvidencePromotionResult> PromoteEvidenceAsync(ProductionRealityEvent rawEvent, string auditorAuthority, CancellationToken cancellationToken = default);
    }

    public sealed class ProductionEvidencePromotionResult
    {
        public bool IsPromoted { get; init; }
        public EpistemicEvidenceLevel ResultingLevel { get; init; }
        public string PromotionRationale { get; init; } = string.Empty;
        public string ReconciledDigestSha256 { get; init; } = string.Empty;
    }
}
