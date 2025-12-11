using paradigm_ehb.CommandCenter.Core.Enums;
using paradigm_ehb.CommandCenter.Core.Factories;
using paradigm_ehb.CommandCenter.Core.Interfaces;
using paradigm_ehb.CommandCenter.Core.Models;

namespace paradigm_ehb.CommandCenter.Core.Tests
{
    public class AgentEndpointFactoryTests
    {
        [Fact(DisplayName = "Create Endpoint With All Parameters Returns Populated Endpoint")]
        public void Create_EndpointWithAllParams_ReturnsPopulatedEndpoint()
        {
            IAgentEndpointFactory agentEndpointFactory = CreateDefaultAgentEndpointFactory();

            string ipAddress = "192.168.1.1";
            int port = 4242;
            bool useTls = false;
            string displayName = "Server01";
            Dictionary<string, string> metadata = new Dictionary<string, string>()
            {
                ["os"] = "windows",
                ["version"] = "11.22.33",
                ["foo"] = "bar"
            };

            AgentEndpoint agentEndpoint = agentEndpointFactory.Create(ipAddress, port, useTls, displayName, metadata);

            Assert.NotNull(agentEndpoint);
            Assert.Equal(ipAddress, agentEndpoint.IpAddress);
            Assert.Equal(port, agentEndpoint.Port);
            Assert.Equal(useTls, agentEndpoint.UseTls);
            Assert.Equal(displayName, agentEndpoint.DisplayName);
            Assert.Equal(metadata, agentEndpoint.Metadata);
            Assert.Null(agentEndpoint.LastSeen);
            Assert.Equal(AgentHealthStatus.Unknown, agentEndpoint.HealthStatus);
        }

        [Fact(DisplayName = "Create Endpoint With Only IP Address Returns Default Endpoint")]
        public void Create_EndpointWithIpOnly_ReturnsDefaultEndpoint()
        {
            IAgentEndpointFactory agentEndpointFactory = CreateDefaultAgentEndpointFactory();

            string ipAddress = "localhost";

            AgentEndpoint agentEndpoint = agentEndpointFactory.Create(ipAddress);

            Assert.NotNull(agentEndpoint);
            Assert.Equal(ipAddress, agentEndpoint.IpAddress);
            Assert.Equal(50051, agentEndpoint.Port);
            Assert.True(agentEndpoint.UseTls);
            Assert.Equal($"{ipAddress}:50051", agentEndpoint.DisplayName);
            Assert.Null(agentEndpoint.Metadata);
            Assert.Null(agentEndpoint.LastSeen);
            Assert.Equal(AgentHealthStatus.Unknown, agentEndpoint.HealthStatus);
        }

        [Theory(DisplayName = "Create Endpoint With No IP Address Throws Argument Exception")]
        [InlineData(null)]
        [InlineData(" ")]
        public void Create_EndpointWithNoIpAddress_ThrowsArgumentException(string? ipAddress)
        {
            IAgentEndpointFactory agentEndpointFactory = CreateDefaultAgentEndpointFactory();

            Assert.Throws<ArgumentException>(() => agentEndpointFactory.Create(ipAddress!));
        }

        private IAgentEndpointFactory CreateDefaultAgentEndpointFactory()
        {
            return new AgentEndpointFactory();
        }
    }
}
