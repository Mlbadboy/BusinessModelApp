using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Commercial;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Commercial;

namespace BusinessModelApp.Infrastructure.Runtime.Enterprise.Commercial
{
    public class InMemoryOpportunityDiscoveryStore : IOpportunityDiscoveryStore
    {
        private readonly ConcurrentDictionary<string, MarketSignalItem> _signals = new();
        private readonly ConcurrentDictionary<string, ICPProfile> _profiles = new();
        private readonly ConcurrentDictionary<string, GroundedOpportunity> _opportunities = new();

        public Task SaveSignalAsync(MarketSignalItem signal)
        {
            _signals[$"{signal.TenantId}:{signal.SignalId}"] = signal;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<MarketSignalItem>> ListSignalsAsync(string tenantId)
        {
            var list = _signals.Values.Where(s => s.TenantId == tenantId).ToList();
            return Task.FromResult<IReadOnlyList<MarketSignalItem>>(list);
        }

        public Task SaveIcpProfileAsync(ICPProfile profile)
        {
            _profiles[$"{profile.TenantId}:{profile.ProfileId}"] = profile;
            return Task.CompletedTask;
        }

        public Task<ICPProfile?> GetIcpProfileAsync(string tenantId, string profileId)
        {
            _profiles.TryGetValue($"{tenantId}:{profileId}", out var profile);
            return Task.FromResult(profile);
        }

        public Task<IReadOnlyList<ICPProfile>> ListIcpProfilesAsync(string tenantId)
        {
            var list = _profiles.Values.Where(p => p.TenantId == tenantId).ToList();
            return Task.FromResult<IReadOnlyList<ICPProfile>>(list);
        }

        public Task SaveOpportunityAsync(GroundedOpportunity opportunity)
        {
            _opportunities[$"{opportunity.TenantId}:{opportunity.OpportunityId}"] = opportunity;
            return Task.CompletedTask;
        }

        public Task<GroundedOpportunity?> GetOpportunityAsync(string tenantId, string opportunityId)
        {
            _opportunities.TryGetValue($"{tenantId}:{opportunityId}", out var opp);
            return Task.FromResult(opp);
        }

        public Task<IReadOnlyList<GroundedOpportunity>> ListOpportunitiesAsync(string tenantId)
        {
            var list = _opportunities.Values.Where(o => o.TenantId == tenantId).ToList();
            return Task.FromResult<IReadOnlyList<GroundedOpportunity>>(list);
        }
    }

    public class OpportunityDiscoveryService : IOpportunityDiscoveryService
    {
        private readonly IOpportunityDiscoveryStore _store;

        public OpportunityDiscoveryService(IOpportunityDiscoveryStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public async Task<MarketSignalItem> IngestMarketSignalAsync(MarketSignalItem signal)
        {
            if (signal == null) throw new ArgumentNullException(nameof(signal));
            if (string.IsNullOrWhiteSpace(signal.TenantId)) throw new ArgumentException("TenantId is required.");

            signal.DetectedAtUtc = DateTime.UtcNow;
            await _store.SaveSignalAsync(signal);
            return signal;
        }

        public async Task<ICPProfile> CreateIcpProfileAsync(ICPProfile profile)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (string.IsNullOrWhiteSpace(profile.TenantId)) throw new ArgumentException("TenantId is required.");

            await _store.SaveIcpProfileAsync(profile);
            return profile;
        }

        public async Task<GroundedOpportunity> EvaluateSignalAgainstIcpAsync(string tenantId, string signalId, string profileId)
        {
            var signals = await _store.ListSignalsAsync(tenantId);
            var signal = signals.FirstOrDefault(s => s.SignalId == signalId);
            if (signal == null) throw new InvalidOperationException($"Market signal {signalId} not found.");

            var profile = await _store.GetIcpProfileAsync(tenantId, profileId);
            if (profile == null) throw new InvalidOperationException($"ICP profile {profileId} not found.");

            // Evaluate Match Criteria
            var industryMatch = profile.TargetIndustries.Count == 0 || profile.TargetIndustries.Any(i => string.Equals(i, signal.Industry, StringComparison.OrdinalIgnoreCase));
            var geographyMatch = profile.TargetGeographies.Count == 0 || profile.TargetGeographies.Any(g => string.Equals(g, signal.Geography, StringComparison.OrdinalIgnoreCase));

            var matchedKeywords = profile.RequiredKeywords.Count(k => signal.RawContent.Contains(k, StringComparison.OrdinalIgnoreCase));
            var hasNegativeKeywords = profile.NegativeKeywords.Any(k => signal.RawContent.Contains(k, StringComparison.OrdinalIgnoreCase));

            decimal icpScore = 0m;
            if (industryMatch && geographyMatch && !hasNegativeKeywords)
            {
                var keywordRatio = profile.RequiredKeywords.Count > 0 ? (decimal)matchedKeywords / profile.RequiredKeywords.Count : 1.0m;
                icpScore = Math.Round(0.5m + (0.5m * keywordRatio), 2);
            }

            var opp = new GroundedOpportunity
            {
                TenantId = tenantId,
                AccountId = $"acc-{signal.Domain.Replace(".", "-")}",
                CompanyName = signal.CompanyName,
                CorroboratedEvidenceIds = new List<string> { signal.SignalId },
                IdentifiedProblem = $"Operational inefficiency and automation gap detected from source: {signal.Source}",
                QuantifiedBusinessImpactINR = profile.MinTargetRevenueINR * 1.5m,
                ICPScore = icpScore,
                CommercialFitScore = Math.Round(icpScore * signal.FreshnessScore, 2),
                EstimatedDealValueINR = profile.MinTargetRevenueINR,
                RiskScore = hasNegativeKeywords ? 0.8m : 0.2m,
                ConfidenceScore = Math.Round(signal.FreshnessScore * (industryMatch ? 0.9m : 0.4m), 2),
                RecommendedNextAction = "Initiate account research and identify decision makers in buying center",
                EvaluationExplanation = $"Evaluated against profile '{profile.ProfileName}': IndustryMatch={industryMatch}, GeoMatch={geographyMatch}, Freshness={signal.FreshnessScore:F2}",
                DiscoveredAtUtc = DateTime.UtcNow
            };

            await _store.SaveOpportunityAsync(opp);
            return opp;
        }

        public async Task<IReadOnlyList<GroundedOpportunity>> GetGroundedOpportunitiesAsync(string tenantId)
        {
            var list = await _store.ListOpportunitiesAsync(tenantId);
            return list.Where(o => o.IsGrounded && o.ICPScore >= 0.5m).ToList();
        }
    }
}
