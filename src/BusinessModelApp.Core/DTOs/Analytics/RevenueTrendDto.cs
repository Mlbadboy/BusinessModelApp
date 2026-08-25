using System;

namespace BusinessModelApp.Core.DTOs.Analytics
{
    public class RevenueTrendDto
    {
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
        public string TrendType { get; set; } = string.Empty; // e.g., "Daily", "Monthly"
    }
}
