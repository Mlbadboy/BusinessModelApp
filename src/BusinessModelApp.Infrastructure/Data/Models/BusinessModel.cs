using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BusinessModelApp.Infrastructure.Data.Models
{
    public class BusinessModel
    {
        public Guid Id { get; set; }
        
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;
        
        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        [Required]
        public Guid UserId { get; set; }
        
        // Navigation properties
        public virtual ICollection<RevenueSource> RevenueSources { get; set; } = new List<RevenueSource>();
        public virtual ICollection<Expense> Expenses { get; set; } = new List<Expense>();
    }
}
