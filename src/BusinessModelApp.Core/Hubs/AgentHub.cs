using System;
using System.Threading.Tasks;
using BusinessModelApp.Core.DTOs.Agent;
using Microsoft.AspNetCore.SignalR;

namespace BusinessModelApp.Core.Hubs
{
    public class AgentHub : Hub<IAgentClient>
    {
        public const string AgentStatusUpdated = "AgentStatusUpdated";
        public const string AgentMetricsUpdated = "AgentMetricsUpdated";
        public const string AgentAlert = "AgentAlert";

        public async Task JoinAgentGroup(string agentId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"agent_{agentId}");
        }

        public async Task LeaveAgentGroup(string agentId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"agent_{agentId}");
        }
    }

    public interface IAgentClient
    {
        Task OnAgentStatusUpdated(string agentId, AgentUpdateDto update);
        Task OnAgentMetricsUpdated(string agentId, AgentMonitoringDto metrics);
        Task OnAgentAlert(string agentId, string alertMessage, string severity);
    }
}
