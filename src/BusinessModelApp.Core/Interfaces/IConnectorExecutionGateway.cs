using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Execution;

namespace BusinessModelApp.Core.Interfaces
{
    public interface IConnectorExecutionGateway
    {
        Task<ExecutionReceipt> DispatchConnectorActionAsync(
            ExecutionRequest request,
            ExecutionPermit permit,
            CancellationToken cancellationToken = default);
    }
}
