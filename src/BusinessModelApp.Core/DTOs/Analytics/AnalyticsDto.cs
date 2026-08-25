using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.Dtos.Analytics
{
    public class RevenueAnalysisDto
    {
        public decimal TotalRevenue { get; set; }
        public decimal TargetRevenue { get; set; }
        public decimal RevenueVariance { get; set; }
        public decimal GrowthRate { get; set; }
        public decimal MonthlyGrowthRate { get; set; }
        public decimal QuarterlyGrowthRate { get; set; }
        public decimal YearlyGrowthRate { get; set; }
        public Dictionary<string, decimal> RevenueBySource { get; set; }
        public Dictionary<string, decimal> RevenueByChannel { get; set; }
        public Dictionary<string, decimal> RevenueByCustomerSegment { get; set; }
        public List<RevenueTrendDto> RevenueTrends { get; set; }
        public List<RevenueSourcePerformanceDto> SourcePerformance { get; set; }
        public List<RevenueRiskDto> RevenueRisks { get; set; }
        public List<RevenueOpportunityDto> RevenueOpportunities { get; set; }
    }

    public class ExpenseAnalysisDto
    {
        public decimal TotalExpenses { get; set; }
        public decimal TargetExpenses { get; set; }
        public decimal ExpenseVariance { get; set; }
        public decimal GrowthRate { get; set; }
        public decimal MonthlyGrowthRate { get; set; }
        public decimal QuarterlyGrowthRate { get; set; }
        public decimal YearlyGrowthRate { get; set; }
        public Dictionary<string, decimal> ExpensesByCategory { get; set; }
        public Dictionary<string, decimal> ExpensesByDepartment { get; set; }
        public Dictionary<string, decimal> ExpensesBySupplier { get; set; }
        public List<ExpenseTrendDto> ExpenseTrends { get; set; }
        public List<ExpenseCategoryPerformanceDto> CategoryPerformance { get; set; }
        public List<ExpenseRiskDto> ExpenseRisks { get; set; }
        public List<ExpenseOptimizationDto> ExpenseOptimizations { get; set; }
    }



    public class RevenueTrendDto
    {
        public DateTime Date { get; set; }
        public decimal Value { get; set; }
        public decimal Target { get; set; }
        public decimal Variance { get; set; }
        public string Period { get; set; }
        public string Source { get; set; }
    }

    public class ExpenseTrendDto
    {
        public DateTime Date { get; set; }
        public decimal Value { get; set; }
        public decimal Target { get; set; }
        public decimal Variance { get; set; }
        public string Period { get; set; }
        public string Category { get; set; }
    }

    public class StrategyPerformanceTrendDto
    {
        public DateTime Date { get; set; }
        public decimal GoalCompletion { get; set; }
        public decimal ActionCompletion { get; set; }
        public decimal ImpactScore { get; set; }
        public string Period { get; set; }
        public string Category { get; set; }
    }

    public class RevenueSourcePerformanceDto
    {
        public string SourceId { get; set; }
        public string Name { get; set; }
        public decimal Revenue { get; set; }
        public decimal Target { get; set; }
        public decimal Variance { get; set; }
        public decimal GrowthRate { get; set; }
        public string Status { get; set; }
    }

    public class ExpenseCategoryPerformanceDto
    {
        public string CategoryId { get; set; }
        public string Name { get; set; }
        public decimal Expenses { get; set; }
        public decimal Target { get; set; }
        public decimal Variance { get; set; }
        public decimal GrowthRate { get; set; }
        public string Status { get; set; }
    }

    public class StrategyResourceDto
    {
        public string ResourceId { get; set; }
        public string Name { get; set; }
        public decimal Allocation { get; set; }
        public decimal Utilization { get; set; }
        public decimal Impact { get; set; }
        public string Status { get; set; }
    }

    public class RevenueRiskDto
    {
        public string Id { get; set; }
        public string Description { get; set; }
        public decimal Impact { get; set; }
        public decimal Likelihood { get; set; }
        public string Status { get; set; }
        public string Mitigation { get; set; }
    }

    public class ExpenseRiskDto
    {
        public string Id { get; set; }
        public string Description { get; set; }
        public decimal Impact { get; set; }
        public decimal Likelihood { get; set; }
        public string Status { get; set; }
        public string Mitigation { get; set; }
    }

    public class StrategyRiskDto
    {
        public string Id { get; set; }
        public string Description { get; set; }
        public decimal Impact { get; set; }
        public decimal Likelihood { get; set; }
        public string Status { get; set; }
        public string Mitigation { get; set; }
    }

    public class RevenueOpportunityDto
    {
        public string Id { get; set; }
        public string Description { get; set; }
        public decimal PotentialValue { get; set; }
        public decimal Probability { get; set; }
        public string Status { get; set; }
        public string ActionPlan { get; set; }
    }

    public class ExpenseOptimizationDto
    {
        public string Id { get; set; }
        public string Description { get; set; }
        public decimal PotentialSavings { get; set; }
        public decimal Probability { get; set; }
        public string Status { get; set; }
        public string ActionPlan { get; set; }
    }

    public class StrategyOpportunityDto
    {
        public string Id { get; set; }
        public string Description { get; set; }
        public decimal Impact { get; set; }
        public decimal Probability { get; set; }
        public string Status { get; set; }
        public string ActionPlan { get; set; }
    }
}
