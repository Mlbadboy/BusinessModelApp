using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Workforce;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Workforce;

namespace BusinessModelApp.Infrastructure.Runtime.Enterprise.Workforce
{
    public interface ICommercialAccountAndLineageStore
    {
        Task SaveCrmMutationAsync(string tenantId, GovernedCrmMutation mutation);
        Task<IReadOnlyList<GovernedCrmMutation>> ListCrmMutationsAsync(string tenantId);

        Task SaveAccountGraphAsync(string tenantId, AccountGraph graph);
        Task<AccountGraph?> GetAccountGraphAsync(string tenantId, string accountId);
        Task<IReadOnlyList<AccountGraph>> ListAccountGraphsAsync(string tenantId);

        Task SaveLineageNodeAsync(string tenantId, CommercialLineageNode node);
        Task<IReadOnlyList<CommercialLineageNode>> GetLineageNodesAsync(string tenantId, string opportunityId);

        Task SaveExternalEffectAsync(string tenantId, ImmutableExternalEffect effect);
        Task<ImmutableExternalEffect?> GetExternalEffectAsync(string tenantId, string effectId);
    }

    public class InMemoryCommercialAccountAndLineageStore : ICommercialAccountAndLineageStore
    {
        private readonly ConcurrentDictionary<string, List<GovernedCrmMutation>> _mutationsByTenant = new();
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, AccountGraph>> _graphsByTenant = new();
        private readonly ConcurrentDictionary<string, List<CommercialLineageNode>> _lineageByTenant = new();
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, ImmutableExternalEffect>> _effectsByTenant = new();

        public Task SaveCrmMutationAsync(string tenantId, GovernedCrmMutation mutation)
        {
            var list = _mutationsByTenant.GetOrAdd(tenantId, _ => new List<GovernedCrmMutation>());
            lock (list)
            {
                list.Add(mutation);
            }
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<GovernedCrmMutation>> ListCrmMutationsAsync(string tenantId)
        {
            if (_mutationsByTenant.TryGetValue(tenantId, out var list))
            {
                lock (list)
                {
                    return Task.FromResult<IReadOnlyList<GovernedCrmMutation>>(list.ToList());
                }
            }
            return Task.FromResult<IReadOnlyList<GovernedCrmMutation>>(Array.Empty<GovernedCrmMutation>());
        }

        public Task SaveAccountGraphAsync(string tenantId, AccountGraph graph)
        {
            var map = _graphsByTenant.GetOrAdd(tenantId, _ => new ConcurrentDictionary<string, AccountGraph>());
            map[graph.AccountId] = graph;
            return Task.CompletedTask;
        }

        public Task<AccountGraph?> GetAccountGraphAsync(string tenantId, string accountId)
        {
            if (_graphsByTenant.TryGetValue(tenantId, out var map) && map.TryGetValue(accountId, out var graph))
            {
                return Task.FromResult<AccountGraph?>(graph);
            }
            return Task.FromResult<AccountGraph?>(null);
        }

        public Task<IReadOnlyList<AccountGraph>> ListAccountGraphsAsync(string tenantId)
        {
            if (_graphsByTenant.TryGetValue(tenantId, out var map))
            {
                return Task.FromResult<IReadOnlyList<AccountGraph>>(map.Values.ToList());
            }
            return Task.FromResult<IReadOnlyList<AccountGraph>>(Array.Empty<AccountGraph>());
        }

        public Task SaveLineageNodeAsync(string tenantId, CommercialLineageNode node)
        {
            var list = _lineageByTenant.GetOrAdd(tenantId, _ => new List<CommercialLineageNode>());
            lock (list)
            {
                list.Add(node);
            }
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<CommercialLineageNode>> GetLineageNodesAsync(string tenantId, string opportunityId)
        {
            if (_lineageByTenant.TryGetValue(tenantId, out var list))
            {
                lock (list)
                {
                    return Task.FromResult<IReadOnlyList<CommercialLineageNode>>(list.Where(n => n.OpportunityId == opportunityId).ToList());
                }
            }
            return Task.FromResult<IReadOnlyList<CommercialLineageNode>>(Array.Empty<CommercialLineageNode>());
        }

        public Task SaveExternalEffectAsync(string tenantId, ImmutableExternalEffect effect)
        {
            var map = _effectsByTenant.GetOrAdd(tenantId, _ => new ConcurrentDictionary<string, ImmutableExternalEffect>());
            map[effect.EffectId] = effect;
            return Task.CompletedTask;
        }

        public Task<ImmutableExternalEffect?> GetExternalEffectAsync(string tenantId, string effectId)
        {
            if (_effectsByTenant.TryGetValue(tenantId, out var map) && map.TryGetValue(effectId, out var effect))
            {
                return Task.FromResult<ImmutableExternalEffect?>(effect);
            }
            return Task.FromResult<ImmutableExternalEffect?>(null);
        }
    }

    public class GovernedCrmService : IGovernedCrmService
    {
        private readonly ICommercialAccountAndLineageStore _store;

        public GovernedCrmService(ICommercialAccountAndLineageStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public async Task<bool> SubmitGovernedMutationAsync(string tenantId, GovernedCrmMutation mutation)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentException("TenantId required", nameof(tenantId));
            if (mutation == null) throw new ArgumentNullException(nameof(mutation));

            // Invariant: Direct mutations without Batch 6 authorization are rejected
            if (!mutation.IsBatch6Authorized)
            {
                throw new InvalidOperationException("Governance Violation: CRM mutations must be authorized by Batch 6 Execution Firewall.");
            }

            mutation.TenantId = tenantId;
            mutation.Timestamp = DateTime.UtcNow;

            await _store.SaveCrmMutationAsync(tenantId, mutation);
            return true;
        }

        public Task<IReadOnlyList<GovernedCrmMutation>> ListMutationsAsync(string tenantId)
        {
            return _store.ListCrmMutationsAsync(tenantId);
        }
    }

    public class AccountGraphService : IAccountGraphService
    {
        private readonly ICommercialAccountAndLineageStore _store;

        public AccountGraphService(ICommercialAccountAndLineageStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public async Task<AccountGraph> SaveAccountGraphAsync(string tenantId, AccountGraph graph)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentException("TenantId required", nameof(tenantId));
            if (graph == null) throw new ArgumentNullException(nameof(graph));

            graph.TenantId = tenantId;
            graph.LastAuditedAt = DateTime.UtcNow;

            await _store.SaveAccountGraphAsync(tenantId, graph);
            return graph;
        }

        public Task<AccountGraph?> GetAccountGraphAsync(string tenantId, string accountId)
        {
            return _store.GetAccountGraphAsync(tenantId, accountId);
        }

        public Task<IReadOnlyList<AccountGraph>> ListAccountGraphsAsync(string tenantId)
        {
            return _store.ListAccountGraphsAsync(tenantId);
        }
    }

    public class CommercialLineageService : ICommercialLineageService
    {
        private readonly ICommercialAccountAndLineageStore _store;

        public CommercialLineageService(ICommercialAccountAndLineageStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public async Task<CommercialLineageNode> AppendLineageNodeAsync(string tenantId, CommercialLineageNode node)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentException("TenantId required", nameof(tenantId));
            if (node == null) throw new ArgumentNullException(nameof(node));

            node.TenantId = tenantId;

            // Compute parent hash link
            var existing = await _store.GetLineageNodesAsync(tenantId, node.OpportunityId);
            if (existing.Count > 0)
            {
                node.PreviousBlockHash = existing.Last().CurrentBlockHash;
            }
            else
            {
                node.PreviousBlockHash = "GENESIS";
            }

            node.CurrentBlockHash = node.ComputeHash();
            node.CreatedAt = DateTime.UtcNow;

            await _store.SaveLineageNodeAsync(tenantId, node);
            return node;
        }

        public Task<IReadOnlyList<CommercialLineageNode>> GetLineageChainAsync(string tenantId, string opportunityId)
        {
            return _store.GetLineageNodesAsync(tenantId, opportunityId);
        }

        public async Task<bool> VerifyLineageIntegrityAsync(string tenantId, string opportunityId)
        {
            var nodes = await _store.GetLineageNodesAsync(tenantId, opportunityId);
            if (nodes.Count == 0) return true;

            string expectedPrevious = "GENESIS";
            foreach (var node in nodes)
            {
                if (node.PreviousBlockHash != expectedPrevious)
                {
                    return false; // Broken cryptographic link
                }

                if (node.CurrentBlockHash != node.ComputeHash())
                {
                    return false; // Tampered payload
                }

                expectedPrevious = node.CurrentBlockHash;
            }

            return true;
        }

        public async Task<ImmutableExternalEffect> RecordExternalEffectAsync(string tenantId, ImmutableExternalEffect effect)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentException("TenantId required", nameof(tenantId));
            if (effect == null) throw new ArgumentNullException(nameof(effect));

            effect.ObservedAt = DateTime.UtcNow;
            await _store.SaveExternalEffectAsync(tenantId, effect);
            return effect;
        }

        public Task<ImmutableExternalEffect?> GetExternalEffectAsync(string tenantId, string effectId)
        {
            return _store.GetExternalEffectAsync(tenantId, effectId);
        }
    }
}
