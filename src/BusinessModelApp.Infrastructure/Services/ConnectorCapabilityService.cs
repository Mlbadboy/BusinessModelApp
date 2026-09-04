using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Connectors;
using BusinessModelApp.Core.Interfaces;
using BusinessModelApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BusinessModelApp.Infrastructure.Services
{
    public class ConnectorCapabilityService : IConnectorCapabilityService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ConnectorCapabilityService> _logger;

        public ConnectorCapabilityService(
            AppDbContext context,
            ILogger<ConnectorCapabilityService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<CapabilityPermissionMode> GetCapabilityPermissionAsync(
            Guid workspaceId,
            ConnectorProvider provider,
            string capabilityKey,
            CancellationToken ct = default)
        {
            var capabilities = await GetCapabilitiesAsync(workspaceId, provider, ct);
            if (capabilities.TryGetValue(capabilityKey, out var mode))
            {
                return mode;
            }

            // Fallback to provider default schema
            var defaults = GetDefaultCapabilities(provider);
            return defaults.TryGetValue(capabilityKey, out var defaultMode) ? defaultMode : CapabilityPermissionMode.Denied;
        }

        public async Task<Dictionary<string, CapabilityPermissionMode>> GetCapabilitiesAsync(
            Guid workspaceId,
            ConnectorProvider provider,
            CancellationToken ct = default)
        {
            var entity = await _context.Connectors.FirstOrDefaultAsync(c => c.WorkspaceId == workspaceId && c.Provider == provider, ct);
            if (entity == null || string.IsNullOrWhiteSpace(entity.CapabilitiesJson) || entity.CapabilitiesJson == "{}")
            {
                return GetDefaultCapabilities(provider);
            }

            try
            {
                var stored = JsonSerializer.Deserialize<Dictionary<string, CapabilityPermissionMode>>(entity.CapabilitiesJson);
                var merged = GetDefaultCapabilities(provider);

                if (stored != null)
                {
                    foreach (var kv in stored)
                    {
                        merged[kv.Key] = kv.Value;
                    }
                }
                return merged;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse CapabilitiesJson for workspace {WorkspaceId}, provider {Provider}. Using defaults.", workspaceId, provider);
                return GetDefaultCapabilities(provider);
            }
        }

        public async Task UpdateCapabilitiesAsync(
            Guid workspaceId,
            ConnectorProvider provider,
            Dictionary<string, CapabilityPermissionMode> updatedCapabilities,
            CancellationToken ct = default)
        {
            var entity = await _context.Connectors.FirstOrDefaultAsync(c => c.WorkspaceId == workspaceId && c.Provider == provider, ct);
            if (entity == null)
            {
                entity = new ConnectorEntity
                {
                    WorkspaceId = workspaceId,
                    Provider = provider,
                    Status = ConnectorStatus.Configured,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Connectors.Add(entity);
            }

            var current = await GetCapabilitiesAsync(workspaceId, provider, ct);
            foreach (var kv in updatedCapabilities)
            {
                current[kv.Key] = kv.Value;
            }

            entity.CapabilitiesJson = JsonSerializer.Serialize(current);
            entity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("Updated capability permissions for provider {Provider} in workspace {WorkspaceId}", provider, workspaceId);
        }

        public Dictionary<string, CapabilityPermissionMode> GetDefaultCapabilities(ConnectorProvider provider)
        {
            return provider switch
            {
                ConnectorProvider.GoogleWorkspace => new Dictionary<string, CapabilityPermissionMode>
                {
                    { "read_email", CapabilityPermissionMode.Allowed },
                    { "draft_email", CapabilityPermissionMode.Allowed },
                    { "send_email", CapabilityPermissionMode.RequireApproval },
                    { "read_calendar", CapabilityPermissionMode.Allowed },
                    { "create_calendar_event", CapabilityPermissionMode.RequireApproval },
                    { "read_drive", CapabilityPermissionMode.Allowed }
                },

                ConnectorProvider.Microsoft365 => new Dictionary<string, CapabilityPermissionMode>
                {
                    { "read_outlook", CapabilityPermissionMode.Allowed },
                    { "send_outlook", CapabilityPermissionMode.RequireApproval },
                    { "read_calendar", CapabilityPermissionMode.Allowed },
                    { "create_meeting", CapabilityPermissionMode.RequireApproval }
                },

                ConnectorProvider.Vapi or ConnectorProvider.Retell or ConnectorProvider.Twilio => new Dictionary<string, CapabilityPermissionMode>
                {
                    { "dispatch_test_call", CapabilityPermissionMode.Allowed },
                    { "dispatch_live_call", CapabilityPermissionMode.RequireApproval },
                    { "read_transcripts", CapabilityPermissionMode.Allowed },
                    { "reconcile_costs", CapabilityPermissionMode.Allowed }
                },

                ConnectorProvider.Razorpay or ConnectorProvider.Stripe => new Dictionary<string, CapabilityPermissionMode>
                {
                    { "create_payment_link", CapabilityPermissionMode.Allowed },
                    { "create_customer", CapabilityPermissionMode.Allowed },
                    { "verify_webhook", CapabilityPermissionMode.Allowed },
                    { "issue_refund", CapabilityPermissionMode.RequireApproval }
                },

                ConnectorProvider.GitHub => new Dictionary<string, CapabilityPermissionMode>
                {
                    { "read_repository", CapabilityPermissionMode.Allowed },
                    { "create_repository", CapabilityPermissionMode.RequireApproval },
                    { "commit_code", CapabilityPermissionMode.RequireApproval }
                },

                ConnectorProvider.Vercel => new Dictionary<string, CapabilityPermissionMode>
                {
                    { "read_deployments", CapabilityPermissionMode.Allowed },
                    { "trigger_deployment", CapabilityPermissionMode.RequireApproval }
                },

                _ => new Dictionary<string, CapabilityPermissionMode>()
            };
        }
    }
}
