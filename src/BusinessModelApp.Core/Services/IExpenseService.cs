using BusinessModelApp.Core.DTOs;
using BusinessModelApp.Core.DTOs.Expense;
using BusinessModelApp.Core.DTOs.Analytics;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BusinessModelApp.Core.Services
{
    public interface IExpenseService
    {
        Task<IEnumerable<ExpenseDto>> GetAllExpensesAsync();
        Task<ExpenseDto> GetExpenseByIdAsync(string id);
        Task<ExpenseDto> CreateExpenseAsync(ExpenseDto expense);
        Task UpdateExpenseAsync(ExpenseDto expense);
        Task DeleteExpenseAsync(string id);
        Task<IEnumerable<ExpenseMetricDto>> GetExpenseMetricsAsync();
        Task<ExpenseAnalysisDto> GetExpenseAnalysisAsync();
        Task<ExpenseTrendDto> GetExpenseTrendAsync();
        Task<IEnumerable<ExpenseCategoryDto>> GetExpenseCategoriesAsync();
    }
}
