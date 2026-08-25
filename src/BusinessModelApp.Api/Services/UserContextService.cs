using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Interfaces;
using BusinessModelApp.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BusinessModelApp.Api.Services
{
    public class UserContextService : IUserContextService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly AppDbContext _context;

        public UserContextService(IHttpContextAccessor httpContextAccessor, AppDbContext context)
        {
            _httpContextAccessor = httpContextAccessor;
            _context = context;
        }

        private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

        public Task<Guid> GetCurrentUserIdAsync(CancellationToken ct = default)
        {
            var userClaim = Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? Principal?.FindFirst("sub")?.Value;

            if (string.IsNullOrWhiteSpace(userClaim) || !Guid.TryParse(userClaim, out var userId))
            {
                throw new UnauthorizedAccessException("User is not authenticated or claims are invalid.");
            }

            return Task.FromResult(userId);
        }

        public async Task<Guid> GetCurrentOrganizationIdAsync(CancellationToken ct = default)
        {
            var userId = await GetCurrentUserIdAsync(ct);
            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);

            if (user == null || !user.IsActive)
            {
                throw new UnauthorizedAccessException("User account is inactive or not found.");
            }

            if (!user.OrganizationId.HasValue)
            {
                throw new UnauthorizedAccessException("User does not belong to an active organization.");
            }

            return user.OrganizationId.Value;
        }

        public async Task<Guid> GetAuthorizedWorkspaceIdAsync(Guid? requestedWorkspaceId = null, CancellationToken ct = default)
        {
            var userId = await GetCurrentUserIdAsync(ct);
            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);

            if (user == null || !user.IsActive || !user.OrganizationId.HasValue)
            {
                throw new UnauthorizedAccessException("User account or organization is inactive.");
            }

            var targetWorkspaceId = requestedWorkspaceId ?? user.DefaultWorkspaceId;

            if (!targetWorkspaceId.HasValue || targetWorkspaceId.Value == Guid.Empty)
            {
                // Fallback to first active workspace in user's organization
                var firstWorkspace = await _context.Workspaces
                    .AsNoTracking()
                    .FirstOrDefaultAsync(w => w.OrganizationId == user.OrganizationId.Value && !w.IsDeleted && w.IsActive, ct);

                if (firstWorkspace == null)
                {
                    throw new KeyNotFoundException("No active workspace found for this organization.");
                }

                return firstWorkspace.Id;
            }

            // Verify requested workspace strictly belongs to the user's organization
            var authorized = await _context.Workspaces
                .AsNoTracking()
                .AnyAsync(w => w.Id == targetWorkspaceId.Value && w.OrganizationId == user.OrganizationId.Value && !w.IsDeleted && w.IsActive, ct);

            if (!authorized)
            {
                throw new UnauthorizedAccessException($"Access denied: Workspace {targetWorkspaceId.Value} does not belong to user's organization.");
            }

            return targetWorkspaceId.Value;
        }

        public async Task<bool> HasWorkspaceAccessAsync(Guid workspaceId, CancellationToken ct = default)
        {
            try
            {
                var orgId = await GetCurrentOrganizationIdAsync(ct);
                return await _context.Workspaces
                    .AsNoTracking()
                    .AnyAsync(w => w.Id == workspaceId && w.OrganizationId == orgId && !w.IsDeleted && w.IsActive, ct);
            }
            catch
            {
                return false;
            }
        }
    }
}
