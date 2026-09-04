using System;
using BusinessModelApp.Core.Domain.Commercial;
using BusinessModelApp.Core.Interfaces;
using BusinessModelApp.Infrastructure.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BusinessModelApp.Infrastructure.Services
{
    public static class VoiceServiceRegistration
    {
        public static IServiceCollection AddVoiceTelephonyServices(this IServiceCollection services)
        {
            services.AddHttpClient<VapiVoiceService>();
            services.AddHttpClient<RetellVoiceService>();
            services.AddHttpClient<TwilioVoiceService>();
            services.AddScoped<SimulatedVoiceService>();

            services.AddScoped<IVoiceTelephonyService>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<VoiceOptions>>().Value;
                var providerName = options.Provider?.Trim() ?? "Simulation";

                return providerName.ToLowerInvariant() switch
                {
                    "vapi" => sp.GetRequiredService<VapiVoiceService>(),
                    "retell" => sp.GetRequiredService<RetellVoiceService>(),
                    "twilio" => sp.GetRequiredService<TwilioVoiceService>(),
                    _ => sp.GetRequiredService<SimulatedVoiceService>()
                };
            });

            return services;
        }
    }
}
