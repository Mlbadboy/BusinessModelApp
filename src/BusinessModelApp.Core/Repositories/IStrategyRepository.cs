using BusinessModelApp.Core.DTOs;
using BusinessModelApp.Core.DTOs.Strategy;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BusinessModelApp.Core.Repositories
{
    public interface IStrategyRepository : IRepository<BusinessStrategyDto>
    {
        Task<IEnumerable<BusinessStrategyDto>> GetStrategiesByCategoryAsync(string category);
        Task<IEnumerable<BusinessStrategyDto>> GetStrategiesByStatusAsync(string status);
        Task<IEnumerable<BusinessStrategyDto>> GetStrategiesByPriorityAsync(string priority);
        Task<IEnumerable<BusinessStrategyDto>> GetStrategiesByPeriodAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<StrategyGoalDto>> GetGoalsByStatusAsync(string status);
        Task<IEnumerable<StrategyActionDto>> GetActionsByStatusAsync(string status);
        Task<IEnumerable<StrategyActionDto>> GetActionsByAssignedToAsync(string assignedTo);
        Task<IEnumerable<StrategyActionDto>> GetOverdueActionsAsync();
        Task<IEnumerable<StrategyGoalDto>> GetIncompleteGoalsAsync();
        Task<IEnumerable<StrategyActionDto>> GetDelayedActionsAsync();
        Task<IEnumerable<BusinessStrategyDto>> GetStrategiesByImpactAsync();
    }
}
