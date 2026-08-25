using System;
using BusinessModelApp.Core.DTOs;
using System.Collections.Generic;

namespace BusinessModelApp.Core.Dtos
{
    public class UserDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public int RoleId { get; set; }
        public string Role { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsOnline { get; set; }
        public ICollection<RoleDto> Roles { get; set; } = new List<RoleDto>();
    }
}
