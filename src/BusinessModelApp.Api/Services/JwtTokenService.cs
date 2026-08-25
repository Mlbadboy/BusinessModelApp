using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BusinessModelApp.Core.Domain.Users;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace BusinessModelApp.Api.Services
{
    public interface IJwtTokenService
    {
        string GenerateToken(User user, IList<string> roles, out DateTime expiresAt);
    }

    public class JwtTokenService : IJwtTokenService
    {
        private readonly IConfiguration _configuration;

        public JwtTokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerateToken(User user, IList<string> roles, out DateTime expiresAt)
        {
            var jwtKey = _configuration["Jwt:Key"] ?? "SecureSecretKeyForBusinessModelAppAuthentication2026";
            var issuer = _configuration["Jwt:Issuer"] ?? "BusinessModelApp";
            var audience = _configuration["Jwt:Audience"] ?? "BusinessModelAppClient";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            expiresAt = DateTime.UtcNow.AddDays(7);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.GivenName, user.FirstName ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.FamilyName, user.LastName ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            if (user.OrganizationId.HasValue)
            {
                claims.Add(new Claim("organization_id", user.OrganizationId.Value.ToString()));
            }

            if (user.DefaultWorkspaceId.HasValue)
            {
                claims.Add(new Claim("workspace_id", user.DefaultWorkspaceId.Value.ToString()));
            }

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expiresAt,
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
