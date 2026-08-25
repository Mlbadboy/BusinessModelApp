using BusinessModelApp.Core.DTOs;
using BusinessModelApp.Core.DTOs.Expense;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BusinessModelApp.Core.Repositories
{
    public interface IExpenseRepository : IRepository<ExpenseDto>
    {
        Task<IEnumerable<ExpenseDto>> GetExpensesByCategoryAsync(string category);
        Task<IEnumerable<ExpenseDto>> GetExpensesByStatusAsync(string status);
        Task<IEnumerable<ExpenseDto>> GetExpensesByPeriodAsync(DateTime startDate, DateTime endDate);
        Task<decimal> GetTotalExpensesAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<decimal> GetTargetExpensesAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<IEnumerable<ExpenseMetricDto>> GetExpenseMetricsByPeriodAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<ExpenseDto>> GetTopExpensesAsync(int topN = 10);
        Task<IEnumerable<ExpenseDto>> GetLowestExpensesAsync(int bottomN = 10);
        Task<IEnumerable<ExpenseDto>> GetExpensesByGrowthRateAsync();
        Task<IEnumerable<ExpenseDto>> GetExpensesByDepartmentAsync();
        Task<IEnumerable<ExpenseDto>> GetExpensesBySupplierAsync();
    }
}
