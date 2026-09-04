using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BusinessModelApp.Core.Domain.Security;
using BusinessModelApp.Infrastructure.Data;

namespace BusinessModelApp.Infrastructure.Security
{
    /// <summary>
    /// Governed Registry enforcing the Hard Target Allowlist.
    /// Invariant: Every Red Team probe must resolve:
    /// Target -> TargetType -> Environment -> Tenant -> SandboxId -> AllowedCapability.
    /// If not explicitly registered in allowlist: DENY fail-closed.
    /// Zero third-party or external production probing.
    /// </summary>
    public class SecurityTargetRegistry
    {
        private readonly AppDbContext _dbContext;

        public SecurityTargetRegistry(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<SecurityTargetRegistration> RegisterTargetAsync(SecurityTargetRegistration target, CancellationToken ct = default)
        {
            if (target.WorkspaceId == Guid.Empty)
                throw new ArgumentException("WorkspaceId must be provided.", nameof(target));
            if (target.SandboxId == Guid.Empty)
                throw new ArgumentException("SandboxId must be provided for isolated security evaluation.", nameof(target));

            // Prohibit external production targets
            if (target.Environment != TargetEnvironment.Sandbox && target.Environment != TargetEnvironment.IsolatedTest)
            {
                throw new SecurityException("Target Environment must be Sandbox or IsolatedTest. External production scanning is strictly prohibited.");
            }

            var existing = await _dbContext.SecurityTargetRegistrations
                .FirstOrDefaultAsync(t => t.WorkspaceId == target.WorkspaceId && t.SandboxId == target.SandboxId, ct);

            if (existing != null)
            {
                existing.TargetName = target.TargetName;
                existing.AllowedCapabilitiesJson = target.AllowedCapabilitiesJson;
                existing.IsActive = target.IsActive;
                await _dbContext.SaveChangesAsync(ct);
                return existing;
            }

            target.RegisteredAt = DateTime.UtcNow;
            _dbContext.SecurityTargetRegistrations.Add(target);
            await _dbContext.SaveChangesAsync(ct);
            return target;
        }

        public async Task<bool> ValidateTargetAllowlistAsync(Guid targetId, Guid workspaceId, string requestedCapability, CancellationToken ct = default)
        {
            var target = await _dbContext.SecurityTargetRegistrations
                .FirstOrDefaultAsync(t => t.Id == targetId && t.WorkspaceId == workspaceId && t.IsActive, ct);

            if (target == null)
            {
                throw new SecurityException(
                    $"DENIED: Target {targetId} is not in the pre-approved security allowlist for workspace {workspaceId}.");
            }

            if (target.Environment != TargetEnvironment.Sandbox && target.Environment != TargetEnvironment.IsolatedTest)
            {
                throw new SecurityException(
                    $"DENIED: Target {targetId} is not in an authorized test environment ({target.Environment}). Probes forbidden.");
            }

            if (!string.IsNullOrWhiteSpace(requestedCapability))
            {
                var allowed = JsonSerializer.Deserialize<List<string>>(target.AllowedCapabilitiesJson) ?? new List<string>();
                if (!allowed.Contains(requestedCapability) && !allowed.Contains("*"))
                {
                    throw new SecurityException(
                        $"DENIED: Capability '{requestedCapability}' is not granted for target {targetId}. Allowed: {target.AllowedCapabilitiesJson}.");
                }
            }

            return true;
        }

        public async Task<IReadOnlyList<SecurityTargetRegistration>> ListTargetsAsync(Guid workspaceId, CancellationToken ct = default)
        {
            return await _dbContext.SecurityTargetRegistrations
                .Where(t => t.WorkspaceId == workspaceId && t.IsActive)
                .ToListAsync(ct);
        }
    }
}
