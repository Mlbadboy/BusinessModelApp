using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Connectors;

namespace BusinessModelApp.Core.Interfaces
{
    public interface IConnectorCapabilityService
    {
        Task<CapabilityPermissionMode> GetCapabilityPermissionAsync(
            Guid workspaceId,
            ConnectorProvider provider,
            string capabilityKey,
            CancellationToken ct = default);

        Task<Dictionary<string, CapabilityPermissionMode>> GetCapabilitiesAsync(
            Guid workspaceId,
            ConnectorProvider provider,
            CancellationToken ct = default);

        Task UpdateCapabilitiesAsync(
            Guid workspaceId,
            ConnectorProvider provider,
            Dictionary<string, CapabilityPermissionMode> updatedCapabilities,
            CancellationToken ct = default);

        Dictionary<string, CapabilityPermissionMode> GetDefaultCapabilities(ConnectorProvider provider);
    }
}
