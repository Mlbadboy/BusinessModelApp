using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Execution;

namespace BusinessModelApp.Core.Interfaces
{
    public interface IBrainFabricGovernanceService
    {
        Task<List<BrainProviderSummaryDto>> GetProvidersAsync(Guid workspaceId, CancellationToken cancellationToken = default);
        Task<BrainProviderSummaryDto> ConfigureProviderAsync(Guid workspaceId, ConfigureBrainProviderDto dto, CancellationToken cancellationToken = default);
        Task<bool> TestProviderConnectionAsync(Guid workspaceId, BrainProviderType provider, CancellationToken cancellationToken = default);
        Task<List<DiscoveredModelMetadata>> DiscoverModelsAsync(Guid workspaceId, BrainProviderType provider, CancellationToken cancellationToken = default);
        Task<BrainInferenceTestResponseDto> RunInferenceTestAsync(Guid workspaceId, BrainInferenceTestRequestDto dto, CancellationToken cancellationToken = default);
    }
}
