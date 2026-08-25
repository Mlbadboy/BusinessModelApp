using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.DTOs.Agent;
using BusinessModelApp.Core.Domain.Agent;
using BusinessModelApp.Core.Interfaces;
using BusinessModelApp.Core.Repositories;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace BusinessModelApp.Core.Services
{
    public class AgentService : IAgentService
    {
        private readonly IAgentRepository _repository;
        private readonly ICacheService _cache;
        private readonly ILogger<AgentService> _logger;
        private readonly HashSet<Guid> _monitoredAgents;
        private const string CacheKeyPrefix = "agent:";
        private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(15);

        public AgentService(
            IAgentRepository repository,
            ICacheService cache,
            ILogger<AgentService> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _monitoredAgents = new HashSet<Guid>();
        }

        public async Task<AgentDto> GetAgentAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var cacheKey = $"{CacheKeyPrefix}{id}";
            var cachedAgent = await _cache.GetAsync<AgentDto>(cacheKey, cancellationToken);
            
            if (cachedAgent != null)
            {
                _logger.LogDebug("Cache hit for agent {AgentId}", id);
                return cachedAgent;
            }

            var agent = await _repository.GetByIdAsync(id, cancellationToken);
            if (agent == null) return null;

            var agentDto = MapToDto(agent);
            await CacheAgentAsync(agentDto, cancellationToken);

            return agentDto;
        }

        public async Task<IEnumerable<AgentDto>> GetAllAgentsAsync(CancellationToken cancellationToken = default)
        {
            var cacheKey = $"{CacheKeyPrefix}all";
            var cachedAgents = await _cache.GetAsync<IEnumerable<AgentDto>>(cacheKey, cancellationToken);
            
            if (cachedAgents != null)
            {
                _logger.LogDebug("Cache hit for all agents");
                return cachedAgents;
            }

            var agents = await _repository.GetAllAsync(cancellationToken);
            var agentDtos = agents.Select(MapToDto).ToList();

            await CacheAgentsListAsync(agentDtos, cancellationToken);
            return agentDtos;
        }

        public async Task<AgentDto> CreateAgentAsync(CreateAgentDto createDto, CancellationToken cancellationToken = default)
        {
            var agent = new Agent
            {
                Id = Guid.NewGuid(),
                Name = createDto.Name,
                Department = createDto.Department,
                SkillLevels = createDto.InitialSkills?.ToDictionary(k => k.Key, v => v.Value) 
                    ?? new Dictionary<string, int>(),
                IsOnline = false,
                LastActive = DateTime.UtcNow,
                Status = "Available",
                Availability = new AgentAvailability
                {
                    IsAvailable = true,
                    WorkloadPercentage = 0
                }
            };

            var createdAgent = await _repository.CreateAsync(agent, cancellationToken);
            var agentDto = MapToDto(createdAgent);
            await CacheAgentAsync(agentDto, cancellationToken);
            await InvalidateAgentListCacheAsync(cancellationToken);

            return agentDto;
        }

        public async Task<AgentDto> UpdateAgentAsync(Guid id, AgentUpdateDto updateDto, CancellationToken cancellationToken = default)
        {
            var agent = await _repository.GetByIdAsync(id, cancellationToken);
            if (agent == null) return null;

            // Update agent properties
            agent.Status = updateDto.Status ?? agent.Status;
            if (updateDto.Metrics != null)
            {
                foreach (var metric in updateDto.Metrics)
                {
                    agent.PerformanceMetrics[metric.Key] = metric.Value;
                }
            }
            if (updateDto.Availability != null)
            {
                agent.Availability = new AgentAvailability
                {
                    IsAvailable = updateDto.Availability.IsAvailable,
                    NextAvailableTime = updateDto.Availability.NextAvailableTime != null 
                        ? DateTime.Parse(updateDto.Availability.NextAvailableTime) 
                        : null,
                    CurrentActivity = updateDto.Availability.CurrentActivity,
                    WorkloadPercentage = updateDto.Availability.WorkloadPercentage
                };
            }

            var updatedAgent = await _repository.UpdateAsync(agent, cancellationToken);
            var agentDto = MapToDto(updatedAgent);
            await CacheAgentAsync(agentDto, cancellationToken);
            await InvalidateAgentListCacheAsync(cancellationToken);

            return agentDto;
        }

        public async Task<bool> DeleteAgentAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var result = await _repository.DeleteAsync(id, cancellationToken);
            if (result)
            {
                await InvalidateAgentCacheAsync(id, cancellationToken);
                await InvalidateAgentListCacheAsync(cancellationToken);
            }
            return result;
        }

        public async Task<IEnumerable<AgentDto>> GetAgentsByDepartmentAsync(string department, CancellationToken cancellationToken = default)
        {
            var agents = await _repository.GetByDepartmentAsync(department, cancellationToken);
            return agents.Select(MapToDto);
        }

        public async Task<IEnumerable<AgentDto>> GetAvailableAgentsAsync(CancellationToken cancellationToken = default)
        {
            var agents = await _repository.GetAvailableAgentsAsync(cancellationToken);
            return agents.Select(MapToDto);
        }

        public async Task<IEnumerable<AgentDto>> GetAgentsBySkillAsync(string skill, int minimumLevel, CancellationToken cancellationToken = default)
        {
            var agents = await _repository.GetBySkillLevelAsync(skill, minimumLevel, cancellationToken);
            return agents.Select(MapToDto);
        }

        public async Task<AgentMonitoringDto> GetAgentMonitoringDataAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var agent = await _repository.GetByIdAsync(id, cancellationToken);
            if (agent == null) return null;

            return new AgentMonitoringDto
            {
                AgentId = agent.Id,
                Status = agent.Status,
                RealTimeMetrics = agent.PerformanceMetrics,
                ActiveAlerts = new List<string>(), // TODO: Implement alerts
                CurrentTaskId = agent.AssignedTasks.FirstOrDefault(),
                PerformanceScore = CalculatePerformanceScore(agent),
                SystemResources = new Dictionary<string, string>() // TODO: Implement system resource monitoring
            };
        }

        public async Task<IDictionary<string, double>> GetAgentMetricsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _repository.GetPerformanceMetricsAsync(id, cancellationToken);
        }

        public async Task<bool> UpdateAgentMetricsAsync(Guid id, IDictionary<string, double> metrics, CancellationToken cancellationToken = default)
        {
            var result = await _repository.UpdateMetricsAsync(id, metrics.ToDictionary(k => k.Key, v => v.Value), cancellationToken);
            if (result)
            {
                await InvalidateAgentCacheAsync(id, cancellationToken);
            }
            return result;
        }

        public async Task<bool> UpdateAgentStatusAsync(Guid id, string status, CancellationToken cancellationToken = default)
        {
            var result = await _repository.UpdateStatusAsync(id, status, cancellationToken);
            if (result)
            {
                await InvalidateAgentCacheAsync(id, cancellationToken);
            }
            return result;
        }

        public async Task<bool> UpdateAgentAvailabilityAsync(Guid id, AgentAvailabilityDto availability, CancellationToken cancellationToken = default)
        {
            var agentAvailability = new AgentAvailability
            {
                IsAvailable = availability.IsAvailable,
                NextAvailableTime = availability.NextAvailableTime != null 
                    ? DateTime.Parse(availability.NextAvailableTime) 
                    : null,
                CurrentActivity = availability.CurrentActivity,
                WorkloadPercentage = availability.WorkloadPercentage
            };

            var result = await _repository.UpdateAvailabilityAsync(id, agentAvailability, cancellationToken);
            if (result)
            {
                await InvalidateAgentCacheAsync(id, cancellationToken);
            }
            return result;
        }

        public async Task<bool> UpdateAgentSkillsAsync(Guid id, IEnumerable<AgentSkillDto> skills, CancellationToken cancellationToken = default)
        {
            var skillLevels = skills.ToDictionary(s => s.Name, s => s.Level);
            var result = await _repository.UpdateSkillLevelsAsync(id, skillLevels, cancellationToken);
            if (result)
            {
                await InvalidateAgentCacheAsync(id, cancellationToken);
            }
            return result;
        }

        public async Task<IEnumerable<AgentSkillDto>> GetAgentSkillsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var agent = await _repository.GetByIdAsync(id, cancellationToken);
            if (agent == null) return Enumerable.Empty<AgentSkillDto>();

            return agent.SkillLevels.Select(s => new AgentSkillDto
            {
                Name = s.Key,
                Level = s.Value,
                LastUsed = DateTime.UtcNow, // TODO: Implement skill usage tracking
                TimesUsed = 0 // TODO: Implement skill usage tracking
            });
        }

        public Task StartMonitoringAsync(Guid id, CancellationToken cancellationToken = default)
        {
            _monitoredAgents.Add(id);
            return Task.CompletedTask;
        }

        public Task StopMonitoringAsync(Guid id, CancellationToken cancellationToken = default)
        {
            _monitoredAgents.Remove(id);
            return Task.CompletedTask;
        }

        public Task<bool> IsBeingMonitoredAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_monitoredAgents.Contains(id));
        }

        private async Task CacheAgentAsync(AgentDto agent, CancellationToken cancellationToken)
        {
            var cacheKey = $"{CacheKeyPrefix}{agent.Id}";
            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheExpiration,
                SlidingExpiration = TimeSpan.FromMinutes(5)
            };

            await _cache.SetAsync(cacheKey, agent, options, cancellationToken);
        }

        private async Task CacheAgentsListAsync(IEnumerable<AgentDto> agents, CancellationToken cancellationToken)
        {
            var cacheKey = $"{CacheKeyPrefix}all";
            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheExpiration,
                SlidingExpiration = TimeSpan.FromMinutes(5)
            };

            await _cache.SetAsync(cacheKey, agents, options, cancellationToken);
        }

        private async Task InvalidateAgentCacheAsync(Guid id, CancellationToken cancellationToken)
        {
            var cacheKey = $"{CacheKeyPrefix}{id}";
            await _cache.RemoveAsync(cacheKey, cancellationToken);
        }

        private async Task InvalidateAgentListCacheAsync(CancellationToken cancellationToken)
        {
            var cacheKey = $"{CacheKeyPrefix}all";
            await _cache.RemoveAsync(cacheKey, cancellationToken);
        }

        private static AgentDto MapToDto(Agent agent)
        {
            return new AgentDto
            {
                Id = agent.Id,
                Name = agent.Name,
                IsOnline = agent.IsOnline,
                LastActive = agent.LastActive.ToString("O"),
                Status = agent.Status,
                Department = agent.Department,
                Metrics = agent.PerformanceMetrics,
                Stats = new AgentStatsDto
                {
                    CompletedTasks = agent.CompletedTasks,
                    EfficiencyRating = agent.EfficiencyRating,
                    ActiveTasks = agent.AssignedTasks.Count,
                    ResponseTime = 0, // TODO: Implement response time tracking
                    TaskSuccessRate = 0, // TODO: Implement task success rate tracking
                    AverageTaskDuration = 0 // TODO: Implement task duration tracking
                },
                Availability = new AgentAvailabilityDto
                {
                    IsAvailable = agent.Availability.IsAvailable,
                    NextAvailableTime = agent.Availability.NextAvailableTime?.ToString("O"),
                    CurrentActivity = agent.Availability.CurrentActivity,
                    WorkloadPercentage = agent.Availability.WorkloadPercentage,
                    ShiftStatus = "Active" // TODO: Implement shift management
                },
                Skills = agent.SkillLevels.Select(s => new AgentSkillDto
                {
                    Name = s.Key,
                    Level = s.Value,
                    LastUsed = DateTime.UtcNow, // TODO: Implement skill usage tracking
                    TimesUsed = 0 // TODO: Implement skill usage tracking
                }).ToList()
            };
        }

        private static double CalculatePerformanceScore(Agent agent)
        {
            // TODO: Implement a more sophisticated performance scoring algorithm
            return agent.EfficiencyRating;
        }
        public Task<IEnumerable<AgentActivityDto>> GetAgentActivitiesAsync(int limit = 50, CancellationToken cancellationToken = default)
        {
            // Core implementation - for now return empty list or implement real logging later
            return Task.FromResult<IEnumerable<AgentActivityDto>>(new List<AgentActivityDto>());
        }
    }
}
