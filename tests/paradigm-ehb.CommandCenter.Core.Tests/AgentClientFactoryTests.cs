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

        [Fact]
        public async Task Create_ClientWithAllParams_ReturnsPopulatedClient()
        {
            Mock<IAgentClientRegistry> mockAgentClientRegistry = CreateDefaultMock<IAgentClientRegistry>();
            Mock<IGrpcChannelFactory> mockGrpcChannelFactory = CreateDefaultMock<IGrpcChannelFactory>();

            // Setup the mock to return a GrpcChannel when CreateChannel is called
            mockGrpcChannelFactory.Setup(factory => factory.CreateChannel(It.IsAny<AgentEndpoint>()))
                .Returns(Grpc.Net.Client.GrpcChannel.ForAddress("http://localhost"));

            IAgentClientFactory agentClientFactory = CreateDefaultAgentClientFactory(mockAgentClientRegistry.Object, mockGrpcChannelFactory.Object);

            AgentEndpoint agentEndpoint = new AgentEndpoint
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

            AgentClientEntry agentClientEntry = await agentClientFactory.CreateClientAsync(agentEndpoint);

            Assert.Equal(agentEndpoint.Id, agentClientEntry.EndpointId);
            Assert.IsType<Grpc.Net.Client.GrpcChannel>(agentClientEntry.Channel);

            mockAgentClientRegistry.Verify();
            mockGrpcChannelFactory.Verify();
        }

        [Fact]
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

        [Fact]
        public async Task CreateAndRegister_ClientWithAllParams_ReturnsPopulatedClient()
        {
            Mock<IAgentClientRegistry> mockAgentClientRegistry = CreateDefaultMock<IAgentClientRegistry>();

            mockAgentClientRegistry.Setup(registry => registry.RegisterAsync(It.IsAny<AgentClientEntry>()))
                .ReturnsAsync((AgentClientEntry entry, CancellationToken _) => new AgentClientRegistrationResult(true, entry, new List<string>().AsReadOnly()));

            Mock<IGrpcChannelFactory> mockGrpcChannelFactory = CreateDefaultMock<IGrpcChannelFactory>();

            mockGrpcChannelFactory.Setup(factory => factory.CreateChannel(It.IsAny<AgentEndpoint>()))
                .Returns(Grpc.Net.Client.GrpcChannel.ForAddress("http://localhost"));

            IAgentClientFactory agentClientFactory = CreateDefaultAgentClientFactory(mockAgentClientRegistry.Object, mockGrpcChannelFactory.Object);

            AgentEndpoint agentEndpoint = new AgentEndpoint
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

            AgentClientEntry agentClientEntry = await agentClientFactory.CreateAndRegisterClientAsync(agentEndpoint);

            Assert.Equal(agentEndpoint.Id, agentClientEntry.EndpointId);
            Assert.IsType<Grpc.Net.Client.GrpcChannel>(agentClientEntry.Channel);

            mockAgentClientRegistry.Verify();
            mockGrpcChannelFactory.Verify();
        }

        private IAgentClientFactory CreateDefaultAgentClientFactory(IAgentClientRegistry agentClientRegistry, IGrpcChannelFactory grpcChannelFactory)
        {
            return new AgentClientFactory(agentClientRegistry, grpcChannelFactory);
        }

        private Mock<T> CreateDefaultMock<T>() where T : class
        {
            return new Mock<T>();
        }
    }
}
