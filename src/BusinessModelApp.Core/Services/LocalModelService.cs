using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace BusinessModelApp.Core.Services
{
    public class LocalModelService : ILocalModelService, IAIService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<LocalModelService> _logger;
        private readonly string _lmStudioUrl;
        
        public string ProviderName => "LMStudio";

        public LocalModelService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<LocalModelService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _lmStudioUrl = configuration["LMStudio:BaseUrl"] ?? "http://localhost:1234";
        }

        Task<float[]> IAIService.GetEmbeddingAsync(string text)
        {
            // Convert the string embedding to float array
            // This is a placeholder - implement actual embedding conversion logic
            return Task.FromResult(new float[0]);
        }

        public async Task<string> GetCompletionAsync(string prompt)
        {
            try
            {
                var request = new
                {
                    prompt,
                    max_tokens = 1000,
                    temperature = 0.7
                };

                var response = await _httpClient.PostAsync($"{_lmStudioUrl}/v1/completions",
                    new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

                response.EnsureSuccessStatusCode();
                
                var responseContent = await response.Content.ReadAsStringAsync();
                using var jsonDoc = JsonDocument.Parse(responseContent);
                return jsonDoc.RootElement.GetProperty("choices")[0].GetProperty("text").GetString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting completion from local model");
                throw;
            }
        }

        // ILocalModelService methods
        private string _loadedModel = string.Empty;

        public Task LoadModelAsync(string modelName)
        {
            _logger.LogInformation($"Loading model {modelName}...");
            // In a real implementation, this would interact with the local model server to load a model.
            _loadedModel = modelName;
            return Task.CompletedTask;
        }

        public Task UnloadModelAsync()
        {
            _logger.LogInformation($"Unloading model {_loadedModel}...");
            _loadedModel = string.Empty;
            return Task.CompletedTask;
        }

        public Task<bool> IsModelLoaded()
        {
            return Task.FromResult(!string.IsNullOrEmpty(_loadedModel));
        }
    }
}
