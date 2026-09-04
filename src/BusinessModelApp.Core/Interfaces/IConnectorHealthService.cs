using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Connectors;

namespace BusinessModelApp.Core.Interfaces
{
    public interface IConnectorHealthService
    {
        Task<ConnectorHealthReportDto> RunHealthProbesAsync(
            Guid workspaceId,
            ConnectorProvider provider,
            CancellationToken ct = default);

        Task<List<ConnectorSummaryDto>> GetAllConnectorSummariesAsync(
            Guid workspaceId,
            CancellationToken ct = default);
    }
}
