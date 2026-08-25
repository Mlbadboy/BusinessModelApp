using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessModelApp.Core.DTOs;

namespace BusinessModelApp.Core.Services
{
    public interface IRecommendationService
    {
        Task<IEnumerable<Recommendation>> GetAgentRecommendationsAsync(string agentId);
        Task<IEnumerable<Recommendation>> GetExecutiveRecommendationsAsync(string executiveId);
        Task LogFeedbackAsync(string recommendationId, bool wasHelpful, string feedback = null);
        Task TrainOnNewDataAsync(IEnumerable<TrainingExample> examples);
    }
}
