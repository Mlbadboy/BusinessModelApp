using System.Threading.Tasks;

namespace BusinessModelApp.Core.Services
{
    public interface IGeminiService : IAIService
    {
        Task<string> GetCompletionAsync(string prompt, string modelId = null);
        Task<float[]> GetEmbeddingAsync(string text);
        Task<decimal> GetCostEstimate(string modelId, int promptTokens, int completionTokens);
        
        // Model Management
        Task<string[]> GetAvailableModelsAsync();
        Task SetDefaultModelAsync(string modelId);
    }
}
