using System;

namespace BusinessModelApp.Core.DTOs.Analytics
{
    public class OpportunityAnalysisDto2
    {
        public string OpportunityName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal PotentialValue { get; set; }
        public decimal SuccessProbability { get; set; }
        public string RequiredActions { get; set; } = string.Empty;
    }
}
