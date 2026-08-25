using System.Threading.Tasks;

namespace BusinessModelApp.Core.Interfaces
{
    public interface IAgentBroadcaster
    {
        Task BroadcastLog(string message);
        Task BroadcastStatus(string status);
    }
}
