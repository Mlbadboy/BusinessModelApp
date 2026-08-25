namespace BusinessModelApp.Core.DTOs.Expense
{
    public class ExpenseCategoryPerformanceDto
    {
        public string CategoryName { get; set; } = string.Empty;
        public decimal TotalExpenses { get; set; }
        public decimal Budget { get; set; }
        public decimal Variance { get; set; }
    }
}
