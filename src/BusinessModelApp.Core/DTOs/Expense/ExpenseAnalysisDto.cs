using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.DTOs.Expense
{
    public class ExpenseAnalysisDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime AnalysisDate { get; set; }
        
        // Expense Overview
        public decimal TotalExpenses { get; set; }
        public decimal FixedExpenses { get; set; }
        public decimal VariableExpenses { get; set; }
        public decimal ProjectedExpenses { get; set; }
        
        // Growth Metrics
        public decimal YearOverYearGrowth { get; set; }
        public decimal QuarterOverQuarterGrowth { get; set; }
        public decimal MonthOverMonthGrowth { get; set; }
        
        // Expense Categories
        public ICollection<ExpenseCategoryDto> Categories { get; set; }
        public IDictionary<string, decimal> ExpenseBreakdown { get; set; }
        public ICollection<ExpenseTrendDto> ExpenseTrends { get; set; }
        public ICollection<ExpenseCategoryDto> CategoryPerformance { get; set; }
        public IDictionary<string, decimal> ExpensesByCategory { get; set; }
        
        // Budget Analysis
        public decimal BudgetVariance { get; set; }
        public decimal BudgetUtilization { get; set; }  // Percentage
        public decimal ForecastAccuracy { get; set; }  // Percentage
        
        // Efficiency Metrics
        public decimal CostPerRevenueDollar { get; set; }
        public decimal OperatingEfficiencyRatio { get; set; }
        public decimal ExpenseToIncomeRatio { get; set; }
        
        // Trends and Forecasts
        public ICollection<ExpenseTrendDto> Trends { get; set; }
        public IDictionary<string, decimal> Forecasts { get; set; }
        
        // Risk Analysis
        public ICollection<ExpenseRiskDto> Risks { get; set; }
        public decimal RiskScore { get; set; }
        
        // Optimization Opportunities
        public ICollection<ExpenseOptimizationDto> Optimizations { get; set; }
        public decimal PotentialSavings { get; set; }
        
        // Compliance and Policy
        public bool IsCompliant { get; set; }
        public string[] ComplianceIssues { get; set; }
        public string[] PolicyViolations { get; set; }
        
        // Meta Information
        public string AnalyzedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string Status { get; set; }
    }
}
