using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using BusinessModelApp.Core.DTOs.Agent;
using BusinessModelApp.Core.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace BusinessModelApp.Core.Services
{
    public interface IAgentMonitoringService
    {
        Task StartMonitoringAsync(string agentId, CancellationToken cancellationToken = default);
        Task StopMonitoringAsync(string agentId, CancellationToken cancellationToken = default);
        Task UpdateAgentStatusAsync(string agentId, AgentUpdateDto update, CancellationToken cancellationToken = default);
        Task UpdateAgentMetricsAsync(string agentId, AgentMonitoringDto metrics, CancellationToken cancellationToken = default);
        Task SendAgentAlertAsync(string agentId, string message, string severity, CancellationToken cancellationToken = default);
    }

    public class AgentMonitoringService : IAgentMonitoringService
    {
        private readonly IHubContext<AgentHub, IAgentClient> _hubContext;
        private readonly ILogger<AgentMonitoringService> _logger;
        private readonly ConcurrentDictionary<string, AgentMonitoringState> _monitoredAgents;

        public AgentMonitoringService(
            IHubContext<AgentHub, IAgentClient> hubContext,
            ILogger<AgentMonitoringService> logger)
        {
            _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _monitoredAgents = new ConcurrentDictionary<string, AgentMonitoringState>();
        }

        public async Task StartMonitoringAsync(string agentId, CancellationToken cancellationToken = default)
        {
            var state = _monitoredAgents.GetOrAdd(agentId, id => new AgentMonitoringState
            {
                AgentId = id,
                StartTime = DateTime.UtcNow,
                IsActive = true
            });

            _logger.LogInformation("Started monitoring agent {AgentId}", agentId);
        }

        public async Task StopMonitoringAsync(string agentId, CancellationToken cancellationToken = default)
        {
            if (_monitoredAgents.TryRemove(agentId, out var state))
            {
                state.IsActive = false;
                _logger.LogInformation("Stopped monitoring agent {AgentId}", agentId);
            }
        }

        public async Task UpdateAgentStatusAsync(string agentId, AgentUpdateDto update, CancellationToken cancellationToken = default)
        {
            if (!_monitoredAgents.TryGetValue(agentId, out var state) || !state.IsActive)
            {
                return;
            }

            try
            {
                await _hubContext.Clients.Group($"agent_{agentId}")
                    .OnAgentStatusUpdated(agentId, update);
                
                _logger.LogDebug("Updated status for agent {AgentId}: {Status}", agentId, update.Status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating status for agent {AgentId}", agentId);
                throw;
            }
        }

        public async Task UpdateAgentMetricsAsync(string agentId, AgentMonitoringDto metrics, CancellationToken cancellationToken = default)
        {
            if (!_monitoredAgents.TryGetValue(agentId, out var state) || !state.IsActive)
            {
                return;
            }

            try
            {
                await _hubContext.Clients.Group($"agent_{agentId}")
                    .OnAgentMetricsUpdated(agentId, metrics);
                
                _logger.LogDebug("Updated metrics for agent {AgentId}", agentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating metrics for agent {AgentId}", agentId);
                throw;
            }
        }

        public async Task SendAgentAlertAsync(string agentId, string message, string severity, CancellationToken cancellationToken = default)
        {
            if (!_monitoredAgents.TryGetValue(agentId, out var state) || !state.IsActive)
            {
                return;
            }

            try
            {
                await _hubContext.Clients.Group($"agent_{agentId}")
                    .OnAgentAlert(agentId, message, severity);
                
                _logger.LogInformation("Sent alert for agent {AgentId}: {Message}", agentId, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending alert for agent {AgentId}", agentId);
                throw;
            }
        }

        private class AgentMonitoringState
        {
            public string AgentId { get; set; }
            public DateTime StartTime { get; set; }
            public bool IsActive { get; set; }
        }
    }
}
