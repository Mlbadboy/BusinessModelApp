using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.ExternalReality;
using BusinessModelApp.Core.Domain.Runtime;
using BusinessModelApp.Core.Domain.Runtime.Fleet;
using BusinessModelApp.Core.Domain.Runtime.Reputation;
using BusinessModelApp.Core.Interfaces.Runtime.Reputation;

namespace BusinessModelApp.Infrastructure.Runtime.Reputation
{
    public class InMemoryReputationStore : IReputationStore
    {
        // Key: $"{workspaceId}:{agentId}:{capabilityId}:{domainKey}:{regime}"
        private readonly ConcurrentDictionary<string, CapabilityPerformanceProfile> _profiles = new(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<Guid, List<CapabilityPerformanceProfile>> _profileHistory = new();

        // Key: $"{workspaceId}:{agentId}"
        private readonly ConcurrentDictionary<string, AgentPerformanceProfile> _agentProfiles = new(StringComparer.Ordinal);

        // Key: $"{capabilityId}"
        private readonly ConcurrentDictionary<string, CapabilityVersionProfile> _capabilityProfiles = new(StringComparer.Ordinal);

        private readonly ConcurrentDictionary<Guid, ReputationEvidenceToken> _evidenceTokens = new();
        private readonly ConcurrentDictionary<Guid, RoutingDecisionRecord> _routingDecisions = new();

        public Task SaveProfileAsync(CapabilityPerformanceProfile profile, CancellationToken ct = default)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));

            var key = BuildProfileKey(profile.WorkspaceId, profile.AgentDefinitionId, profile.CapabilityId, profile.DomainContext.ToCanonicalKey(), profile.MarketRegime);
            _profiles[key] = profile;

            _profileHistory.AddOrUpdate(
                profile.ProfileId,
                new List<CapabilityPerformanceProfile> { profile },
                (_, list) =>
                {
                    lock (list)
                    {
                        list.Add(profile);
                    }
                    return list;
                });

            return Task.CompletedTask;
        }

        public Task<CapabilityPerformanceProfile?> GetProfileAsync(
            Guid workspaceId,
            AgentDefinitionId agentId,
            CapabilityId capabilityId,
            string canonicalDomainKey,
            MarketRegimeState regime,
            CancellationToken ct = default)
        {
            var key = BuildProfileKey(workspaceId, agentId, capabilityId, canonicalDomainKey, regime);
            _profiles.TryGetValue(key, out var profile);
            return Task.FromResult(profile);
        }

        public Task<IReadOnlyList<CapabilityPerformanceProfile>> GetProfileHistoryAsync(Guid profileId, CancellationToken ct = default)
        {
            if (_profileHistory.TryGetValue(profileId, out var history))
            {
                lock (history)
                {
                    return Task.FromResult<IReadOnlyList<CapabilityPerformanceProfile>>(history.ToList());
                }
            }
            return Task.FromResult<IReadOnlyList<CapabilityPerformanceProfile>>(Array.Empty<CapabilityPerformanceProfile>());
        }

        public Task SaveAgentProfileAsync(AgentPerformanceProfile profile, CancellationToken ct = default)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            var key = $"{profile.WorkspaceId}:{profile.AgentDefinitionId.Value}";
            _agentProfiles[key] = profile;
            return Task.CompletedTask;
        }

        public Task<AgentPerformanceProfile?> GetAgentProfileAsync(Guid workspaceId, AgentDefinitionId agentId, CancellationToken ct = default)
        {
            var key = $"{workspaceId}:{agentId.Value}";
            _agentProfiles.TryGetValue(key, out var profile);
            return Task.FromResult(profile);
        }

        public Task SaveCapabilityProfileAsync(CapabilityVersionProfile profile, CancellationToken ct = default)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            var key = profile.CapabilityId.ToString();
            _capabilityProfiles[key] = profile;
            return Task.CompletedTask;
        }

        public Task<CapabilityVersionProfile?> GetCapabilityProfileAsync(CapabilityId capabilityId, CancellationToken ct = default)
        {
            var key = capabilityId.ToString();
            _capabilityProfiles.TryGetValue(key, out var profile);
            return Task.FromResult(profile);
        }

        public Task SaveEvidenceTokenAsync(ReputationEvidenceToken token, CancellationToken ct = default)
        {
            if (token == null) throw new ArgumentNullException(nameof(token));
            _evidenceTokens[token.TokenId] = token;
            if (token.WorkerId.Value != Guid.Empty)
            {
                _workerAgentBindings[token.WorkerId.Value] = token.AgentDefinitionId;
            }
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ReputationEvidenceToken>> GetEvidenceTokensForAttemptAsync(ExecutionAttemptId attemptId, CancellationToken ct = default)
        {
            var matched = _evidenceTokens.Values.Where(t => t.AttemptId == attemptId).ToList();
            return Task.FromResult<IReadOnlyList<ReputationEvidenceToken>>(matched);
        }

        public Task SaveRoutingDecisionAsync(RoutingDecisionRecord decision, CancellationToken ct = default)
        {
            if (decision == null) throw new ArgumentNullException(nameof(decision));
            _routingDecisions[decision.DecisionId] = decision;
            return Task.CompletedTask;
        }

        public Task<RoutingDecisionRecord?> GetRoutingDecisionAsync(Guid decisionId, CancellationToken ct = default)
        {
            _routingDecisions.TryGetValue(decisionId, out var decision);
            return Task.FromResult(decision);
        }

        public Task BindWorkerAgentAsync(WorkerProcessId workerId, AgentDefinitionId agentId, CancellationToken ct = default)
        {
            _workerAgentBindings[workerId.Value] = agentId;
            return Task.CompletedTask;
        }

        public Task<AgentDefinitionId?> GetAgentForWorkerAsync(WorkerProcessId workerId, CancellationToken ct = default)
        {
            _workerAgentBindings.TryGetValue(workerId.Value, out var agentId);
            return Task.FromResult<AgentDefinitionId?>(agentId == default ? null : agentId);
        }

        public Task QuarantineCapabilityProfilesAsync(Guid workspaceId, AgentDefinitionId agentId, CapabilityId capabilityId, string reason, CancellationToken ct = default)
        {
            foreach (var kvp in _profiles)
            {
                var profile = kvp.Value;
                if (profile.WorkspaceId == workspaceId &&
                    profile.AgentDefinitionId == agentId &&
                    profile.CapabilityId == capabilityId)
                {
                    profile.Metrics = profile.Metrics with
                    {
                        IsQuarantined = true,
                        QuarantineReason = reason,
                        QuarantinedAt = DateTimeOffset.UtcNow
                    };
                    _profileHistory.AddOrUpdate(
                        profile.ProfileId,
                        new List<CapabilityPerformanceProfile> { profile },
                        (_, list) =>
                        {
                            lock (list)
                            {
                                list.Add(profile);
                            }
                            return list;
                        });
                }
            }
            return Task.CompletedTask;
        }

        private readonly ConcurrentDictionary<Guid, AgentDefinitionId> _workerAgentBindings = new();

        private static string BuildProfileKey(Guid workspaceId, AgentDefinitionId agentId, CapabilityId capabilityId, string canonicalDomainKey, MarketRegimeState regime) =>
            $"{workspaceId}:{agentId.Value}:{capabilityId.ToString().ToLowerInvariant()}:{canonicalDomainKey.ToLowerInvariant()}:{regime}";
    }
}
