using System.Threading.Tasks;

namespace BusinessModelApp.Core.Services
{
    public interface ILocalModelService : IAIService
    {
        Task LoadModelAsync(string modelPath);
        Task UnloadModelAsync();
        Task<bool> IsModelLoaded();
    }
}
