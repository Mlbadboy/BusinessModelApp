using System.Threading.Tasks;

namespace BusinessModelApp.Core.Services
{
    public interface IOpenRouterService : IAIService
    {
        Task<decimal> GetCostEstimate(string modelId, int promptTokens, int completionTokens);
        
        // Model Management
        Task<string[]> GetAvailableModelsAsync();
        Task SetDefaultModelAsync(string modelId);
    }
}
