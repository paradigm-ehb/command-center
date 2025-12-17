using Microsoft.Extensions.DependencyInjection;
using paradigm_ehb.CommandCenter.Core.Factories;
using paradigm_ehb.CommandCenter.Core.Interfaces;
using paradigm_ehb.CommandCenter.Core.Services;

namespace paradigm_ehb.CommandCenter.Core
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddCommandCenterCore(this IServiceCollection services)
        {
            services.AddSingleton<IAgentEndpointFactory, AgentEndpointFactory>();
            services.AddSingleton<IAgentEndpointRegistry, AgentEndpointRegistry>(); //Endpoint aanmaken
            services.AddSingleton<IGrpcChannelFactory, GrpcChannelFactory>(); //Niet nodig
            services.AddSingleton<IAgentClientFactory, AgentClientFactory>(); //Wordt gebruikt om een connectie te make met de agent
            services.AddSingleton<IAgentClientRegistry, AgentClientRegistry>();
            services.AddSingleton<IAgentMonitor, AgentMonitor>();
            return services;
        }
    }
}
