using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Commercial;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Commercial;

namespace BusinessModelApp.Infrastructure.Runtime.Enterprise.Commercial
{
    public class InMemoryAccountIntelligenceStore : IAccountIntelligenceStore
    {
        private readonly ConcurrentDictionary<string, EnterpriseAccountGraph> _accounts = new();

        public Task SaveAccountGraphAsync(EnterpriseAccountGraph graph)
        {
            _accounts[$"{graph.TenantId}:{graph.AccountId}"] = graph;
            return Task.CompletedTask;
        }

        public Task<EnterpriseAccountGraph?> GetAccountGraphAsync(string tenantId, string accountId)
        {
            _accounts.TryGetValue($"{tenantId}:{accountId}", out var graph);
            return Task.FromResult(graph);
        }

        public Task<IReadOnlyList<EnterpriseAccountGraph>> ListAccountGraphsAsync(string tenantId)
        {
            var list = _accounts.Values.Where(a => a.TenantId == tenantId).ToList();
            return Task.FromResult<IReadOnlyList<EnterpriseAccountGraph>>(list);
        }
    }

    public class AccountIntelligenceService : IAccountIntelligenceService
    {
        private readonly IAccountIntelligenceStore _store;

        public AccountIntelligenceService(IAccountIntelligenceStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public async Task<EnterpriseAccountGraph> UpsertAccountGraphAsync(EnterpriseAccountGraph graph)
        {
            if (graph == null) throw new ArgumentNullException(nameof(graph));
            if (string.IsNullOrWhiteSpace(graph.TenantId)) throw new ArgumentException("TenantId is required.");

            graph.LastAuditedAtUtc = DateTime.UtcNow;
            await _store.SaveAccountGraphAsync(graph);
            return graph;
        }

        public async Task<EnterpriseAccountGraph?> GetAccountGraphAsync(string tenantId, string accountId)
        {
            return await _store.GetAccountGraphAsync(tenantId, accountId);
        }

        public async Task<BuyingCenterContact> AddBuyingCenterContactAsync(string tenantId, string accountId, BuyingCenterContact contact)
        {
            if (contact == null) throw new ArgumentNullException(nameof(contact));

            var account = await _store.GetAccountGraphAsync(tenantId, accountId);
            if (account == null)
                throw new InvalidOperationException($"Account {accountId} not found for tenant {tenantId}.");

            // Sub-Batch 4.4.2 Invariant: Unknown people without authoritative evidence remain strictly UNKNOWN
            if (string.IsNullOrWhiteSpace(contact.AuthoritativeEvidenceId) || contact.ConfidenceScore < 0.7m)
            {
                contact.Role = BuyingCenterPersonaRole.UNKNOWN;
            }

            contact.ObservedAtUtc = DateTime.UtcNow;
            account.BuyingCenter.Add(contact);
            account.LastAuditedAtUtc = DateTime.UtcNow;

            await _store.SaveAccountGraphAsync(account);
            return contact;
        }

        public async Task<IReadOnlyList<EnterpriseAccountGraph>> ListAccountsAsync(string tenantId)
        {
            return await _store.ListAccountGraphsAsync(tenantId);
        }
    }
}
