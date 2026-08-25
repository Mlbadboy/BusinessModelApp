using System.Threading.Tasks;

namespace BusinessModelApp.Core.Interfaces
{
    public interface IRealTimeMonitoringService
    {
        Task StartMonitoringAsync(string userId, string monitorType);
        void StopMonitoring(string userId);
    }
}
