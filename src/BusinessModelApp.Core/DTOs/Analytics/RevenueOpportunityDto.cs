namespace BusinessModelApp.Core.DTOs.Analytics
{
    public class RevenueOpportunityDto
    {
        public string OpportunityName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal PotentialRevenue { get; set; }
        public double SuccessProbability { get; set; }
    }
}
