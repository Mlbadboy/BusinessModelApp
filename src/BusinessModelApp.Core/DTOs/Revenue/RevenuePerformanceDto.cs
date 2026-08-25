using System.Collections.Generic;

namespace BusinessModelApp.Core.DTOs.Revenue
{
    public class RevenuePerformanceDto
    {
        public string RevenueStreamName { get; set; }
        public decimal ActualAmount { get; set; }
        public decimal TargetAmount { get; set; }
        public decimal Variance { get; set; }
        public List<string> KeyContributors { get; set; }
    }
}
