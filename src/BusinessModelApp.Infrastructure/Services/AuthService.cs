using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Users;
using BusinessModelApp.Infrastructure.Data;
using BusinessModelApp.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace BusinessModelApp.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IRepository<User> _userRepository;
        private readonly IRepository<Role> _roleRepository;
        // private readonly IRepository<UserRole> _userRoleRepository;
        // private readonly IRepository<RolePermission> _rolePermissionRepository;

        public AuthService(
            AppDbContext context,
            IConfiguration configuration,
            IRepository<User> userRepository,
            IRepository<Role> roleRepository,
            // IRepository<UserRole> userRoleRepository,
            IRepository<RolePermission> rolePermissionRepository)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _roleRepository = roleRepository ?? throw new ArgumentNullException(nameof(roleRepository));
            // _userRoleRepository = userRoleRepository ?? throw new ArgumentNullException(nameof(userRoleRepository));
            _rolePermissionRepository = rolePermissionRepository ?? throw new ArgumentNullException(nameof(rolePermissionRepository));
        }

        public async Task<(bool Success, string Token, User User)> AuthenticateAsync(
            string email, 
            string password, 
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email cannot be empty", nameof(email));
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password cannot be empty", nameof(password));

            var user = await _userRepository.FirstOrDefaultAsync(
                u => u.Email.ToLower() == email.ToLower() && !u.IsDeleted, 
                cancellationToken);

            if (user == null || !user.IsActive || !VerifyPasswordHash(password, user.HashedPassword, user.Salt))
                return (false, null, null);

            // Update last login
            user.UpdateLastLogin(DateTime.UtcNow);
            await _userRepository.SaveChangesAsync(cancellationToken);

            // Generate JWT token
            var token = GenerateJwtToken(user);
            return (true, token, user);
        }

        public async Task<(bool Success, string[] Errors)> RegisterAsync(
            string email, 
            string password, 
            string firstName, 
            string lastName, 
            CancellationToken cancellationToken = default)
        {
            var errors = new List<string>();
            
            // Validate input
            if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
                errors.Add("Email is not valid");
                
            if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
                errors.Add("Password must be at least 8 characters long");
                
            if (string.IsNullOrWhiteSpace(firstName))
                errors.Add("First name is required");
                
            if (string.IsNullOrWhiteSpace(lastName))
                errors.Add("Last name is required");
                
            // Check if user already exists
            var existingUser = await _userRepository.FirstOrDefaultAsync(
                u => u.Email.ToLower() == email.ToLower(), 
                cancellationToken);
                
            if (existingUser != null)
                errors.Add("User with this email already exists");
                
            if (errors.Any())
                return (false, errors.ToArray());
            
            // Create password hash and salt
            CreatePasswordHash(password, out var passwordHash, out var salt);
            
            // Create new user
            var user = new User(
                email: email,
                firstName: firstName.Trim(),
                lastName: lastName.Trim(),
                hashedPassword: passwordHash,
                salt: salt);
            
            // Assign default role (User)
            var defaultRole = await _roleRepository.FirstOrDefaultAsync(
                r => r.Name == "User" && !r.IsDeleted, 
                cancellationToken);
                
            if (defaultRole == null)
                throw new InvalidOperationException("Default user role not found");
            
            // Add user to database
            await _userRepository.AddAsync(user, cancellationToken);
            
            // Assign default role
            // var userRole = new UserRole { UserId = user.Id, RoleId = defaultRole.Id };
            // await _userRoleRepository.AddAsync(userRole, cancellationToken);
            
            await _userRepository.SaveChangesAsync(cancellationToken);
            
            return (true, Array.Empty<string>());
        }

        public async Task<(bool Success, string[] Errors)> ChangePasswordAsync(
            Guid userId, 
            string currentPassword, 
            string newPassword, 
            CancellationToken cancellationToken = default)
        {
            var errors = new List<string>();
            
            if (string.IsNullOrWhiteSpace(currentPassword))
                errors.Add("Current password is required");
                
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
                errors.Add("New password must be at least 8 characters long");
                
            if (errors.Any())
                return (false, errors.ToArray());
            
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            
            if (user == null || !user.IsActive || user.IsDeleted)
                return (false, new[] { "User not found or inactive" });
                
            if (!VerifyPasswordHash(currentPassword, user.HashedPassword, user.Salt))
                return (false, new[] { "Current password is incorrect" });
                
            // Update password
            CreatePasswordHash(newPassword, out var newPasswordHash, out var newSalt);
            user.UpdatePassword(newPasswordHash, newSalt);
            
            await _userRepository.SaveChangesAsync(cancellationToken);
            
            return (true, Array.Empty<string>());
        }

        public async Task<bool> ResetPasswordAsync(
            string email, 
            string token, 
            string newPassword, 
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(newPassword))
                return false;
                
            // In a real implementation, validate the reset token
            // For now, we'll just find the user by email and update the password
            var user = await _userRepository.FirstOrDefaultAsync(
                u => u.Email.ToLower() == email.ToLower() && u.IsActive && !u.IsDeleted, 
                cancellationToken);
                
            if (user == null)
                return false;
                
            // Update password
            CreatePasswordHash(newPassword, out var passwordHash, out var salt);
            user.UpdatePassword(passwordHash, salt);
            
            await _userRepository.SaveChangesAsync(cancellationToken);
            
            return true;
        }

        public async Task<string> GeneratePasswordResetTokenAsync(
            string email, 
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(email))
                return null;
                
            var user = await _userRepository.FirstOrDefaultAsync(
                u => u.Email.ToLower() == email.ToLower() && u.IsActive && !u.IsDeleted, 
                cancellationToken);
                
            if (user == null)
                return null;
                
            // In a real implementation, generate a secure token and store it with an expiration
            // For now, we'll just return a simple token for demonstration
            var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
                .Replace("/", "")
                .Replace("+", "")
                .Replace("=", "");
                
            return token;
        }

        public async Task<User> GetUserAsync(
            ClaimsPrincipal principal, 
            CancellationToken cancellationToken = default)
        {
            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return null;
                
            return await _userRepository.GetByIdAsync(userId, cancellationToken);
        }

        public async Task<bool> HasPermissionAsync(
            Guid userId, 
            string permission, 
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(permission))
                return false;
                
            // Check if user has the specified permission through any of their roles
            var hasPermission = await _context.UserRoles
                .Where(ur => ur.UserId == userId && !ur.Role.IsDeleted)
                .Join(
                    _context.RolePermissions,
                    ur => ur.RoleId,
                    rp => rp.RoleId,
                    (ur, rp) => rp.Permission)
                .AnyAsync(p => p == permission, cancellationToken);
                
            return hasPermission;
        }

        public async Task<IEnumerable<string>> GetUserPermissionsAsync(
            Guid userId, 
            CancellationToken cancellationToken = default)
        {
            // Get all permissions for the user's roles
            var permissions = await _context.UserRoles
                .Where(ur => ur.UserId == userId && !ur.Role.IsDeleted)
                .Join(
                    _context.RolePermissions,
                    ur => ur.RoleId,
                    rp => rp.RoleId,
                    (ur, rp) => rp.Permission)
                .Distinct()
                .ToListAsync(cancellationToken);
                
            return permissions;
        }

        #region Private Methods

        private string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Secret"]);
            
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] 
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.GivenName, user.FirstName),
                    new Claim(ClaimTypes.Surname, user.LastName),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key), 
                    SecurityAlgorithms.HmacSha256Signature)
            };
            
            // Add roles as claims
            var roles = _context.UserRoles
                .Where(ur => ur.UserId == user.Id && !ur.Role.IsDeleted)
                .Select(ur => ur.Role.Name)
                .ToList();
                
            foreach (var role in roles)
            {
                tokenDescriptor.Subject.AddClaim(new Claim(ClaimTypes.Role, role));
            }
            
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private static void CreatePasswordHash(string password, out string passwordHash, out string salt)
        {
            if (password == null) throw new ArgumentNullException(nameof(password));
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("Value cannot be empty or whitespace only string.", nameof(password));

            using var hmac = new HMACSHA512();
            salt = Convert.ToBase64String(hmac.Key);
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            passwordHash = Convert.ToBase64String(computedHash);
        }

        private static bool VerifyPasswordHash(string password, string storedHash, string storedSalt)
        {
            if (password == null) throw new ArgumentNullException(nameof(password));
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("Value cannot be empty or whitespace only string.", nameof(password));
            if (string.IsNullOrEmpty(storedHash)) throw new ArgumentException("Invalid length of password hash (64 bytes expected).", nameof(storedHash));
            if (string.IsNullOrEmpty(storedSalt)) throw new ArgumentException("Invalid length of password salt (128 bytes expected).", nameof(storedSalt));

            using var hmac = new HMACSHA512(Convert.FromBase64String(storedSalt));
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            var computedHashString = Convert.ToBase64String(computedHash);
            
            return computedHashString == storedHash;
        }

        #endregion
    }
}
