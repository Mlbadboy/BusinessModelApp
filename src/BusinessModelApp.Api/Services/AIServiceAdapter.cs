using System;
using System.Threading.Tasks;
using BusinessModelApp.Core.AI;
using BusinessModelApp.Core.Services;

namespace BusinessModelApp.Api.Services
{
    [Obsolete("Legacy IAIService is deprecated. Migrate consumers to IAIInferenceGateway instead.")]
    public class AIServiceAdapter : IAIService
    {
        private readonly IAIInferenceGateway _gateway;

        public AIServiceAdapter(IAIInferenceGateway gateway)
        {
            _gateway = gateway;
        }

        public string ProviderName => "OmniRouteGateway";

        public async Task<string> GetCompletionAsync(string prompt)
        {
            var request = new AIRequest
            {
                TaskType = AITaskType.GeneralAssistant,
                Messages = { AIMessage.User(prompt) }
            };

            var response = await _gateway.ExecuteAsync(request);
            return response.Content;
        }

        public async Task<float[]> GetEmbeddingAsync(string text)
        {
            return await _gateway.GetEmbeddingAsync(text);
        }
    }
}
