using System;
using System.ComponentModel.DataAnnotations;

namespace BusinessModelApp.Core.DTOs.Revenue
{
    public class RevenueSourceDto
    {
        public string Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        [Required]
        public string Type { get; set; } = string.Empty; // e.g., "Subscription", "One-time", "Recurring"
        
        [Required]
        public decimal Amount { get; set; }
        
        public string? Currency { get; set; } = "USD";
        
        [Required]
        public int BusinessModelId { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public DateTime? StartDate { get; set; }
        
        public DateTime? EndDate { get; set; }
        
        public string? BillingCycle { get; set; } // e.g., "Monthly", "Quarterly", "Annually"
        
        public int? ExpectedCustomers { get; set; }
        
        public string? CreatedBy { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        public string? LastModifiedBy { get; set; }
        
        public DateTime? LastModifiedAt { get; set; }

        public double GrowthRate { get; set; }
    }
}
