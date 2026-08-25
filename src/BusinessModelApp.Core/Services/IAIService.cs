using System.Threading.Tasks;

namespace BusinessModelApp.Core.Services
{
    public interface IAIService
    {
        Task<string> GetCompletionAsync(string prompt);
        Task<float[]> GetEmbeddingAsync(string text);
        string ProviderName { get; }
    }
}
