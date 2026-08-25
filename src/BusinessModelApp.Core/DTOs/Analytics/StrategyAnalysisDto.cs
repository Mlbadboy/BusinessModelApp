namespace BusinessModelApp.Core.DTOs.Analytics
{
    public class StrategyAnalysisDto
    {
        public double StrategyEffectiveness { get; set; }
        public double ResourceAllocation { get; set; }
        public string RiskAnalysis { get; set; }
        public string OpportunityAnalysis { get; set; }
        public List<PerformanceTrendDto> PerformanceTrends { get; set; }
    }
}
