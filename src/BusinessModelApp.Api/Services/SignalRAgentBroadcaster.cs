using System.Threading.Tasks;
using BusinessModelApp.Api.Hubs;
using BusinessModelApp.Core.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace BusinessModelApp.Api.Services
{
    public class SignalRAgentBroadcaster : IAgentBroadcaster
    {
        private readonly IHubContext<AgentHub> _hubContext;

        public SignalRAgentBroadcaster(IHubContext<AgentHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task BroadcastLog(string message)
        {
            await _hubContext.Clients.All.SendAsync("ReceiveLog", message);
        }

        public async Task BroadcastStatus(string status)
        {
            await _hubContext.Clients.All.SendAsync("ReceiveStatus", status);
        }
    }
}
