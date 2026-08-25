using System;

namespace BusinessModelApp.Core.DTOs.Expense
{
    public class ExpenseCategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public ExpenseType Type { get; set; }
        
        // Financial Data
        public decimal Budget { get; set; }
        public decimal ActualSpend { get; set; }
        public decimal Variance { get; set; }
        public decimal UtilizationRate { get; set; }  // Percentage
        
        // Category Details
        public string AccountCode { get; set; }
        public bool IsActive { get; set; }
        public int Priority { get; set; }  // 1-5
        public string[] Tags { get; set; }
        
        // Allocation
        public string Department { get; set; }
        public string CostCenter { get; set; }
        public string[] Projects { get; set; }
        public decimal AllocationPercentage { get; set; }
        
        // Limits and Controls
        public decimal MonthlyLimit { get; set; }
        public decimal YearlyLimit { get; set; }
        public bool RequiresApproval { get; set; }
        public decimal ApprovalThreshold { get; set; }
        
        // Tracking
        public string TrackingMethod { get; set; }  // "Per Unit", "Fixed", "Variable"
        public string BillingCycle { get; set; }  // "Monthly", "Quarterly", "Annually"
        public bool AutomaticPayment { get; set; }
        
        // Analysis
        public decimal TrendGrowthRate { get; set; }
        public string HealthStatus { get; set; }  // "Under Budget", "On Track", "Over Budget"
        public string[] OptimizationSuggestions { get; set; }
        
        // Meta Information
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public enum ExpenseType
    {
        FixedCost,
        VariableCost,
        OperatingExpense,
        CapitalExpenditure,
        Administrative,
        Marketing,
        IT,
        HR,
        Facilities,
        Travel,
        Other
    }
}
