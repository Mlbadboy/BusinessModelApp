namespace BusinessModelApp.Core.DTOs.Analytics
{
    public class RevenueSourcePerformanceDto
    {
        public string SourceName { get; set; } = string.Empty;
        public decimal TotalRevenue { get; set; }
        public int NumberOfTransactions { get; set; }
        public decimal AverageTransactionValue { get; set; }
    }
}
