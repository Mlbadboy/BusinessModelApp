using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.ControlTower;

namespace BusinessModelApp.Core.Interfaces.Runtime.Enterprise.ControlTower
{
    public interface IBusinessControlTowerService
    {
        Task<ControlTowerExecutiveDashboard> GetExecutiveDashboardAsync(
            string tenantId,
            CancellationToken cancellationToken = default);
    }
}
