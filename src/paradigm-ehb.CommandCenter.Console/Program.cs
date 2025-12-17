// See https://aka.ms/new-console-template for more information
using Grpc.Core;
using Grpc.Health.V1;
using Microsoft.Extensions.DependencyInjection;
using paradigm_ehb.CommandCenter.Core.Enums;
using paradigm_ehb.CommandCenter.Core.Factories;
using paradigm_ehb.CommandCenter.Core.Interfaces;
using paradigm_ehb.CommandCenter.Core.Models;
using paradigm_ehb.CommandCenter.Core.Services;

Console.WriteLine("Hello, World!");

Console.WriteLine(Client.DependencyMethod());

IServiceCollection services = new ServiceCollection()
    .AddCommandCenterCore();

ServiceProvider provider = services.BuildServiceProvider();

IAgentEndpointFactory agentEndpointFactory = provider.GetRequiredService<IAgentEndpointFactory>();

IAgentClientFactory agentClientFactory = provider.GetRequiredService<IAgentClientFactory>();

IAgentClientRegistry agentClientRegistry = provider.GetRequiredService<IAgentClientRegistry>();

IAgentEndpointRegistry agentEndpointRegistry = provider.GetRequiredService<IAgentEndpointRegistry>();

AgentEndpoint agentEndpoint = agentEndpointFactory.Create("localhost", 50051, false);

AgentEndpoint agentEndpoint2 = agentEndpointFactory.Create("localhost", 50051, false);

await agentEndpointRegistry.RegisterAsync(agentEndpoint);
await agentEndpointRegistry.RegisterAsync(agentEndpoint2);

AgentClient agentClient = await agentClientFactory.CreateClientAsync(agentEndpoint);

AgentClient agentClient2 = await agentClientFactory.CreateClientAsync(agentEndpoint2);

AgentMonitor agentMonitor = new AgentMonitor(TimeSpan.FromSeconds(10), 5);

agentMonitor.StartAsync(agentEndpointRegistry);

await agentClientRegistry.RegisterAsync(agentClient);
await agentClientRegistry.RegisterAsync(agentClient2);

agentEndpoint.HealthStatusChanged += ChangedHealthStatus;
agentEndpoint.ReachabilityChanged += ChangedReachabilityStatus;

static void ChangedHealthStatus(AgentEndpoint sender, HealthStatusChangedEventArgs eventArgs)
{
    Console.WriteLine($"Health status of {sender.DisplayName} to: {eventArgs.HealthStatus}");
}

static void ChangedReachabilityStatus(AgentEndpoint sender, ReachabilityChangedEventArgs eventArgs)
{
    Console.WriteLine($"Reachability status of {sender.DisplayName} to: {eventArgs.AgentReachability}");
}

Console.ReadKey();
