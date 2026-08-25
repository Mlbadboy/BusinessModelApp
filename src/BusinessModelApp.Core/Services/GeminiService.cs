using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BusinessModelApp.Core.Services
{
    public class GeminiService : IAIService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<GeminiService> _logger;
        private readonly string _apiKey;
        private readonly string _model;

        public string ProviderName => "Gemini";

        public GeminiService(
            HttpClient httpClient, 
            IConfiguration configuration,
            ILogger<GeminiService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _apiKey = configuration["Gemini:ApiKey"];
            _model = configuration["Gemini:DefaultModel"];
            
            var endpoint = configuration["Gemini:Endpoint"];
            if (string.IsNullOrEmpty(endpoint))
            {
                _logger.LogError("Gemini:Endpoint is missing in configuration.");
                throw new ArgumentNullException("Gemini:Endpoint");
            }
            
            if (!endpoint.EndsWith("/"))
            {
                endpoint += "/";
            }
            
            _httpClient.BaseAddress = new Uri(endpoint);
        }

        public async Task<string> GetCompletionAsync(string prompt)
        {
            var request = new
            {
                contents = new[] 
                {
                    new 
                    {
                        parts = new[] { new { text = prompt } }
                    }
                }
            };

            var url = $"models/{_model}:generateContent?key={_apiKey}";
            var response = await _httpClient.PostAsync(url,
                new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                var msg = $"Gemini API Error: {response.StatusCode} - {errorContent}";
                _logger.LogError(msg);
                throw new HttpRequestException(msg);
            }
            response.EnsureSuccessStatusCode();
            
            var responseContent = await response.Content.ReadAsStringAsync();
            using var jsonDoc = JsonDocument.Parse(responseContent);
            return jsonDoc.RootElement.GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();
        }

        public async Task<float[]> GetEmbeddingAsync(string text)
        {
            // Implementation for embeddings
            throw new NotImplementedException();
        }
    }
}
