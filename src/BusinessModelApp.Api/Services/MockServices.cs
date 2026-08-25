using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain;
using BusinessModelApp.Core.Domain.Users;
using BusinessModelApp.Core.Dtos;
using BusinessModelApp.Core.DTOs;
using BusinessModelApp.Core.DTOs.Task;
using BusinessModelApp.Core.DTOs.Revenue;
using BusinessModelApp.Core.DTOs.Expense;
using BusinessModelApp.Core.DTOs.Strategy;
using BusinessModelApp.Core.DTOs.Analytics;
using BusinessModelApp.Core.DTOs.Agent;
using BusinessModelApp.Core.Interfaces;
using BusinessModelApp.Core.Repositories;
using BusinessModelApp.Core.Services;

namespace BusinessModelApp.Api.Services
{
    public class MockProductService : IProductService
    {
        public Task<Product> GetProductAsync(int id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new Product { Id = id, Name = "Mock Product", Price = 99.99m });
        }

        public Task<IEnumerable<Product>> GetFeaturedProductsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IEnumerable<Product>>(new[]
            {
                new Product { Id = 1, Name = "Product 1", Price = 10.00m },
                new Product { Id = 2, Name = "Product 2", Price = 20.00m }
            });
        }

        public Task<Product> UpdateProductPriceAsync(int id, decimal newPrice, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new Product { Id = id, Name = "Mock Product", Price = newPrice });
        }

        public Task InvalidateProductCacheAsync(int id, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<BusinessModelApp.Core.Services.CacheStats> GetCacheStatsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new BusinessModelApp.Core.Services.CacheStats());
        }
    }

    public class MockBusinessModelRepository : IBusinessModelRepository
    {
        public Task<IEnumerable<BusinessModelDto>> GetAllAsync()
        {
            return Task.FromResult<IEnumerable<BusinessModelDto>>(new List<BusinessModelDto>());
        }

        public Task<BusinessModelDto> GetByIdAsync(Guid id)
        {
            return Task.FromResult(new BusinessModelDto { Id = id, Name = "Mock Business Model" });
        }

        public Task<BusinessModelDto> AddAsync(BusinessModelDto businessModelDto)
        {
            businessModelDto.Id = Guid.NewGuid();
            return Task.FromResult(businessModelDto);
        }

        public Task<BusinessModelDto> UpdateAsync(BusinessModelDto businessModelDto)
        {
            return Task.FromResult(businessModelDto);
        }

        public Task DeleteAsync(Guid id)
        {
            return Task.CompletedTask;
        }
    }

    public class MockUserRepository : IUserRepository
    {
        public Task<User> GetUserByIdAsync(Guid id)
        {
            return Task.FromResult(new User { Id = id, UserName = "MockUser", Email = "mock@example.com" });
        }

        public Task<User> GetUserByEmailAsync(string email)
        {
            return Task.FromResult(new User { Id = Guid.NewGuid(), UserName = "MockUser", Email = email });
        }

        public Task<IEnumerable<User>> GetAllUsersAsync()
        {
            return Task.FromResult<IEnumerable<User>>(new[]
            {
                new User { Id = Guid.NewGuid(), UserName = "CEO", FirstName = "Chief", LastName = "Executive", Email = "ceo@company.com", Role = "CEO" },
                new User { Id = Guid.NewGuid(), UserName = "CFO", FirstName = "Chief", LastName = "Financial", Email = "cfo@company.com", Role = "CFO" },
                new User { Id = Guid.NewGuid(), UserName = "CTO", FirstName = "Chief", LastName = "Technology", Email = "cto@company.com", Role = "CTO" },
                new User { Id = Guid.NewGuid(), UserName = "CHRO", FirstName = "Chief", LastName = "HR", Email = "chro@company.com", Role = "CHRO" }
            });
        }

        public Task AddUserAsync(User user)
        {
            return Task.CompletedTask;
        }

        public Task UpdateUserAsync(User user)
        {
            return Task.CompletedTask;
        }

        public Task DeleteUserAsync(Guid id)
        {
            return Task.CompletedTask;
        }
    }

    public class MockRoleRepository : IRoleRepository
    {
        public Task<Role> GetRoleByIdAsync(Guid id)
        {
            return Task.FromResult(new Role("MockRole", "Description") { Id = id });
        }

        public Task<Role> GetRoleByNameAsync(string roleName)
        {
            return Task.FromResult(new Role(roleName, "Description") { Id = Guid.NewGuid() });
        }

        public Task<IEnumerable<Role>> GetAllRolesAsync()
        {
            return Task.FromResult<IEnumerable<Role>>(new[]
            {
                new Role("Admin", "Administrator Role") { Id = Guid.NewGuid() },
                new Role("User", "Standard User Role") { Id = Guid.NewGuid() }
            });
        }

        public Task AddRoleAsync(Role role)
        {
            role.Id = Guid.NewGuid();
            return Task.CompletedTask;
        }

        public Task UpdateRoleAsync(Role role)
        {
            return Task.CompletedTask;
        }

        public Task DeleteRoleAsync(Guid id)
        {
            return Task.CompletedTask;
        }
    }

    public class MockTaskRepository : BusinessModelApp.Core.Interfaces.ITaskRepository
    {
        private static readonly List<BusinessModelApp.Core.DTOs.Task.TaskDto> _tasks = new List<BusinessModelApp.Core.DTOs.Task.TaskDto>();
        private readonly IAIService _aiService;

        public MockTaskRepository(IAIService aiService)
        {
            _aiService = aiService;
        }

        public Task<BusinessModelApp.Core.DTOs.Task.TaskDto> GetTaskByIdAsync(Guid id)
        {
            var task = _tasks.FirstOrDefault(t => t.Id == id);
            return Task.FromResult(task ?? new BusinessModelApp.Core.DTOs.Task.TaskDto { Id = id, Title = "Mock Task" });
        }

        public Task<IEnumerable<BusinessModelApp.Core.DTOs.Task.TaskDto>> GetAllTasksAsync()
        {
            return Task.FromResult<IEnumerable<BusinessModelApp.Core.DTOs.Task.TaskDto>>(_tasks);
        }

        public Task<BusinessModelApp.Core.DTOs.Task.TaskDto> CreateTaskAsync(BusinessModelApp.Core.DTOs.Task.CreateTaskDto taskDto, int creatorUserId)
        {
            Console.WriteLine($"[MockTaskRepository] Creating task: Title='{taskDto.Title}', Description='{taskDto.Description}', DueDate='{taskDto.DueDate}'");
            var newTask = new BusinessModelApp.Core.DTOs.Task.TaskDto 
            { 
                Id = Guid.NewGuid(), 
                Title = taskDto.Title,
                Description = taskDto.Description,
                Status = "Pending",
                AssignedToUserId = taskDto.AssignedToUserId,
                AssignedToUserName = "Assigned Agent", // Ideally lookup user name, but mock is fine 
                CreatedAt = DateTime.UtcNow
            };
            _tasks.Add(newTask);
            Console.WriteLine($"[MockTaskRepository] Task created with ID: {newTask.Id}");

            // Simulate agent picking up the task
            _ = SimulateAgentWork(newTask);

            return Task.FromResult(newTask);
        }

        public Task UpdateTaskStatusAsync(Guid id, string status)
        {
            var task = _tasks.FirstOrDefault(t => t.Id == id);
            if (task != null)
            {
                task.Status = status;
                Console.WriteLine($"[MockTaskRepository] Updated task {id} status to {status}");
            }
            return Task.CompletedTask;
        }

        private async Task SimulateAgentWork(BusinessModelApp.Core.DTOs.Task.TaskDto task)
        {
            var agentName = "Agent Smith"; // Mock agent name
            var agentId = Guid.NewGuid();

            // Wait a bit before starting
            await Task.Delay(3000);

            // 1. Agent picks up the task
            task.Status = "In Progress";
            task.AssignedToUserName = agentName;
            MockAgentService.LogActivityStatic(agentId, agentName, "TaskStarted", $"Started working on task: {task.Title}", $"Task ID: {task.Id}");
            Console.WriteLine($"[Simulation] {agentName} started task {task.Id}");

            // Simulate work duration
            await Task.Delay(5000);

            // 2. Perform business logic using Real AI
            string resultDescription = "Task completed successfully.";
            
            try 
            {
                var prompt = $"You are an autonomous AI agent named {agentName}. Your task is: '{task.Title}'. Description: '{task.Description}'. \n" +
                             "Perform this task and provide a brief summary of the result (max 1 sentence). \n" +
                             "If the task involves generating revenue, state the amount in the format 'REVENUE: <amount>'. \n" +
                             "If the task involves an expense, state the amount in the format 'EXPENSE: <amount>'.";
                
                var aiResponse = await _aiService.GetCompletionAsync(prompt);
                resultDescription = aiResponse;

                // Simple parsing for revenue/expense
                if (aiResponse.Contains("REVENUE:"))
                {
                    var parts = aiResponse.Split("REVENUE:");
                    if (parts.Length > 1 && decimal.TryParse(parts[1].Trim().Split(' ')[0], out decimal amount))
                    {
                        MockRevenueService.AddRevenueStatic($"Revenue from {task.Title}", amount);
                        MockAgentService.LogActivityStatic(agentId, agentName, "RevenueGenerated", $"Generated ${amount:N0}", $"Source: {task.Title}");
                    }
                }
                else if (aiResponse.Contains("EXPENSE:"))
                {
                    var parts = aiResponse.Split("EXPENSE:");
                    if (parts.Length > 1 && decimal.TryParse(parts[1].Trim().Split(' ')[0], out decimal amount))
                    {
                        MockExpenseService.AddExpenseStatic($"Expense for {task.Title}", amount);
                        MockAgentService.LogActivityStatic(agentId, agentName, "ExpenseRecorded", $"Recorded ${amount:N0}", $"Category: {task.Title}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Simulation] AI Error: {ex.Message}");
                resultDescription = "Task completed (AI generation failed, using fallback).";
            }

            // 3. Agent completes the task
            task.Status = "Completed";
            MockAgentService.LogActivityStatic(agentId, agentName, "TaskCompleted", $"Completed task: {task.Title}", resultDescription);
            Console.WriteLine($"[Simulation] {agentName} completed task {task.Id}");
        }
    }

    public class MockExpenseService : IExpenseService
    {
        private static readonly List<ExpenseDto> _expenses = new List<ExpenseDto>
        {
            new ExpenseDto { Id = Guid.NewGuid().ToString(), Name = "Operational Costs", Amount = 50000, Category = "Operations" }
        };

        public static void AddExpenseStatic(string name, decimal amount)
        {
            _expenses.Add(new ExpenseDto 
            { 
                Id = Guid.NewGuid().ToString(), 
                Name = name, 
                Amount = amount, 
                Category = "Miscellaneous"
            });
        }

        public Task<IEnumerable<ExpenseDto>> GetAllExpensesAsync()
        {
            return Task.FromResult<IEnumerable<ExpenseDto>>(_expenses);
        }

        public Task<ExpenseDto> GetExpenseByIdAsync(string id)
        {
            return Task.FromResult(_expenses.FirstOrDefault(e => e.Id == id));
        }

        public Task<ExpenseDto> CreateExpenseAsync(ExpenseDto expense)
        {
            expense.Id = Guid.NewGuid().ToString();
            _expenses.Add(expense);
            return Task.FromResult(expense);
        }

        public Task UpdateExpenseAsync(ExpenseDto expense)
        {
            var existing = _expenses.FirstOrDefault(e => e.Id == expense.Id);
            if (existing != null)
            {
                existing.Name = expense.Name;
                existing.Amount = expense.Amount;
            }
            return Task.CompletedTask;
        }

        public Task DeleteExpenseAsync(string id)
        {
            var item = _expenses.FirstOrDefault(e => e.Id == id);
            if (item != null) _expenses.Remove(item);
            return Task.CompletedTask;
        }

        public Task<IEnumerable<ExpenseMetricDto>> GetExpenseMetricsAsync()
        {
            try
            {
                var totalExpenses = _expenses.Sum(e => e.Amount);
                var metrics = new List<ExpenseMetricDto>
                {
                    new ExpenseMetricDto { MetricName = "Total Expenses", Value = (double)totalExpenses, Unit = "USD", Trend = "-5%" },
                    new ExpenseMetricDto { MetricName = "Expense Count", Value = _expenses.Count, Unit = "Count", Trend = "+2" }
                };
                return Task.FromResult<IEnumerable<ExpenseMetricDto>>(metrics);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MockExpenseService] Error: {ex}");
                throw;
            }
        }

        public Task<ExpenseAnalysisDto> GetExpenseAnalysisAsync()
        {
            return Task.FromResult(new ExpenseAnalysisDto());
        }

        public Task<ExpenseTrendDto> GetExpenseTrendAsync()
        {
            return Task.FromResult(new ExpenseTrendDto());
        }

        public Task<IEnumerable<ExpenseCategoryDto>> GetExpenseCategoriesAsync()
        {
            return Task.FromResult<IEnumerable<ExpenseCategoryDto>>(new List<ExpenseCategoryDto>());
        }
    }

    public class MockRevenueService : IRevenueService
    {
        private static readonly List<RevenueSourceDto> _revenueSources = new List<RevenueSourceDto>
        {
            new RevenueSourceDto { Id = Guid.NewGuid().ToString(), Name = "Product Sales", Amount = 1200000, Type = "Recurring" }
        };

        public static void AddRevenueStatic(string name, decimal amount)
        {
            _revenueSources.Add(new RevenueSourceDto 
            { 
                Id = Guid.NewGuid().ToString(), 
                Name = name, 
                Amount = amount, 
                Type = "One-time",
                GrowthRate = 0.05
            });
        }

        public Task<IEnumerable<RevenueSourceDto>> GetAllRevenueSourcesAsync()
        {
            return Task.FromResult<IEnumerable<RevenueSourceDto>>(_revenueSources);
        }

        public Task<RevenueSourceDto> GetRevenueSourceByIdAsync(string id)
        {
            return Task.FromResult(_revenueSources.FirstOrDefault(r => r.Id == id));
        }

        public Task<RevenueSourceDto> CreateRevenueSourceAsync(RevenueSourceDto source)
        {
            source.Id = Guid.NewGuid().ToString();
            _revenueSources.Add(source);
            return Task.FromResult(source);
        }

        public Task UpdateRevenueSourceAsync(RevenueSourceDto source)
        {
            var existing = _revenueSources.FirstOrDefault(r => r.Id == source.Id);
            if (existing != null)
            {
                existing.Name = source.Name;
                existing.Amount = source.Amount;
            }
            return Task.CompletedTask;
        }

        public Task DeleteRevenueSourceAsync(string id)
        {
            var item = _revenueSources.FirstOrDefault(r => r.Id == id);
            if (item != null) _revenueSources.Remove(item);
            return Task.CompletedTask;
        }

        public Task<IEnumerable<RevenueMetricDto>> GetRevenueMetricsAsync()
        {
            try
            {
                var totalRevenue = _revenueSources.Sum(r => r.Amount);
                var metrics = new List<RevenueMetricDto>
                {
                    new RevenueMetricDto { MetricName = "Total Revenue", Value = (double)totalRevenue, Unit = "USD", Trend = "+12%" },
                    new RevenueMetricDto { MetricName = "Active Sources", Value = _revenueSources.Count, Unit = "Count", Trend = "+1" }
                };
                return Task.FromResult<IEnumerable<RevenueMetricDto>>(metrics);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MockRevenueService] Error: {ex}");
                throw;
            }
        }

        public Task<RevenuePerformanceDto> GetRevenuePerformanceAsync()
        {
            return Task.FromResult(new RevenuePerformanceDto());
        }

        public Task<BusinessModelApp.Core.DTOs.Revenue.RevenueAnalysisDto> GetRevenueAnalysisAsync()
        {
            return Task.FromResult(new BusinessModelApp.Core.DTOs.Revenue.RevenueAnalysisDto());
        }
    }

    public class MockStrategyService : IStrategyService
    {
        public Task<IEnumerable<BusinessStrategyDto>> GetAllStrategiesAsync()
        {
            return Task.FromResult<IEnumerable<BusinessStrategyDto>>(new List<BusinessStrategyDto>());
        }

        public Task<BusinessStrategyDto> GetStrategyByIdAsync(string id)
        {
            return Task.FromResult(new BusinessStrategyDto { Id = id, Name = "Mock Strategy" });
        }

        public Task<BusinessStrategyDto> CreateStrategyAsync(BusinessStrategyDto strategy)
        {
            strategy.Id = Guid.NewGuid().ToString();
            return Task.FromResult(strategy);
        }

        public Task UpdateStrategyAsync(BusinessStrategyDto strategy)
        {
            return Task.CompletedTask;
        }

        public Task DeleteStrategyAsync(string id)
        {
            return Task.CompletedTask;
        }

        public Task<IEnumerable<StrategyGoalDto>> GetGoalsAsync()
        {
            return Task.FromResult<IEnumerable<StrategyGoalDto>>(new List<StrategyGoalDto>());
        }

        public Task<IEnumerable<StrategyActionDto>> GetActionsAsync()
        {
            return Task.FromResult<IEnumerable<StrategyActionDto>>(new List<StrategyActionDto>());
        }

        public Task<IEnumerable<StrategyMetricDto>> GetMetricsAsync()
        {
            return Task.FromResult<IEnumerable<StrategyMetricDto>>(new List<StrategyMetricDto>());
        }

        public Task<StrategyPerformanceDto> GetPerformanceAsync()
        {
            return Task.FromResult(new StrategyPerformanceDto());
        }

        public Task<BusinessModelApp.Core.DTOs.Strategy.StrategyAnalysisDto> GetAnalysisAsync()
        {
            return Task.FromResult(new BusinessModelApp.Core.DTOs.Strategy.StrategyAnalysisDto());
        }
    }

    public class MockAgentService : IAgentService
    {
        public Task<BusinessModelApp.Core.DTOs.Agent.AgentDto> GetAgentAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new BusinessModelApp.Core.DTOs.Agent.AgentDto { Id = id, Name = "Mock Agent" });
        }

        public Task<IEnumerable<BusinessModelApp.Core.DTOs.Agent.AgentDto>> GetAllAgentsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IEnumerable<BusinessModelApp.Core.DTOs.Agent.AgentDto>>(new List<BusinessModelApp.Core.DTOs.Agent.AgentDto>());
        }

        public Task<BusinessModelApp.Core.DTOs.Agent.AgentDto> CreateAgentAsync(BusinessModelApp.Core.DTOs.Agent.CreateAgentDto createDto, CancellationToken cancellationToken = default)
        {
            var agentId = Guid.NewGuid();
            LogActivity(agentId, createDto.Name, "AgentCreated", $"Agent '{createDto.Name}' created.");
            return Task.FromResult(new BusinessModelApp.Core.DTOs.Agent.AgentDto { Id = agentId, Name = createDto.Name });
        }

        public Task<BusinessModelApp.Core.DTOs.Agent.AgentDto> UpdateAgentAsync(Guid id, BusinessModelApp.Core.DTOs.Agent.AgentUpdateDto updateDto, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new BusinessModelApp.Core.DTOs.Agent.AgentDto { Id = id, Name = "Updated Agent" });
        }

        public Task<bool> DeleteAgentAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        public Task<IEnumerable<BusinessModelApp.Core.DTOs.Agent.AgentDto>> GetAgentsByDepartmentAsync(string department, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IEnumerable<BusinessModelApp.Core.DTOs.Agent.AgentDto>>(new List<BusinessModelApp.Core.DTOs.Agent.AgentDto>());
        }

        public Task<IEnumerable<BusinessModelApp.Core.DTOs.Agent.AgentDto>> GetAvailableAgentsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IEnumerable<BusinessModelApp.Core.DTOs.Agent.AgentDto>>(new List<BusinessModelApp.Core.DTOs.Agent.AgentDto>());
        }

        public Task<IEnumerable<BusinessModelApp.Core.DTOs.Agent.AgentDto>> GetAgentsBySkillAsync(string skill, int minimumLevel, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IEnumerable<BusinessModelApp.Core.DTOs.Agent.AgentDto>>(new List<BusinessModelApp.Core.DTOs.Agent.AgentDto>());
        }

        public Task<BusinessModelApp.Core.DTOs.Agent.AgentMonitoringDto> GetAgentMonitoringDataAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new BusinessModelApp.Core.DTOs.Agent.AgentMonitoringDto());
        }

        public Task<IDictionary<string, double>> GetAgentMetricsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IDictionary<string, double>>(new Dictionary<string, double>());
        }

        public Task<bool> UpdateAgentMetricsAsync(Guid id, IDictionary<string, double> metrics, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        public Task<bool> UpdateAgentStatusAsync(Guid id, string status, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        public Task<bool> UpdateAgentAvailabilityAsync(Guid id, BusinessModelApp.Core.DTOs.Agent.AgentAvailabilityDto availability, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        public Task<bool> UpdateAgentSkillsAsync(Guid id, IEnumerable<BusinessModelApp.Core.DTOs.Agent.AgentSkillDto> skills, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        public Task<IEnumerable<BusinessModelApp.Core.DTOs.Agent.AgentSkillDto>> GetAgentSkillsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IEnumerable<BusinessModelApp.Core.DTOs.Agent.AgentSkillDto>>(new List<BusinessModelApp.Core.DTOs.Agent.AgentSkillDto>());
        }

        public Task StartMonitoringAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task StopMonitoringAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        private static readonly List<BusinessModelApp.Core.DTOs.Agent.AgentActivityDto> _activities = new List<BusinessModelApp.Core.DTOs.Agent.AgentActivityDto>();
        private readonly HashSet<Guid> _monitoredAgents = new HashSet<Guid>();

        public Task<bool> IsBeingMonitoredAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_monitoredAgents.Contains(id));
        }

        public Task<IEnumerable<BusinessModelApp.Core.DTOs.Agent.AgentActivityDto>> GetAgentActivitiesAsync(int limit = 50, CancellationToken cancellationToken = default)
        {
            var activities = _activities.OrderByDescending(a => a.Timestamp).Take(limit).ToList();
            return Task.FromResult<IEnumerable<BusinessModelApp.Core.DTOs.Agent.AgentActivityDto>>(activities);
        }

        private void LogActivity(Guid agentId, string agentName, string activityType, string description, string details = "")
        {
            LogActivityStatic(agentId, agentName, activityType, description, details);
        }

        public static void LogActivityStatic(Guid agentId, string agentName, string activityType, string description, string details = "")
        {
            var activity = new BusinessModelApp.Core.DTOs.Agent.AgentActivityDto
            {
                Id = Guid.NewGuid(),
                AgentId = agentId,
                AgentName = agentName,
                ActivityType = activityType,
                Description = description,
                Details = details,
                Timestamp = DateTime.UtcNow
            };
            _activities.Add(activity);
            Console.WriteLine($"[AgentActivity] {agentName}: {description}");
        }
    }

    public class MockRecommendationService : IRecommendationService
    {
        public Task<IEnumerable<Recommendation>> GetAgentRecommendationsAsync(string agentId)
        {
            return Task.FromResult<IEnumerable<Recommendation>>(new List<Recommendation>());
        }

        public Task<IEnumerable<Recommendation>> GetExecutiveRecommendationsAsync(string executiveId)
        {
            return Task.FromResult<IEnumerable<Recommendation>>(new List<Recommendation>());
        }

        public Task LogFeedbackAsync(string recommendationId, bool wasHelpful, string feedback = null)
        {
            return Task.CompletedTask;
        }

        public Task TrainOnNewDataAsync(IEnumerable<TrainingExample> examples)
        {
            return Task.CompletedTask;
        }
    }
}
