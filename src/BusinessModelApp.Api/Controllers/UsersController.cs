using Microsoft.AspNetCore.Mvc;
using BusinessModelApp.Core.Dtos;
using BusinessModelApp.Core.Domain.Users;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using BusinessModelApp.Core.Interfaces;
using BusinessModelApp.Core.Repositories;

namespace BusinessModelApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserRepository _userRepository;

        public UsersController(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
        {
            var users = await _userRepository.GetAllUsersAsync();
            var userDtos = users.Select(u => new UserDto
            {
                Id = u.Id.ToString(),
                Name = u.UserName, // Simplified mapping
                Email = u.Email,
                Role = u.Role ?? "User", // Use role from entity or default
                IsActive = true,
                IsOnline = true
            });

            return Ok(userDtos);
        }
    }
}


