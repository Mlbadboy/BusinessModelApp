using System;
using System.ComponentModel.DataAnnotations;

namespace BusinessModelApp.Infrastructure.Data.Models
{
    public class Role
    {
        public Guid Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        public bool IsActive { get; set; } = true;
    }
}
