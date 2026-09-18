using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Organizational.Collaboration;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational.Collaboration
{
    public class TeamFormationEngine : ITeamFormationEngine
    {
        private readonly ITeamCharterStore _store;
        private readonly TeamFormationPolicy _systemPolicy;

        public TeamFormationEngine(
            ITeamCharterStore store,
            TeamFormationPolicy? systemPolicy = null)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _systemPolicy = systemPolicy ?? new TeamFormationPolicy();
        }

        public async Task<(bool Valid, string? Reason)> ValidateTeamCompositionAsync(
            string tenantId,
            IReadOnlyList<TeamMemberRole> roles,
            TeamFormationPolicy policy,
            CancellationToken ct = default)
        {
            if (roles == null || roles.Count == 0)
            {
                return (false, "Team must have at least one member role.");
            }

            // Invariant I29-G: Max team size <= 5
            if (roles.Count > policy.MaxTeamSize)
            {
                return (false, $"Team size {roles.Count} exceeds maximum allowed ceiling of {policy.MaxTeamSize} per Invariant I29-G.");
            }

            // Check for duplicate agent instances
            var distinctAgents = roles.Select(r => r.AgentInstanceId).Distinct().Count();
            if (distinctAgents != roles.Count)
            {
                return (false, "Duplicate agent instances in team role allocation are prohibited.");
            }

            // Invariant I29-B: Authority Pooling check - roles cannot pool to elevate risk tier
            foreach (var r in roles)
            {
                if (r.MaxRiskTier > 3)
                {
                    return (false, $"Role '{r.RoleName}' specifies risk tier {r.MaxRiskTier} exceeding autonomous collaboration ceiling (Tier 3) per Invariant I29-B.");
                }

                // Invariant I29-M: Role specialization validation
                if (string.IsNullOrWhiteSpace(r.SpecializationDomain))
                {
                    return (false, $"Role '{r.RoleName}' must have an explicit SpecializationDomain per Invariant I29-M.");
                }
            }

            var activeCharters = await _store.ListActiveChartersAsync(tenantId, ct);
            var activeCount = activeCharters.Count(c => c.ExpiresUtc > DateTime.UtcNow);
            if (activeCount >= policy.MaxActiveTeamsPerTenant)
            {
                return (false, $"Tenant has reached the maximum active teams ceiling ({policy.MaxActiveTeamsPerTenant}) per policy.");
            }

            return (true, null);
        }

        public async Task<(bool Success, TeamCharter? Charter, string? ErrorReason)> FormTeamAsync(
            string tenantId,
            string workId,
            string objective,
            IReadOnlyList<TeamMemberRole> candidateRoles,
            decimal budget = 0m,
            int governanceTier = 1,
            TeamFormationPolicy? policyOverride = null,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) return (false, null, "TenantId is required.");
            if (string.IsNullOrWhiteSpace(workId)) return (false, null, "WorkId is required.");
            if (string.IsNullOrWhiteSpace(objective)) return (false, null, "Objective is required.");

            var effectivePolicy = TeamFormationPolicy.ResolveEffective(_systemPolicy, policyOverride);

            var (valid, reason) = await ValidateTeamCompositionAsync(tenantId, candidateRoles, effectivePolicy, ct);
            if (!valid)
            {
                return (false, null, reason);
            }

            // Invariant I29-B: Team governance tier cannot be lower than the maximum member risk tier
            int maxRoleRiskTier = candidateRoles.Max(r => r.MaxRiskTier);
            int effectiveGovTier = Math.Max(governanceTier, maxRoleRiskTier);

            var charter = new TeamCharter
            {
                CharterId = $"CHT-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
                TenantId = tenantId,
                WorkId = workId,
                Objective = objective,
                FormedUtc = DateTime.UtcNow,
                ExpiresUtc = DateTime.UtcNow.Add(effectivePolicy.DefaultCharterTtl),
                Status = TeamLifecycleStatus.Operating,
                Members = candidateRoles.ToList(),
                ResourceBudget = budget,
                GovernanceTier = effectiveGovTier
            };

            charter.ComputeCharterHash();
            await _store.SaveCharterAsync(charter, ct);

            return (true, charter, null);
        }
    }
}
