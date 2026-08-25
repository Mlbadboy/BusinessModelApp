using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace BusinessModelApp.Api.Hubs
{
    public class AgentHub : Hub
    {
        public async Task SendLog(string message)
        {
            await Clients.All.SendAsync("ReceiveLog", message);
        }

        public async Task SendStatus(string status)
        {
            await Clients.All.SendAsync("ReceiveStatus", status);
        }
    }
}
