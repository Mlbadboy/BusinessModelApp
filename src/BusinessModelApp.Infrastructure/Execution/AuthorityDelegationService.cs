using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BusinessModelApp.Core.Domain.Execution;
using BusinessModelApp.Core.Interfaces;
using BusinessModelApp.Infrastructure.Data;

namespace BusinessModelApp.Infrastructure.Execution
{
    public class AuthorityDelegationService : IAuthorityDelegationService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AuthorityDelegationService> _logger;

        public AuthorityDelegationService(AppDbContext context, ILogger<AuthorityDelegationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<AuthorityDelegationEntity> GrantDelegationAsync(
            Guid workspaceId,
            string department,
            string agentId,
            string agentRole,
            List<string> allowedCapabilities,
            decimal dailyLimitINR,
            decimal perTxLimitINR,
            Guid grantedByUserId,
            DateTime? validUntilUtc = null,
            CancellationToken cancellationToken = default)
        {
            var delegation = new AuthorityDelegationEntity
            {
                WorkspaceId = workspaceId,
                Department = department,
                AgentId = agentId,
                AgentRole = agentRole,
                AllowedCapabilitiesJson = JsonSerializer.Serialize(allowedCapabilities),
                DailySpendLimitINR = dailyLimitINR,
                PerTransactionLimitINR = perTxLimitINR,
                GrantedByUserId = grantedByUserId,
                ValidFromUtc = DateTime.UtcNow,
                ValidUntilUtc = validUntilUtc ?? DateTime.UtcNow.AddMonths(1),
                IsRevoked = false,
                CreatedAtUtc = DateTime.UtcNow
            };

            _context.AuthorityDelegations.Add(delegation);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Granted authority delegation {Id} to agent {AgentId} in workspace {WorkspaceId}",
                delegation.Id, agentId, workspaceId);

            return delegation;
        }

        public async Task<bool> RevokeDelegationAsync(
            Guid delegationId,
            Guid revokedByUserId,
            string reason,
            CancellationToken cancellationToken = default)
        {
            var delegation = await _context.AuthorityDelegations.FindAsync(new object[] { delegationId }, cancellationToken);
            if (delegation == null || delegation.IsRevoked)
                return false;

            delegation.IsRevoked = true;
            delegation.RevokedAtUtc = DateTime.UtcNow;
            delegation.RevocationReason = reason;
            delegation.RevokedByUserId = revokedByUserId;

            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogWarning("Revoked authority delegation {Id} by user {UserId}: {Reason}", delegationId, revokedByUserId, reason);
            return true;
        }

        public async Task<bool> ValidateDelegationAsync(
            Guid workspaceId,
            string agentId,
            string capabilityId,
            decimal amountINR,
            CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var delegations = await _context.AuthorityDelegations
                .Where(d => d.WorkspaceId == workspaceId && d.AgentId == agentId && !d.IsRevoked && d.ValidFromUtc <= now && d.ValidUntilUtc >= now)
                .ToListAsync(cancellationToken);

            foreach (var d in delegations)
            {
                if (d.AllowsCapability(capabilityId))
                {
                    // Check per-transaction ceiling
                    if (amountINR <= d.PerTransactionLimitINR)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public async Task<List<AuthorityDelegationEntity>> GetActiveDelegationsAsync(Guid workspaceId, CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            return await _context.AuthorityDelegations
                .Where(d => d.WorkspaceId == workspaceId && !d.IsRevoked && d.ValidFromUtc <= now && d.ValidUntilUtc >= now)
                .OrderByDescending(d => d.CreatedAtUtc)
                .ToListAsync(cancellationToken);
        }
    }
}
