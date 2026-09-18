using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Workforce;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Workforce;

namespace BusinessModelApp.Infrastructure.Runtime.Enterprise.Workforce
{
    public interface ICommunicationAndSecretStore
    {
        Task SaveIntentAsync(string tenantId, CommunicationIntent intent);
        Task<CommunicationIntent?> GetIntentAsync(string tenantId, string intentId);
        Task<IReadOnlyList<CommunicationIntent>> ListIntentsAsync(string tenantId);

        Task SaveCredentialAsync(string tenantId, ScopedCredential credential);
        Task<ScopedCredential?> GetCredentialAsync(string tenantId, string credentialId);
    }

    public class InMemoryCommunicationAndSecretStore : ICommunicationAndSecretStore
    {
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, CommunicationIntent>> _intentsByTenant = new();
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, ScopedCredential>> _credentialsByTenant = new();

        public Task SaveIntentAsync(string tenantId, CommunicationIntent intent)
        {
            var map = _intentsByTenant.GetOrAdd(tenantId, _ => new ConcurrentDictionary<string, CommunicationIntent>());
            map[intent.IntentId] = intent;
            return Task.CompletedTask;
        }

        public Task<CommunicationIntent?> GetIntentAsync(string tenantId, string intentId)
        {
            if (_intentsByTenant.TryGetValue(tenantId, out var map) && map.TryGetValue(intentId, out var intent))
            {
                return Task.FromResult<CommunicationIntent?>(intent);
            }
            return Task.FromResult<CommunicationIntent?>(null);
        }

        public Task<IReadOnlyList<CommunicationIntent>> ListIntentsAsync(string tenantId)
        {
            if (_intentsByTenant.TryGetValue(tenantId, out var map))
            {
                return Task.FromResult<IReadOnlyList<CommunicationIntent>>(map.Values.ToList());
            }
            return Task.FromResult<IReadOnlyList<CommunicationIntent>>(Array.Empty<CommunicationIntent>());
        }

        public Task SaveCredentialAsync(string tenantId, ScopedCredential credential)
        {
            var map = _credentialsByTenant.GetOrAdd(tenantId, _ => new ConcurrentDictionary<string, ScopedCredential>());
            map[credential.CredentialId] = credential;
            return Task.CompletedTask;
        }

        public Task<ScopedCredential?> GetCredentialAsync(string tenantId, string credentialId)
        {
            if (_credentialsByTenant.TryGetValue(tenantId, out var map) && map.TryGetValue(credentialId, out var cred))
            {
                return Task.FromResult<ScopedCredential?>(cred);
            }
            return Task.FromResult<ScopedCredential?>(null);
        }
    }

    public class UnifiedCommunicationFabric : IUnifiedCommunicationFabric
    {
        private readonly ICommunicationAndSecretStore _store;

        public UnifiedCommunicationFabric(ICommunicationAndSecretStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public async Task<CommunicationIntent> SubmitCommunicationIntentAsync(string tenantId, CommunicationIntent intent)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentException("TenantId required", nameof(tenantId));
            if (intent == null) throw new ArgumentNullException(nameof(intent));

            intent.TenantId = tenantId;
            intent.CreatedAt = DateTime.UtcNow;
            intent.IsDispatched = false;

            // Low risk (R1) auto-approves; R2/R3 require human review/PRG-1
            if (intent.RiskTier <= 1)
            {
                intent.IsApproved = true;
                intent.ApprovedBy = "SystemAutoApproval";
            }
            else
            {
                intent.IsApproved = false;
            }

            await _store.SaveIntentAsync(tenantId, intent);
            return intent;
        }

        public async Task<bool> ApproveCommunicationIntentAsync(string tenantId, string intentId, string approvedBy)
        {
            var intent = await _store.GetIntentAsync(tenantId, intentId);
            if (intent == null) return false;

            intent.IsApproved = true;
            intent.ApprovedBy = approvedBy;
            await _store.SaveIntentAsync(tenantId, intent);
            return true;
        }

        public async Task<bool> DispatchCommunicationAsync(string tenantId, string intentId)
        {
            var intent = await _store.GetIntentAsync(tenantId, intentId);
            if (intent == null) return false;

            if (!intent.IsApproved)
            {
                throw new InvalidOperationException("Governance Violation: High-consequence communication cannot be dispatched without PRG-1 human approval.");
            }

            intent.IsDispatched = true;
            await _store.SaveIntentAsync(tenantId, intent);
            return true;
        }

        public async Task<IReadOnlyList<CommunicationIntent>> ListPendingApprovalsAsync(string tenantId)
        {
            var all = await _store.ListIntentsAsync(tenantId);
            return all.Where(i => !i.IsApproved && !i.IsDispatched).ToList();
        }
    }

    public class WorkforceSecretBroker : IWorkforceSecretBroker
    {
        private readonly ICommunicationAndSecretStore _store;

        public WorkforceSecretBroker(ICommunicationAndSecretStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public async Task<ScopedCredential> IssueScopedCredentialAsync(string tenantId, string capabilityId, int durationMinutes = 60)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentException("TenantId required", nameof(tenantId));
            if (string.IsNullOrWhiteSpace(capabilityId)) throw new ArgumentException("CapabilityId required", nameof(capabilityId));

            var cred = new ScopedCredential
            {
                TenantId = tenantId,
                CapabilityId = capabilityId,
                MaskedToken = $"sec_{Guid.NewGuid():N}_{capabilityId.ToLower()}",
                IssuedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(durationMinutes),
                IsRevoked = false
            };

            await _store.SaveCredentialAsync(tenantId, cred);
            return cred;
        }

        public async Task<bool> ValidateCredentialAsync(string tenantId, string credentialId, string capabilityId)
        {
            var cred = await _store.GetCredentialAsync(tenantId, credentialId);
            if (cred == null) return false;

            if (cred.TenantId != tenantId) return false;
            if (cred.CapabilityId != capabilityId) return false;
            if (!cred.IsValid) return false;

            return true;
        }

        public async Task<bool> RevokeCredentialAsync(string tenantId, string credentialId)
        {
            var cred = await _store.GetCredentialAsync(tenantId, credentialId);
            if (cred == null) return false;

            cred.IsRevoked = true;
            await _store.SaveCredentialAsync(tenantId, cred);
            return true;
        }
    }
}
