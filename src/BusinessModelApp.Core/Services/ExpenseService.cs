using BusinessModelApp.Core.DTOs.Expense;
using BusinessModelApp.Core.DTOs.Analytics;
using BusinessModelApp.Core.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace BusinessModelApp.Core.Services
{
    public class ExpenseService : IExpenseService
    {
        // TODO: Inject IExpenseRepository when available
        private readonly List<ExpenseDto> _expenses = new List<ExpenseDto>
        {
            new ExpenseDto { Id = "1", Description = "Server Costs", Amount = 5000, Category = "Infrastructure" },
            new ExpenseDto { Id = "2", Description = "Salaries", Amount = 40000, Category = "Personnel" }
        };

        public Task<IEnumerable<ExpenseDto>> GetAllExpensesAsync()
        {
            return Task.FromResult((IEnumerable<ExpenseDto>)_expenses);
        }

        public Task<ExpenseDto> GetExpenseByIdAsync(string id)
        {
            var expense = _expenses.FirstOrDefault(e => e.Id == id);
            return Task.FromResult(expense);
        }

        public Task<ExpenseDto> CreateExpenseAsync(ExpenseDto expense)
        {
            expense.Id = Guid.NewGuid().ToString();
            _expenses.Add(expense);
            return Task.FromResult(expense);
        }

        public Task UpdateExpenseAsync(ExpenseDto expense)
        {
            var existing = _expenses.FirstOrDefault(x => x.Id == expense.Id);
            if (existing != null)
            {
                _expenses.Remove(existing);
                _expenses.Add(expense);
            }
            return Task.CompletedTask;
        }

        public Task DeleteExpenseAsync(string id)
        {
            var expense = _expenses.FirstOrDefault(e => e.Id == id);
            if (expense != null)
            {
                _expenses.Remove(expense);
            }
            return Task.CompletedTask;
        }

        public Task<IEnumerable<ExpenseMetricDto>> GetExpenseMetricsAsync()
        {
            var metrics = new List<ExpenseMetricDto>
            {
                new ExpenseMetricDto { MetricName = "Total Expenses", Value = 45000, Unit = "USD", Trend = "-2%" },
                new ExpenseMetricDto { MetricName = "Average Expense", Value = 22500, Unit = "USD", Trend = "-1%" },
                new ExpenseMetricDto { MetricName = "Expense Count", Value = 2, Unit = "Count", Trend = "0" }
            };
            return Task.FromResult((IEnumerable<ExpenseMetricDto>)metrics);
        }

        public Task<ExpenseAnalysisDto> GetExpenseAnalysisAsync()
        {
            return Task.FromResult(new ExpenseAnalysisDto
            {
                TotalExpenses = 45000,
                PotentialSavings = 5000
            });
        }

        public Task<ExpenseTrendDto> GetExpenseTrendAsync()
        {
            return Task.FromResult(new ExpenseTrendDto
            {
                Name = "Monthly Expense Trend",
                TrendDirection = "Stable",
                ChangePercentage = 2.5m
            });
        }

        public Task<IEnumerable<ExpenseCategoryDto>> GetExpenseCategoriesAsync()
        {
            var categories = _expenses.Select(x => x.Category).Distinct().Select(c => new ExpenseCategoryDto { Name = c }).ToList();
            return Task.FromResult((IEnumerable<ExpenseCategoryDto>)categories);
        }
    }
}
