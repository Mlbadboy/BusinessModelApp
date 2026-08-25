using System;
using System.Threading;
using System.Threading.Tasks;

namespace BusinessModelApp.Core.Interfaces
{
    public interface IUserContextService
    {
        Task<Guid> GetCurrentUserIdAsync(CancellationToken ct = default);
        Task<Guid> GetCurrentOrganizationIdAsync(CancellationToken ct = default);
        Task<Guid> GetAuthorizedWorkspaceIdAsync(Guid? requestedWorkspaceId = null, CancellationToken ct = default);
        Task<bool> HasWorkspaceAccessAsync(Guid workspaceId, CancellationToken ct = default);
    }
}
