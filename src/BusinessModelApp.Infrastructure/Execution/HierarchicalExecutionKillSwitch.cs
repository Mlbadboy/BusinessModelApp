using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BusinessModelApp.Core.Domain.Execution;
using BusinessModelApp.Core.Interfaces;
using BusinessModelApp.Infrastructure.Data;

namespace BusinessModelApp.Infrastructure.Execution
{
    public class HierarchicalExecutionKillSwitch : IExecutionKillSwitchService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<HierarchicalExecutionKillSwitch> _logger;

        // Ultra-fast thread-safe cache for sub-100ms halt across 100+ workers
        private static readonly ConcurrentDictionary<string, bool> FastHaltCache = new();
        private static volatile bool PlatformKillActive = false;

        public HierarchicalExecutionKillSwitch(AppDbContext context, ILogger<HierarchicalExecutionKillSwitch> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> IsHaltedAsync(
            Guid workspaceId,
            Guid? missionId = null,
            string? agentId = null,
            string? capabilityId = null,
            CancellationToken cancellationToken = default)
        {
            // 1. Platform-wide kill check
            if (PlatformKillActive) return true;

            // 2. Fast cache checks
            if (FastHaltCache.TryGetValue($"TENANT:{workspaceId}", out var tenantHalt) && tenantHalt) return true;
            if (missionId.HasValue && FastHaltCache.TryGetValue($"MISSION:{missionId}", out var missionHalt) && missionHalt) return true;
            if (!string.IsNullOrWhiteSpace(agentId) && FastHaltCache.TryGetValue($"AGENT:{agentId}", out var agentHalt) && agentHalt) return true;
            if (!string.IsNullOrWhiteSpace(capabilityId) && FastHaltCache.TryGetValue($"CAPABILITY:{capabilityId}", out var capHalt) && capHalt) return true;

            // 3. Database backing check
            var activeSwitches = await _context.ExecutionKillSwitches
                .AsNoTracking()
                .Where(s => s.IsActive && (s.WorkspaceId == null || s.WorkspaceId == workspaceId))
                .ToListAsync(cancellationToken);

            foreach (var s in activeSwitches)
            {
                switch (s.Tier)
                {
                    case ExecutionKillSwitchTier.Platform:
                        PlatformKillActive = true;
                        return true;

                    case ExecutionKillSwitchTier.Tenant:
                        if (s.WorkspaceId == workspaceId)
                        {
                            FastHaltCache[$"TENANT:{workspaceId}"] = true;
                            return true;
                        }
                        break;

                    case ExecutionKillSwitchTier.Mission:
                        if (missionId.HasValue && string.Equals(s.TargetIdentifier, missionId.ToString(), StringComparison.OrdinalIgnoreCase))
                        {
                            FastHaltCache[$"MISSION:{missionId}"] = true;
                            return true;
                        }
                        break;

                    case ExecutionKillSwitchTier.Agent:
                        if (!string.IsNullOrWhiteSpace(agentId) && string.Equals(s.TargetIdentifier, agentId, StringComparison.OrdinalIgnoreCase))
                        {
                            FastHaltCache[$"AGENT:{agentId}"] = true;
                            return true;
                        }
                        break;

                    case ExecutionKillSwitchTier.Capability:
                        if (!string.IsNullOrWhiteSpace(capabilityId) && string.Equals(s.TargetIdentifier, capabilityId, StringComparison.OrdinalIgnoreCase))
                        {
                            FastHaltCache[$"CAPABILITY:{capabilityId}"] = true;
                            return true;
                        }
                        break;
                }
            }

            return false;
        }

        public async Task<ExecutionKillSwitchEntity> TriggerKillSwitchAsync(
            ExecutionKillSwitchTier tier,
            Guid? workspaceId,
            string? targetIdentifier,
            string reason,
            Guid triggeredByUserId,
            CancellationToken cancellationToken = default)
        {
            var entity = new ExecutionKillSwitchEntity
            {
                Tier = tier,
                WorkspaceId = workspaceId,
                TargetIdentifier = targetIdentifier,
                Reason = reason,
                TriggeredByUserId = triggeredByUserId,
                TriggeredAtUtc = DateTime.UtcNow,
                IsActive = true
            };

            _context.ExecutionKillSwitches.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);

            // Immediately set fast cache for instantaneous halt
            switch (tier)
            {
                case ExecutionKillSwitchTier.Platform:
                    PlatformKillActive = true;
                    break;
                case ExecutionKillSwitchTier.Tenant:
                    if (workspaceId.HasValue) FastHaltCache[$"TENANT:{workspaceId.Value}"] = true;
                    break;
                case ExecutionKillSwitchTier.Mission:
                    if (!string.IsNullOrWhiteSpace(targetIdentifier)) FastHaltCache[$"MISSION:{targetIdentifier}"] = true;
                    break;
                case ExecutionKillSwitchTier.Agent:
                    if (!string.IsNullOrWhiteSpace(targetIdentifier)) FastHaltCache[$"AGENT:{targetIdentifier}"] = true;
                    break;
                case ExecutionKillSwitchTier.Capability:
                    if (!string.IsNullOrWhiteSpace(targetIdentifier)) FastHaltCache[$"CAPABILITY:{targetIdentifier}"] = true;
                    break;
            }

            _logger.LogCritical("EMERGENCY EXECUTION KILL SWITCH ACTIVATED: Tier={Tier}, Workspace={WorkspaceId}, Target={Target}, Reason={Reason}",
                tier, workspaceId, targetIdentifier, reason);

            return entity;
        }

        public async Task<bool> DeactivateKillSwitchAsync(Guid switchId, Guid deactivatedByUserId, CancellationToken cancellationToken = default)
        {
            var entity = await _context.ExecutionKillSwitches.FindAsync(new object[] { switchId }, cancellationToken);
            if (entity == null || !entity.IsActive) return false;

            entity.IsActive = false;
            entity.DeactivatedAtUtc = DateTime.UtcNow;
            entity.DeactivatedByUserId = deactivatedByUserId;

            await _context.SaveChangesAsync(cancellationToken);

            // Clear cache
            switch (entity.Tier)
            {
                case ExecutionKillSwitchTier.Platform:
                    PlatformKillActive = false;
                    break;
                case ExecutionKillSwitchTier.Tenant:
                    if (entity.WorkspaceId.HasValue) FastHaltCache.TryRemove($"TENANT:{entity.WorkspaceId.Value}", out _);
                    break;
                case ExecutionKillSwitchTier.Mission:
                    if (!string.IsNullOrWhiteSpace(entity.TargetIdentifier)) FastHaltCache.TryRemove($"MISSION:{entity.TargetIdentifier}", out _);
                    break;
                case ExecutionKillSwitchTier.Agent:
                    if (!string.IsNullOrWhiteSpace(entity.TargetIdentifier)) FastHaltCache.TryRemove($"AGENT:{entity.TargetIdentifier}", out _);
                    break;
                case ExecutionKillSwitchTier.Capability:
                    if (!string.IsNullOrWhiteSpace(entity.TargetIdentifier)) FastHaltCache.TryRemove($"CAPABILITY:{entity.TargetIdentifier}", out _);
                    break;
            }

            _logger.LogInformation("Deactivated Kill Switch {Id} by user {UserId}", switchId, deactivatedByUserId);
            return true;
        }

        public async Task<List<ExecutionKillSwitchEntity>> GetActiveKillSwitchesAsync(Guid? workspaceId = null, CancellationToken cancellationToken = default)
        {
            return await _context.ExecutionKillSwitches
                .Where(s => s.IsActive && (s.WorkspaceId == null || workspaceId == null || s.WorkspaceId == workspaceId))
                .OrderByDescending(s => s.TriggeredAtUtc)
                .ToListAsync(cancellationToken);
        }
    }
}
