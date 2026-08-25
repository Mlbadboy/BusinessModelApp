using BusinessModelApp.Core.Domain.Users;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace BusinessModelApp.Infrastructure.Repositories
{
    public interface IUserRepository
    {
        Task<User> GetUserByIdAsync(Guid id);
        Task<User> GetUserByEmailAsync(string email);
        Task<IEnumerable<User>> GetAllUsersAsync();
        Task AddUserAsync(User user);
        Task UpdateUserAsync(User user);
        Task DeleteUserAsync(Guid id);
    }
}
