using System;

namespace BusinessModelApp.Core.Dtos.Analytics
{
    public class RiskAssessmentDto
    {
        public string RiskName { get; set; }
        public string Description { get; set; }
        public decimal Likelihood { get; set; }
        public decimal Impact { get; set; }
        public decimal OverallRiskScore { get; set; }
        public string MitigationPlan { get; set; }
    }
}
