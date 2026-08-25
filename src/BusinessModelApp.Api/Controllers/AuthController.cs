using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BusinessModelApp.Api.Services;
using BusinessModelApp.Core.Domain.Commercial;
using BusinessModelApp.Core.Domain.Users;
using BusinessModelApp.Core.DTOs.Auth;
using BusinessModelApp.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BusinessModelApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly AppDbContext _context;

        public AuthController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            IJwtTokenService jwtTokenService,
            AppDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtTokenService = jwtTokenService;
            _context = context;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { message = "Email and password are required." });
            }

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null || !user.IsActive)
            {
                return Unauthorized(new { message = "Invalid email or password." });
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);
            if (!result.Succeeded)
            {
                return Unauthorized(new { message = "Invalid email or password." });
            }

            user.LastLogin = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            var roles = await _userManager.GetRolesAsync(user);
            var token = _jwtTokenService.GenerateToken(user, roles, out var expiresAt);

            var org = user.OrganizationId.HasValue ? await _context.Organizations.FindAsync(user.OrganizationId.Value) : null;
            var workspace = user.DefaultWorkspaceId.HasValue ? await _context.Workspaces.FindAsync(user.DefaultWorkspaceId.Value) : null;

            return Ok(new AuthResponseDto
            {
                Token = token,
                ExpiresAt = expiresAt,
                User = new UserProfileDto
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = roles.FirstOrDefault() ?? user.Role,
                    OrganizationId = user.OrganizationId,
                    OrganizationName = org?.Name ?? string.Empty,
                    DefaultWorkspaceId = user.DefaultWorkspaceId,
                    WorkspaceName = workspace?.Name ?? string.Empty,
                    Permissions = roles.ToList()
                }
            });
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { message = "Email and password are required." });
            }

            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return Conflict(new { message = "A user with this email already exists." });
            }

            // Create Organization & Default Workspace for new registration
            var orgName = string.IsNullOrWhiteSpace(request.OrganizationName) 
                ? $"{request.FirstName}'s Organization" 
                : request.OrganizationName;

            var slug = orgName.ToLower().Replace(" ", "-") + "-" + Guid.NewGuid().ToString().Substring(0, 6);

            var org = new Organization
            {
                Name = orgName,
                Slug = slug,
                Plan = "Free",
                IsActive = true
            };
            _context.Organizations.Add(org);
            await _context.SaveChangesAsync();

            var workspace = new Workspace
            {
                OrganizationId = org.Id,
                Name = "Primary Workspace",
                Description = "Default operating workspace",
                Currency = "INR",
                IsActive = true
            };
            _context.Workspaces.Add(workspace);
            await _context.SaveChangesAsync();

            var user = new User
            {
                UserName = request.Email,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                OrganizationId = org.Id,
                DefaultWorkspaceId = workspace.Id,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createResult = await _userManager.CreateAsync(user, request.Password);
            if (!createResult.Succeeded)
            {
                return BadRequest(new { errors = createResult.Errors.Select(e => e.Description) });
            }

            await _userManager.AddToRoleAsync(user, "CEO");

            var roles = await _userManager.GetRolesAsync(user);
            var token = _jwtTokenService.GenerateToken(user, roles, out var expiresAt);

            return Ok(new AuthResponseDto
            {
                Token = token,
                ExpiresAt = expiresAt,
                User = new UserProfileDto
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = "CEO",
                    OrganizationId = org.Id,
                    OrganizationName = org.Name,
                    DefaultWorkspaceId = workspace.Id,
                    WorkspaceName = workspace.Name,
                    Permissions = roles.ToList()
                }
            });
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<UserProfileDto>> GetCurrentUser()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized();
            }

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null || !user.IsActive)
            {
                return Unauthorized();
            }

            var roles = await _userManager.GetRolesAsync(user);
            var org = user.OrganizationId.HasValue ? await _context.Organizations.FindAsync(user.OrganizationId.Value) : null;
            var workspace = user.DefaultWorkspaceId.HasValue ? await _context.Workspaces.FindAsync(user.DefaultWorkspaceId.Value) : null;

            return Ok(new UserProfileDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = roles.FirstOrDefault() ?? user.Role,
                OrganizationId = user.OrganizationId,
                OrganizationName = org?.Name ?? string.Empty,
                DefaultWorkspaceId = user.DefaultWorkspaceId,
                WorkspaceName = workspace?.Name ?? string.Empty,
                Permissions = roles.ToList()
            });
        }

        [HttpPost("logout")]
        [Authorize]
        public IActionResult Logout()
        {
            return Ok(new { message = "Logged out successfully." });
        }
    }
}
