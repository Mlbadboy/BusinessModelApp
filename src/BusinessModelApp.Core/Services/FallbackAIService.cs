using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace BusinessModelApp.Core.Services
{
    public class FallbackAIService : IAIService
    {
        private readonly IEnumerable<IAIService> _services;
        private readonly ILogger<FallbackAIService> _logger;

        // We don't expose a single provider name, but we can return the one that succeeded last or a composite name.
        public string ProviderName => "FallbackOrchestrator";

        public FallbackAIService(IEnumerable<IAIService> services, ILogger<FallbackAIService> logger)
        {
            // Filter out itself to avoid recursion if registered incorrectly, 
            // and sort/prioritize. For now, we rely on the order of registration or a specific list.
            // We'll assume the services are injected in the desired order of priority:
            // 1. Gemini (Primary)
            // 2. OpenRouter (Secondary)
            // 3. Antigravity (Ultimate Fallback)
            _services = services.Where(s => s.GetType() != typeof(FallbackAIService)).ToList();
            _logger = logger;
            
            // Console.WriteLine($"[FallbackAIService] Initialized with {_services.Count()} providers: {string.Join(", ", _services.Select(s => s.ProviderName))}");
        }

        public async Task<string> GetCompletionAsync(string prompt)
        {
            var exceptions = new List<Exception>();

            foreach (var service in _services)
            {
                try
                {
                    Console.WriteLine($"[FallbackAIService] Attempting generation with provider: {service.ProviderName}");
                    var result = await service.GetCompletionAsync(prompt);
                    
                    if (!string.IsNullOrWhiteSpace(result))
                    {
                        Console.WriteLine($"[FallbackAIService] Success with provider: {service.ProviderName}");
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    var msg = $"[FallbackAIService] Provider {service.ProviderName} failed: {ex.Message}";
                    Console.WriteLine(msg);
                    _logger.LogWarning(ex, "Provider {ProviderName} failed.", service.ProviderName);
                    exceptions.Add(ex);
                }
            }

            throw new AggregateException("All AI providers failed to generate content.", exceptions);
        }

        public async Task<float[]> GetEmbeddingAsync(string text)
        {
             var exceptions = new List<Exception>();

            foreach (var service in _services)
            {
                try
                {
                    return await service.GetEmbeddingAsync(text);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }
            throw new AggregateException("All AI providers failed to generate embeddings.", exceptions);
        }
    }
}
