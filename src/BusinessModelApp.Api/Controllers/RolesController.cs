using Microsoft.AspNetCore.Mvc;
using BusinessModelApp.Core.Interfaces;
using BusinessModelApp.Core.Repositories;
using BusinessModelApp.Core.Domain.Users;
using BusinessModelApp.Core.DTOs;
using BusinessModelApp.Core.Dtos;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;

namespace BusinessModelApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RolesController : ControllerBase
    {
        private readonly IRoleRepository _roleRepository;

        public RolesController(IRoleRepository roleRepository)
        {
            _roleRepository = roleRepository;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<RoleDto>>> GetRoles()
        {
            var roles = await _roleRepository.GetAllRolesAsync();
            var roleDtos = roles.Select(r => new RoleDto { Id = r.Id, Name = r.Name, RoleName = r.Name }).ToList();
            return Ok(roleDtos);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<RoleDto>> GetRole(Guid id)
        {
            var role = await _roleRepository.GetRoleByIdAsync(id);
            if (role == null)
            {
                return NotFound();
            }
            return Ok(new RoleDto { Id = role.Id, Name = role.Name });
        }

        [HttpPost]
        public async Task<ActionResult<RoleDto>> CreateRole(RoleDto roleDto)
        {
            var role = new Role(roleDto.Name, roleDto.Description ?? "No description");
            await _roleRepository.AddRoleAsync(role);
            roleDto.Id = role.Id;
            return CreatedAtAction(nameof(GetRole), new { id = role.Id }, roleDto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRole(Guid id, RoleDto roleDto)
        {
            if (id != roleDto.Id)
            {
                return BadRequest();
            }

            var role = await _roleRepository.GetRoleByIdAsync(id);
            if (role == null) return NotFound();

            role.Update(roleDto.Name, roleDto.Description ?? "No description");
            await _roleRepository.UpdateRoleAsync(role);
            return Ok(roleDto);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRole(Guid id)
        {
            await _roleRepository.DeleteRoleAsync(id);
            return NoContent();
        }

        /*
        [HttpGet("users/{roleId}")]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsersInRole(Guid roleId)
        {
            // var users = await _roleRepository.GetUsersInRoleAsync(roleId);
            // return Ok(users);
            return Ok(new List<UserDto>());
        }
        */
    }
}
