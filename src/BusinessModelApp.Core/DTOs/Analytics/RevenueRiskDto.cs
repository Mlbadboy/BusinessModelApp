namespace BusinessModelApp.Core.DTOs.Analytics
{
    public class RevenueRiskDto
    {
        public string RiskName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal PotentialImpact { get; set; }
        public double Probability { get; set; }
    }
}
