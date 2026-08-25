using BusinessModelApp.Core.DTOs.Expense;
using BusinessModelApp.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BusinessModelApp.Api.Controllers
{
    [ApiController]
    [Route("api/business/expense")]
    // [Authorize]
    public class ExpenseController : ControllerBase
    {
        private readonly IExpenseService _expenseService;
        public ExpenseController(IExpenseService expenseService)
        {
            _expenseService = expenseService;
        }

        // GET: api/business/expense/list
        [HttpGet("list")]
        // [Authorize(Roles = "CEO,COO,CFO,CBO")]
        public async Task<ActionResult<IEnumerable<ExpenseDto>>> GetExpenses()
        {
            var expenses = await _expenseService.GetAllExpensesAsync();
            return Ok(expenses);
        }

        // GET: api/business/expense/{id}
        [HttpGet("{id}")]
        // [Authorize(Roles = "CEO,COO,CFO,CBO")]
        public async Task<ActionResult<ExpenseDto>> GetExpense(string id)
        {
            var expense = await _expenseService.GetExpenseByIdAsync(id);
            if (expense == null)
            {
                return NotFound();
            }
            return Ok(expense);
        }

        // POST: api/business/expense
        [HttpPost]
        // [Authorize(Roles = "CEO,COO,CFO,CBO")]
        public async Task<ActionResult<ExpenseDto>> CreateExpense(ExpenseDto expense)
        {
            var result = await _expenseService.CreateExpenseAsync(expense);
            return CreatedAtAction(nameof(GetExpense), new { id = result.Id }, result);
        }

        // PUT: api/business/expense/{id}
        [HttpPut("{id}")]
        // [Authorize(Roles = "CEO,COO,CFO,CBO")]
        public async Task<IActionResult> UpdateExpense(string id, ExpenseDto expense)
        {
            if (id != expense.Id)
            {
                return BadRequest();
            }

            await _expenseService.UpdateExpenseAsync(expense);
            return NoContent();
        }

        // DELETE: api/business/expense/{id}
        [HttpDelete("{id}")]
        // [Authorize(Roles = "CEO,COO,CFO,CBO")]
        public async Task<IActionResult> DeleteExpense(string id)
        {
            await _expenseService.DeleteExpenseAsync(id);
            return NoContent();
        }

        // GET: api/business/expense/metrics
        [HttpGet("metrics")]
        // [Authorize(Roles = "CEO,COO,CFO,CBO")]
        public async Task<ActionResult<IEnumerable<ExpenseMetricDto>>> GetExpenseMetrics()
        {
            try
            {
                var metrics = await _expenseService.GetExpenseMetricsAsync();
                return Ok(metrics);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ExpenseController] Error: {ex}");
                return StatusCode(500, ex.Message);
            }
        }

        // GET: api/business/expense/analysis
        [HttpGet("analysis")]
        // [Authorize(Roles = "CEO,COO,CFO,CBO")]
        public async Task<ActionResult<ExpenseAnalysisDto>> GetExpenseAnalysis()
        {
            var analysis = await _expenseService.GetExpenseAnalysisAsync();
            return Ok(analysis);
        }

        // GET: api/business/expense/trend
        [HttpGet("trend")]
        // [Authorize(Roles = "CEO,COO,CFO,CBO")]
        public async Task<ActionResult<ExpenseTrendDto>> GetExpenseTrend()
        {
            var trend = await _expenseService.GetExpenseTrendAsync();
            return Ok(trend);
        }

        // GET: api/business/expense/category
        [HttpGet("category")]
        // [Authorize(Roles = "CEO,COO,CFO,CBO")]
        public async Task<ActionResult<IEnumerable<ExpenseCategoryDto>>> GetExpenseCategories()
        {
            var categories = await _expenseService.GetExpenseCategoriesAsync();
            return Ok(categories);
        }
    }
}
