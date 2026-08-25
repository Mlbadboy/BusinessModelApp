using BusinessModelApp.Core.DTOs.Strategy;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BusinessModelApp.Core.Services
{
    public interface IStrategyService
    {
        Task<IEnumerable<BusinessStrategyDto>> GetAllStrategiesAsync();
        Task<BusinessStrategyDto> GetStrategyByIdAsync(string id);
        Task<BusinessStrategyDto> CreateStrategyAsync(BusinessStrategyDto strategy);
        Task UpdateStrategyAsync(BusinessStrategyDto strategy);
        Task DeleteStrategyAsync(string id);
        Task<IEnumerable<StrategyGoalDto>> GetGoalsAsync();
        Task<IEnumerable<StrategyActionDto>> GetActionsAsync();
        Task<IEnumerable<StrategyMetricDto>> GetMetricsAsync();
        Task<StrategyPerformanceDto> GetPerformanceAsync();
        Task<DTOs.Strategy.StrategyAnalysisDto> GetAnalysisAsync();
    }
}
