using System;

namespace BusinessModelApp.Core.Dtos.Analytics
{
    public class OpportunityAnalysisDto
    {
        public string OpportunityName { get; set; }
        public string Description { get; set; }
        public decimal PotentialValue { get; set; }
        public decimal SuccessProbability { get; set; }
        public string RequiredActions { get; set; }
    }
}
