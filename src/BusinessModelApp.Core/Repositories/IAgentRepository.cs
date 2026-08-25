using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Agent;

namespace BusinessModelApp.Core.Repositories
{
    public interface IAgentRepository
    {
        Task<Agent> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IEnumerable<Agent>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<Agent>> GetByDepartmentAsync(string department, CancellationToken cancellationToken = default);
        Task<IEnumerable<Agent>> GetAvailableAgentsAsync(CancellationToken cancellationToken = default);
        Task<Agent> CreateAsync(Agent agent, CancellationToken cancellationToken = default);
        Task<Agent> UpdateAsync(Agent agent, CancellationToken cancellationToken = default);
        Task<bool> UpdateStatusAsync(Guid id, string status, CancellationToken cancellationToken = default);
        Task<bool> UpdateAvailabilityAsync(Guid id, AgentAvailability availability, CancellationToken cancellationToken = default);
        Task<bool> UpdateMetricsAsync(Guid id, Dictionary<string, double> metrics, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IEnumerable<Agent>> GetBySkillLevelAsync(string skill, int minimumLevel, CancellationToken cancellationToken = default);
        Task<bool> UpdateSkillLevelsAsync(Guid id, Dictionary<string, int> skillLevels, CancellationToken cancellationToken = default);
        Task<Dictionary<string, double>> GetPerformanceMetricsAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
