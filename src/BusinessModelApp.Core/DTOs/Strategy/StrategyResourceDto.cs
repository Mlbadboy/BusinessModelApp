namespace BusinessModelApp.Core.DTOs.Strategy
{
    public class StrategyResourceDto
    {
        public string ResourceName { get; set; } = string.Empty;
        public decimal AllocatedBudget { get; set; }
        public int AllocatedPersonnel { get; set; }
    }
}
