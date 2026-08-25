using BusinessModelApp.Core.Domain.Users;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BusinessModelApp.Core.Repositories
{
    public interface IRoleRepository
    {
        Task<Role> GetRoleByIdAsync(Guid id);
        Task<Role> GetRoleByNameAsync(string roleName);
        Task<IEnumerable<Role>> GetAllRolesAsync();
        Task AddRoleAsync(Role role);
        Task UpdateRoleAsync(Role role);
        Task DeleteRoleAsync(Guid id);
    }
}
