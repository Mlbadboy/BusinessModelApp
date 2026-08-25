using System;
using System.ComponentModel.DataAnnotations;

namespace BusinessModelApp.Infrastructure.Data.Models
{
    public class Expense
    {
        public Guid Id { get; set; }
        
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;
        
        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;
        
        public decimal Amount { get; set; }
        
        [MaxLength(50)]
        public string Currency { get; set; } = "USD";
        
        [MaxLength(100)]
        public string Category { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        [Required]
        public Guid BusinessModelId { get; set; }
        
        // Navigation property
        public virtual BusinessModel BusinessModel { get; set; } = null!;
    }
}
