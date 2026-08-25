using BusinessModelApp.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;

namespace BusinessModelApp.Core.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAIServices(this IServiceCollection services, IConfiguration configuration)
        {
            // HTTP Clients
            // services.AddHttpClient<IOpenRouterService, OpenRouterService>();
            // services.AddHttpClient<MistralService>();
            // services.AddHttpClient<GeminiService>();
            // services.AddHttpClient<ILocalModelService, LocalModelService>();
            
            // Register AI services
            // services.AddScoped<MistralService>();
            // services.AddScoped<GeminiService>();
            
            // Factory for switching providers
            /*
            services.AddScoped<IAIService>(provider => 
            {
                var providerName = configuration["AI:Provider"];
                return providerName switch
                {
                    "Mistral" => provider.GetRequiredService<MistralService>(),
                    "Gemini" => provider.GetRequiredService<GeminiService>(),
                    "LMStudio" => provider.GetRequiredService<ILocalModelService>(),
                    _ => provider.GetRequiredService<IOpenRouterService>()
                };
            });
            */
            
            return services;
        }
    }
}
