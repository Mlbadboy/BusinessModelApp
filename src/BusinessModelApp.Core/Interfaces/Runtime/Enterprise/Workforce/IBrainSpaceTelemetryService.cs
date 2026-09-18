using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Workforce;

namespace BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Workforce
{
    public interface IBrainSpaceTelemetryService
    {
        Task<BrainSpaceTelemetrySnapshot> CaptureSnapshotAsync(string tenantId);
    }
}
