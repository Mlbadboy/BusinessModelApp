using BusinessModelApp.Core.Domain.Users;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace BusinessModelApp.Infrastructure.Services
{
    public interface IAuthService
    {
        Task<(bool Success, string Token, User User)> AuthenticateAsync(string email, string password, CancellationToken cancellationToken = default);
        Task<(bool Success, string[] Errors)> RegisterAsync(string email, string password, string firstName, string lastName, CancellationToken cancellationToken = default);
        Task<(bool Success, string[] Errors)> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default);
        Task<bool> ResetPasswordAsync(string email, string token, string newPassword, CancellationToken cancellationToken = default);
        Task<string> GeneratePasswordResetTokenAsync(string email, CancellationToken cancellationToken = default);
        Task<User> GetUserAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default);
        Task<bool> HasPermissionAsync(Guid userId, string permission, CancellationToken cancellationToken = default);
        Task<IEnumerable<string>> GetUserPermissionsAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}
