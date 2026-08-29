using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessModelApp.Core.Connectors;

namespace BusinessModelApp.Core.Services
{
    public interface ICharlieConnectService
    {
        Task<List<CharlieConnection>> GetConnectionsAsync(Guid workspaceId);
        Task<CharlieConnection> ConnectProviderAsync(Guid workspaceId, ConnectionProvider provider, string accountIdentifier, List<string> requestedScopes);
        Task<bool> DisconnectProviderAsync(Guid workspaceId, ConnectionProvider provider);
        Task<bool> ValidateActionPermissionAsync(Guid workspaceId, ConnectionProvider provider, string actionName);
        Task<CharlieConnection> TestConnectionAsync(Guid workspaceId, ConnectionProvider provider);
    }
}
