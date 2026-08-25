using BusinessModelApp.Core.Domain.Users;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace BusinessModelApp.Infrastructure.Repositories
{
    public interface IRoleRepository
    {
        Task<Role> GetRoleByIdAsync(int id);
        Task<Role> GetRoleByNameAsync(string roleName);
        Task<IEnumerable<Role>> GetAllRolesAsync();
        Task AddRoleAsync(Role role);
        Task UpdateRoleAsync(Role role);
        Task DeleteRoleAsync(int id);
    }
}
