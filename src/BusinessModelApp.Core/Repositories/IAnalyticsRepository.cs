using BusinessModelApp.Core.DTOs.Analytics;
using BusinessModelApp.Core.DTOs.Expense;
using BusinessModelApp.Core.DTOs.Revenue;
using BusinessModelApp.Core.DTOs.Strategy;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BusinessModelApp.Core.Repositories
{
    public interface IAnalyticsRepository
    {
        // Revenue Analytics
        Task<DTOs.Revenue.RevenueAnalysisDto> GetRevenueAnalysisAsync();
        Task<IEnumerable<DTOs.Revenue.RevenueTrendDto>> GetRevenueTrendsAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<DTOs.Revenue.RevenueSourcePerformanceDto>> GetRevenueSourcePerformanceAsync();
        Task<IEnumerable<DTOs.Revenue.RevenueRiskDto>> GetRevenueRisksAsync();
        Task<IEnumerable<DTOs.Revenue.RevenueOpportunityDto>> GetRevenueOpportunitiesAsync();
        Task<decimal> GetRevenueGrowthRateAsync(DateTime startDate, DateTime endDate);
        Task<decimal> GetRevenueVarianceAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<RevenueSourceDto>> GetTopRevenueSourcesAsync(int topN = 10);
        Task<IEnumerable<RevenueSourceDto>> GetLowestRevenueSourcesAsync(int bottomN = 10);

        // Expense Analytics
        Task<DTOs.Expense.ExpenseAnalysisDto> GetExpenseAnalysisAsync();
        Task<IEnumerable<DTOs.Expense.ExpenseTrendDto>> GetExpenseTrendsAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<DTOs.Expense.ExpenseCategoryDto>> GetExpenseCategoryPerformanceAsync();
        Task<IEnumerable<DTOs.Expense.ExpenseRiskDto>> GetExpenseRisksAsync();
        Task<IEnumerable<DTOs.Expense.ExpenseOptimizationDto>> GetExpenseOptimizationsAsync();
        Task<decimal> GetExpenseGrowthRateAsync(DateTime startDate, DateTime endDate);
        Task<decimal> GetExpenseVarianceAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<DTOs.Expense.ExpenseDto>> GetTopExpensesAsync(int topN = 10);
        Task<IEnumerable<DTOs.Expense.ExpenseDto>> GetLowestExpensesAsync(int bottomN = 10);

        // Strategy Analytics
        Task<DTOs.Strategy.StrategyAnalysisDto> GetStrategyAnalysisAsync();
        Task<IEnumerable<DTOs.Strategy.PerformanceTrendDto>> GetStrategyPerformanceTrendsAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<DTOs.Strategy.StrategyResourceDto>> GetStrategyResourceAllocationAsync();
        Task<IEnumerable<DTOs.Strategy.StrategyRiskDto>> GetStrategyRisksAsync();
        Task<IEnumerable<DTOs.Strategy.StrategyOpportunityDto>> GetStrategyOpportunitiesAsync();
        Task<decimal> GetStrategyImpactScoreAsync();
        Task<decimal> GetGoalCompletionRateAsync();
        Task<decimal> GetActionCompletionRateAsync();

        // Combined Analytics
        Task<FinancialPerformanceDto> GetFinancialPerformanceAsync();
        Task<BusinessHealthDto> GetBusinessHealthAsync();
        // Task<IEnumerable<FinancialMetricDto>> GetFinancialMetricsAsync();
        // Task<IEnumerable<BusinessKPIDto>> GetBusinessKPIsAsync();
        // Task<IEnumerable<RiskAssessmentDto>> GetRiskAssessmentsAsync();
        // Task<IEnumerable<OpportunityAnalysisDto>> GetOpportunityAnalysisAsync();
    }
}
