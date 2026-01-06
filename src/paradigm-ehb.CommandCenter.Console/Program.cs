// See https://aka.ms/new-console-template for more information
using Grpc.Core;
using Grpc.Health.V1;
using Journal.V1;
using Microsoft.Extensions.DependencyInjection;
using paradigm_ehb.CommandCenter.Core.Enums;
using paradigm_ehb.CommandCenter.Core.Factories;
using paradigm_ehb.CommandCenter.Core.Interfaces;
using paradigm_ehb.CommandCenter.Core.Models;
using paradigm_ehb.CommandCenter.Core.Services;
using Services.V1;

Console.WriteLine("Hello, World!");

Console.WriteLine(Client.DependencyMethod());

IServiceCollection services = new ServiceCollection()
    .AddCommandCenterCore();

ServiceProvider provider = services.BuildServiceProvider();

IAgentEndpointFactory agentEndpointFactory = provider.GetRequiredService<IAgentEndpointFactory>();

IAgentClientFactory agentClientFactory = provider.GetRequiredService<IAgentClientFactory>();

IAgentClientRegistry agentClientRegistry = provider.GetRequiredService<IAgentClientRegistry>();

IAgentEndpointRegistry agentEndpointRegistry = provider.GetRequiredService<IAgentEndpointRegistry>();

AgentEndpoint agentEndpoint = agentEndpointFactory.Create("62.84.183.50", 5000, false);

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

AsyncServerStreamingCall<JournalChunk> call = agentClient.Journal.Action(new Journal.V1.JournalRequest() { NumFromTail=20, Field=Journal.V1.JournalRequest.Types.Field.Systemd, Value="systemd-journald.service" });


await foreach (var response in call.ResponseStream.ReadAllAsync())
{
    Console.WriteLine(response.Reply.ToStringUtf8());
}

static void ChangedHealthStatus(object? sender, HealthStatusChangedEventArgs eventArgs)
{
    if (sender is AgentEndpoint endpoint)
    {
    Console.WriteLine($"Health status of {endpoint.DisplayName} to: {eventArgs.HealthStatus}\n");
    }
}

static void ChangedReachabilityStatus(object? sender, ReachabilityChangedEventArgs eventArgs)
{
    if (sender is AgentEndpoint endpoint)
    {
    Console.WriteLine($"Reachability status of {endpoint.DisplayName} to: {eventArgs.AgentReachability}");
    }
}

Console.ReadKey();
