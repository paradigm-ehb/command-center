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
            services.AddSingleton<IAgentEndpointRegistry, AgentEndpointRegistry>();
            services.AddSingleton<IGrpcChannelFactory, GrpcChannelFactory>();
            services.AddSingleton<IAgentClientFactory, AgentClientFactory>();
            services.AddSingleton<IAgentClientRegistry, AgentClientRegistry>();
            services.AddSingleton<IAgentMonitor, AgentMonitor>();
            return services;
        }
    }
}
