using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.DTOs.Agent;

namespace BusinessModelApp.Core.Services
{
    public interface IAgentService
    {
        // Core operations
        Task<AgentDto> GetAgentAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IEnumerable<AgentDto>> GetAllAgentsAsync(CancellationToken cancellationToken = default);
        Task<AgentDto> CreateAgentAsync(CreateAgentDto createDto, CancellationToken cancellationToken = default);
        Task<AgentDto> UpdateAgentAsync(Guid id, AgentUpdateDto updateDto, CancellationToken cancellationToken = default);
        Task<bool> DeleteAgentAsync(Guid id, CancellationToken cancellationToken = default);

        // Specialized queries
        Task<IEnumerable<AgentDto>> GetAgentsByDepartmentAsync(string department, CancellationToken cancellationToken = default);
        Task<IEnumerable<AgentDto>> GetAvailableAgentsAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<AgentDto>> GetAgentsBySkillAsync(string skill, int minimumLevel, CancellationToken cancellationToken = default);

        // Monitoring and metrics
        Task<AgentMonitoringDto> GetAgentMonitoringDataAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IDictionary<string, double>> GetAgentMetricsAsync(Guid id, CancellationToken cancellationToken = default);
        Task<bool> UpdateAgentMetricsAsync(Guid id, IDictionary<string, double> metrics, CancellationToken cancellationToken = default);

        // Status and availability management
        Task<bool> UpdateAgentStatusAsync(Guid id, string status, CancellationToken cancellationToken = default);
        Task<bool> UpdateAgentAvailabilityAsync(Guid id, AgentAvailabilityDto availability, CancellationToken cancellationToken = default);
        
        // Skills management
        Task<bool> UpdateAgentSkillsAsync(Guid id, IEnumerable<AgentSkillDto> skills, CancellationToken cancellationToken = default);
        Task<IEnumerable<AgentSkillDto>> GetAgentSkillsAsync(Guid id, CancellationToken cancellationToken = default);

        // Real-time monitoring
        Task StartMonitoringAsync(Guid id, CancellationToken cancellationToken = default);
        Task StopMonitoringAsync(Guid id, CancellationToken cancellationToken = default);
        Task<bool> IsBeingMonitoredAsync(Guid id, CancellationToken cancellationToken = default);

        // Activity Logging
        Task<IEnumerable<AgentActivityDto>> GetAgentActivitiesAsync(int limit = 50, CancellationToken cancellationToken = default);
    }
}
