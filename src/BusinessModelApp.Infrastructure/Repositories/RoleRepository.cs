using BusinessModelApp.Core.Domain.Users;
using BusinessModelApp.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BusinessModelApp.Infrastructure.Repositories
{
    public class RoleRepository : IRoleRepository
    {
        private readonly AppDbContext _context;
        private readonly RoleManager<Role> _roleManager;

        public RoleRepository(AppDbContext context, RoleManager<Role> roleManager)
        {
            _context = context;
            _roleManager = roleManager;
        }

        public async Task<Role> GetRoleByIdAsync(int id)
        {
            return await _roleManager.FindByIdAsync(id.ToString());
        }

        public async Task<Role> GetRoleByNameAsync(string roleName)
        {
            return await _roleManager.FindByNameAsync(roleName);
        }

        public async Task<IEnumerable<Role>> GetAllRolesAsync()
        {
            return await _roleManager.Roles
                .Include(r => r.UserRoles)
                .Include(r => r.RolePermissions)
                .ToListAsync();
        }

        public async Task AddRoleAsync(Role role)
        {
            var result = await _roleManager.CreateAsync(role);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Failed to create role: {string.Join(", ", result.Errors)}");
            }
        }

        public async Task UpdateRoleAsync(Role role)
        {
            var result = await _roleManager.UpdateAsync(role);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Failed to update role: {string.Join(", ", result.Errors)}");
            }
        }

        public async Task DeleteRoleAsync(int id)
        {
            var role = await _roleManager.FindByIdAsync(id.ToString());
            if (role != null)
            {
                var result = await _roleManager.DeleteAsync(role);
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException($"Failed to delete role: {string.Join(", ", result.Errors)}");
                }
            }
        }
    }
}
