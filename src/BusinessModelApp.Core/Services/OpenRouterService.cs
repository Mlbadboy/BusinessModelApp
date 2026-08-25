using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;

namespace BusinessModelApp.Core.Services
{
    public class OpenRouterService : IAIService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OpenRouterService> _logger;
        private readonly string _apiKey;
        private readonly string _model;
        private readonly string _siteUrl;
        private readonly string _siteName;

        public string ProviderName => "OpenRouter";

        public OpenRouterService(
            HttpClient httpClient, 
            IConfiguration configuration,
            ILogger<OpenRouterService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _apiKey = configuration["AI:OpenRouter:ApiKey"];
            _model = configuration["AI:OpenRouter:DefaultModel"] ?? "mistralai/mistral-7b-instruct";
            _siteUrl = configuration["AI:OpenRouter:SiteUrl"] ?? "http://localhost:3000";
            _siteName = configuration["AI:OpenRouter:SiteName"] ?? "BusinessModelApp";

            _httpClient.BaseAddress = new Uri("https://openrouter.ai/api/v1/");
            
            if (!string.IsNullOrEmpty(_apiKey))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
                _httpClient.DefaultRequestHeaders.Add("HTTP-Referer", _siteUrl);
                _httpClient.DefaultRequestHeaders.Add("X-Title", _siteName);
            }
            
            Console.WriteLine($"[OpenRouterService] Initialized. Model: {_model}, API Key Present: {!string.IsNullOrEmpty(_apiKey)}");
        }

        public async Task<string> GetCompletionAsync(string prompt)
        {
            if (string.IsNullOrEmpty(_apiKey) || _apiKey == "YOUR_OPENROUTER_KEY")
            {
                throw new InvalidOperationException("OpenRouter API Key is missing or invalid.");
            }

            var request = new
            {
                model = _model,
                messages = new[] 
                {
                    new { role = "user", content = prompt }
                }
            };

            var response = await _httpClient.PostAsync("chat/completions",
                new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                var msg = $"OpenRouter API Error: {response.StatusCode} - {errorContent}";
                Console.WriteLine($"[OpenRouterService] {msg}");
                _logger.LogError(msg);
                throw new HttpRequestException(msg);
            }
            
            var responseContent = await response.Content.ReadAsStringAsync();
            using var jsonDoc = JsonDocument.Parse(responseContent);
            return jsonDoc.RootElement.GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();
        }

        public async Task<float[]> GetEmbeddingAsync(string text)
        {
            throw new NotImplementedException();
        }
    }
}
