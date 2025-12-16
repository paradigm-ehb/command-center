// See https://aka.ms/new-console-template for more information
using Grpc.Core;
using Grpc.Health.V1;
using Microsoft.Extensions.DependencyInjection;
using paradigm_ehb.CommandCenter.Core.Factories;
using paradigm_ehb.CommandCenter.Core.Interfaces;
using paradigm_ehb.CommandCenter.Core.Models;

Console.WriteLine("Hello, World!");

Console.WriteLine(Client.DependencyMethod());

IServiceCollection services = new ServiceCollection()
    .AddCommandCenterCore();

ServiceProvider provider = services.BuildServiceProvider();

IAgentEndpointFactory agentEndpointFactory = provider.GetRequiredService<IAgentEndpointFactory>();

IAgentClientFactory agentClientFactory = provider.GetRequiredService<IAgentClientFactory>();

IAgentClientRegistry agentClientRegistry = provider.GetRequiredService<IAgentClientRegistry>();

AgentEndpoint agentEndpoint = agentEndpointFactory.Create("localhost", 50051, false);

AgentEndpoint agentEndpoint2 = agentEndpointFactory.Create("localhost", 50051, false);

AgentClient agentClient = await agentClientFactory.CreateClientAsync(agentEndpoint);

AgentClient agentClient2 = await agentClientFactory.CreateClientAsync(agentEndpoint2);


await agentClientRegistry.RegisterAsync(agentClient);
await agentClientRegistry.RegisterAsync(agentClient2);

Console.WriteLine(agentClient.Health.GetType().Name);

while (true)
{
    Console.WriteLine($"AgentClient: {agentClient.Endpoint.HealthStatus}");
    Console.WriteLine($"AgentClient2: {agentClient2.Endpoint.HealthStatus}");
    Task.Delay(5000).Wait();
}
