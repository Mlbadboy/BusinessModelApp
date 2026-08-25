using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BusinessModelApp.Core.AI
{
    public interface IAIInferenceGateway
    {
        Task<AIResponse> ExecuteAsync(AIRequest request, CancellationToken ct = default);
        IAsyncEnumerable<AIStreamChunk> StreamAsync(AIRequest request, CancellationToken ct = default);
        Task<float[]> GetEmbeddingAsync(string text, CancellationToken ct = default);
    }
}
