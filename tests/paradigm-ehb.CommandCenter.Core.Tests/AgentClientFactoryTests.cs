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
            IAgentClientFactory agentClientFactory = CreateDefaultAgentClientFactory();

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
        }


        private IAgentClientFactory CreateDefaultAgentClientFactory()
        {
            Mock<IAgentClientRegistry> mockAgentClientRegistry = new Mock<IAgentClientRegistry>();
            Mock<IGrpcChannelFactory> mockGrpcChannelFactory = new Mock<IGrpcChannelFactory>();
            mockGrpcChannelFactory.Setup(factory => factory.CreateChannel(It.IsAny<AgentEndpoint>()))
                .Returns(Grpc.Net.Client.GrpcChannel.ForAddress("http://localhost"));

            return new AgentClientFactory(mockAgentClientRegistry.Object, mockGrpcChannelFactory.Object);
        }
    }
}
