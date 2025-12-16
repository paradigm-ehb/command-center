using Moq;
using paradigm_ehb.CommandCenter.Core.Factories;
using paradigm_ehb.CommandCenter.Core.Interfaces;
using paradigm_ehb.CommandCenter.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace paradigm_ehb.CommandCenter.Core.Tests
{
    public class AgentClientFactoryTests
    {

        [Fact(DisplayName = "Create Client - Client with all params returns populated client")]
        public async Task Create_ClientWithAllParams_ReturnsPopulatedClient()
        {
            Mock<IAgentClientRegistry> mockAgentClientRegistry = CreateDefaultMock<IAgentClientRegistry>();
            Mock<IGrpcChannelFactory> mockGrpcChannelFactory = CreateDefaultMock<IGrpcChannelFactory>();

            // Setup the mock to return a GrpcChannel when CreateChannel is called
            mockGrpcChannelFactory.Setup(factory => factory.CreateChannel(It.IsAny<AgentEndpoint>()))
                .Returns(Grpc.Net.Client.GrpcChannel.ForAddress("http://localhost"));

            IAgentClientFactory agentClientFactory = CreateDefaultAgentClientFactory(mockAgentClientRegistry.Object, mockGrpcChannelFactory.Object);

            AgentEndpoint agentEndpoint = CreateDefaultAgentEndpoint();

            AgentClient agentClientEntry = await agentClientFactory.CreateClientAsync(agentEndpoint);

            Assert.Equal(agentEndpoint.Id, agentClientEntry.Endpoint.Id);
            Assert.IsType<Grpc.Net.Client.GrpcChannel>(agentClientEntry.Channel);

            mockAgentClientRegistry.VerifyAll();
            mockGrpcChannelFactory.VerifyAll();
        }

        [Fact(DisplayName = "Create Client - Client with null endpoint throws ArgumentNullException")]
        public async Task Create_ClientWithNullEndpoint_ThrowsArgumentNullException()
        {
            Mock<IAgentClientRegistry> mockAgentClientRegistry = CreateDefaultMock<IAgentClientRegistry>();
            Mock<IGrpcChannelFactory> mockGrpcChannelFactory = CreateDefaultMock<IGrpcChannelFactory>();

            IAgentClientFactory agentClientFactory = CreateDefaultAgentClientFactory(mockAgentClientRegistry.Object, mockGrpcChannelFactory.Object);

            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await agentClientFactory.CreateClientAsync(null!);
            });

            mockAgentClientRegistry.VerifyAll();
            mockGrpcChannelFactory.VerifyAll();
        }

        [Fact(DisplayName = "Create and Register Client - Client with all params returns populated client")]
        public async Task CreateAndRegister_ClientWithAllParams_ReturnsPopulatedClient()
        {
            Mock<IAgentClientRegistry> mockAgentClientRegistry = CreateDefaultMock<IAgentClientRegistry>();

            mockAgentClientRegistry.Setup(registry => registry.RegisterAsync(It.IsAny<AgentClient>()))
                .ReturnsAsync((AgentClient entry, CancellationToken _) => new AgentClientRegistrationResult(true, entry, new List<string>().AsReadOnly()));

            Mock<IGrpcChannelFactory> mockGrpcChannelFactory = CreateDefaultMock<IGrpcChannelFactory>();

            mockGrpcChannelFactory.Setup(factory => factory.CreateChannel(It.IsAny<AgentEndpoint>()))
                .Returns(Grpc.Net.Client.GrpcChannel.ForAddress("http://localhost"));

            IAgentClientFactory agentClientFactory = CreateDefaultAgentClientFactory(mockAgentClientRegistry.Object, mockGrpcChannelFactory.Object);

            AgentEndpoint agentEndpoint = CreateDefaultAgentEndpoint();

            AgentClient agentClientEntry = await agentClientFactory.CreateAndRegisterClientAsync(agentEndpoint);

            Assert.Equal(agentEndpoint.Id, agentClientEntry.Endpoint.Id);
            Assert.IsType<Grpc.Net.Client.GrpcChannel>(agentClientEntry.Channel);

            mockAgentClientRegistry.VerifyAll();
            mockGrpcChannelFactory.VerifyAll();
        }

        [Fact(DisplayName = "Create and Register Client - Client with null endpoint throws ArgumentNullException")]
        public async Task CreateAndRegister_ClientWithNullEndpoint_ThrowsArgumentNullException()
        {
            Mock<IAgentClientRegistry> mockAgentClientRegistry = CreateDefaultMock<IAgentClientRegistry>();
            Mock<IGrpcChannelFactory> mockGrpcChannelFactory = CreateDefaultMock<IGrpcChannelFactory>();

            IAgentClientFactory agentClientFactory = CreateDefaultAgentClientFactory(mockAgentClientRegistry.Object, mockGrpcChannelFactory.Object);

            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await agentClientFactory.CreateAndRegisterClientAsync(null!);
            });

            mockAgentClientRegistry.VerifyAll();
            mockGrpcChannelFactory.VerifyAll();
        }

        private IAgentClientFactory CreateDefaultAgentClientFactory(IAgentClientRegistry agentClientRegistry, IGrpcChannelFactory grpcChannelFactory) => new AgentClientFactory(agentClientRegistry, grpcChannelFactory);

        private Mock<T> CreateDefaultMock<T>() where T : class => new Mock<T>();

        private AgentEndpoint CreateDefaultAgentEndpoint() => new AgentEndpoint
        {
            Id = Guid.NewGuid(),
            IpAddress = "localhost",
            Port = 50051,
            UseTls = false,
            DisplayName = "Local Agent",
            Metadata = new Dictionary<string, string>
            {
                ["os"] = "linux",
                ["version"] = "1.0.0"
            }
        };
    }
}
