using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BusinessModelApp.Core.Services
{
    public class MistralService : IAIService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<MistralService> _logger;
        private readonly string _apiKey;
        private readonly string _model;

        public string ProviderName => "Mistral";

        public MistralService(
            HttpClient httpClient, 
            IConfiguration configuration,
            ILogger<MistralService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _apiKey = configuration["Mistral:ApiKey"];
            _model = configuration["Mistral:DefaultModel"];
            
            _httpClient.BaseAddress = new Uri(configuration["Mistral:Endpoint"]);
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        }

        public async Task<string> GetCompletionAsync(string prompt)
        {
            var request = new
            {
                model = _model,
                messages = new[] { new { role = "user", content = prompt } },
                temperature = 0.7
            };

            var response = await _httpClient.PostAsync("/chat/completions", 
                new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

            response.EnsureSuccessStatusCode();
            
            var responseContent = await response.Content.ReadAsStringAsync();
            using var jsonDoc = JsonDocument.Parse(responseContent);
            return jsonDoc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
        }

        public async Task<float[]> GetEmbeddingAsync(string text)
        {
            // Implementation for embeddings
            throw new NotImplementedException();
        }
    }
}
