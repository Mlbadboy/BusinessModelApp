using System;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Execution;

namespace BusinessModelApp.Core.Interfaces
{
    public interface IAutonomousMissionExecutor
    {
        Task<ExecutionReceipt> ExecuteMissionStepAsync(
            ExecutionRequest stepRequest,
            CancellationToken cancellationToken = default);
    }
}
