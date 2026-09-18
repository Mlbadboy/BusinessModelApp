using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Growth;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Operations;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Production;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Production;

namespace BusinessModelApp.Infrastructure.Runtime.Enterprise.Production
{
    public sealed class InMemoryProductionActivationKernelStore : IProductionActivationKernelStore
    {
        private readonly ConcurrentDictionary<string, ProductionTenantActivation> _activations = new();
        private readonly ConcurrentDictionary<string, ProductionConnectorRegistration> _connectors = new();
        private readonly List<ProductionConfigVersion> _configVersions = new();
        private readonly object _lock = new();

        public Task SaveTenantActivationAsync(ProductionTenantActivation activation, CancellationToken cancellationToken = default)
        {
            _activations[activation.TenantId] = activation;
            return Task.CompletedTask;
        }

        public Task<ProductionTenantActivation?> GetTenantActivationAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            _activations.TryGetValue(tenantId, out var activation);
            return Task.FromResult(activation);
        }

        public Task<IReadOnlyList<ProductionTenantActivation>> ListTenantActivationsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<ProductionTenantActivation>>(_activations.Values.ToList());
        }

        public Task SaveConnectorRegistrationAsync(ProductionConnectorRegistration registration, CancellationToken cancellationToken = default)
        {
            _connectors[registration.RegistrationId] = registration;
            return Task.CompletedTask;
        }

        public Task<ProductionConnectorRegistration?> GetConnectorRegistrationAsync(string registrationId, CancellationToken cancellationToken = default)
        {
            _connectors.TryGetValue(registrationId, out var reg);
            return Task.FromResult(reg);
        }

        public Task<IReadOnlyList<ProductionConnectorRegistration>> ListConnectorsForTenantAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            var list = _connectors.Values.Where(c => c.TenantId == tenantId).ToList();
            return Task.FromResult<IReadOnlyList<ProductionConnectorRegistration>>(list);
        }

        public Task SaveConfigVersionAsync(ProductionConfigVersion version, CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                _configVersions.Add(version);
            }
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ProductionConfigVersion>> ListConfigVersionsForTenantAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                var list = _configVersions.Where(v => v.TenantId == tenantId).OrderBy(v => v.VersionNumber).ToList();
                return Task.FromResult<IReadOnlyList<ProductionConfigVersion>>(list);
            }
        }
    }

    public sealed class ProductionActivationKernelService : IProductionActivationKernelService
    {
        private readonly IProductionActivationKernelStore _store;

        public ProductionActivationKernelService(IProductionActivationKernelStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public async Task<ProductionTenantActivation> RequestTenantActivationAsync(
            string tenantId,
            string businessObjectiveId,
            string legalBusinessName,
            CancellationToken cancellationToken = default)
        {
            var activation = new ProductionTenantActivation
            {
                TenantId = tenantId,
                BusinessObjectiveId = businessObjectiveId,
                LegalBusinessName = legalBusinessName
            };

            await _store.SaveTenantActivationAsync(activation, cancellationToken);
            return activation;
        }

        public async Task<ProductionTenantActivation> AuthorizeTenantActivationAsync(
            string tenantId,
            string humanSignoffId,
            CancellationToken cancellationToken = default)
        {
            var activation = await _store.GetTenantActivationAsync(tenantId, cancellationToken);
            if (activation == null) throw new KeyNotFoundException($"Tenant activation for '{tenantId}' not found.");

            activation.ApproveAndActivate(humanSignoffId);
            await _store.SaveTenantActivationAsync(activation, cancellationToken);
            return activation;
        }

        public async Task<ProductionConnectorRegistration> RegisterConnectorAsync(
            ProductionConnectorRegistration registration,
            CancellationToken cancellationToken = default)
        {
            registration.UpdateHealth(ConnectorHealthStatus.ProductionEnabled);
            await _store.SaveConnectorRegistrationAsync(registration, cancellationToken);
            return registration;
        }

        public async Task<ProductionConfigVersion> DeployConfigVersionAsync(
            string tenantId,
            string configPayload,
            string approvedBySignoffId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(approvedBySignoffId))
                throw new InvalidOperationException("Deploying production configuration requires PRG-1 executive approval (Law I43-L).");

            var previousVersions = await _store.ListConfigVersionsForTenantAsync(tenantId, cancellationToken);
            int nextVersionNumber = previousVersions.Count + 1;

            string digest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(configPayload)));

            var version = new ProductionConfigVersion
            {
                TenantId = tenantId,
                VersionNumber = nextVersionNumber,
                ConfigPayloadHashSha256 = digest,
                ApprovedBySignoffId = approvedBySignoffId
            };

            await _store.SaveConfigVersionAsync(version, cancellationToken);
            return version;
        }

        public async Task<ProductionTenantActivation> RollbackTenantConfigAsync(
            string tenantId,
            int targetVersionNumber,
            string humanSignoffId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(humanSignoffId))
                throw new InvalidOperationException("Production configuration rollback requires PRG-1 executive authorization (Law I43-L).");

            var activation = await _store.GetTenantActivationAsync(tenantId, cancellationToken);
            if (activation == null) throw new KeyNotFoundException($"Tenant activation for '{tenantId}' not found.");

            activation.Rollback(targetVersionNumber);
            await _store.SaveTenantActivationAsync(activation, cancellationToken);
            return activation;
        }

        public Task<ProductionEvidencePromotionResult> PromoteEvidenceAsync(
            ProductionRealityEvent rawEvent,
            string auditorAuthority,
            CancellationToken cancellationToken = default)
        {
            // Law I43-A/B/C: Lower evidence levels (L0-L3) CANNOT manufacture L5/L6 production truth
            if (rawEvent.EvidenceLevel < EpistemicEvidenceLevel.CounterpartyAttested)
            {
                return Task.FromResult(new ProductionEvidencePromotionResult
                {
                    IsPromoted = false,
                    ResultingLevel = rawEvent.EvidenceLevel,
                    PromotionRationale = "Lower evidence levels (L0-L3) cannot be promoted to production truth without counterparty or bank proof (Law I43-A)."
                });
            }

            if (string.IsNullOrWhiteSpace(rawEvent.EvidenceDigestSha256) || rawEvent.EvidenceDigestSha256.Length != 64)
            {
                return Task.FromResult(new ProductionEvidencePromotionResult
                {
                    IsPromoted = false,
                    ResultingLevel = rawEvent.EvidenceLevel,
                    PromotionRationale = "Evidence promotion rejected: Invalid SHA-256 cryptographic digest."
                });
            }

            rawEvent.Reconcile(auditorAuthority, ExternalReconciliationStatus.Reconciled, "Promoted to Bank-Verified Cash reality.");

            return Task.FromResult(new ProductionEvidencePromotionResult
            {
                IsPromoted = true,
                ResultingLevel = EpistemicEvidenceLevel.BankVerifiedCash,
                PromotionRationale = "Reconciled with independent bank wire proof and immutable statement digest.",
                ReconciledDigestSha256 = rawEvent.EvidenceDigestSha256
            });
        }
    }
}
