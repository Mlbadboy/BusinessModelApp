using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Execution;

namespace BusinessModelApp.Core.Interfaces
{
    public interface ISagaExecutionEngine
    {
        Task<SagaExecutionStateEntity> StartSagaAsync(
            Guid workspaceId,
            Guid missionId,
            string sagaName,
            List<SagaStepRecord> steps,
            CancellationToken cancellationToken = default);

        Task<SagaExecutionStateEntity> ExecuteSagaAsync(
            Guid sagaId,
            CancellationToken cancellationToken = default);
    }
}
