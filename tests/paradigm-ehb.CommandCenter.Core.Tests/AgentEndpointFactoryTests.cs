using paradigm_ehb.CommandCenter.Core.Enums;
using paradigm_ehb.CommandCenter.Core.Factories;
using paradigm_ehb.CommandCenter.Core.Models;

namespace paradigm_ehb.CommandCenter.Core.Tests
{
    public class AgentEndpointFactoryTests
    {
        [Fact]
        public void Create_EndpointWithAllParams_ReturnsPopulatedEndpoint()
        {
            AgentEndpointFactory agentEndpointFactory = CreateDefaultAgentEndpointFactory();

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

        [Fact]
        public void Create_EndpointWithIpOnly_ReturnsDefaultEndpoint()
        {
            AgentEndpointFactory agentEndpointFactory = CreateDefaultAgentEndpointFactory();

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

        [Fact]
        public void Create_EndpointWithNoIpAddress_ThrowsArgumentException()
        {
            AgentEndpointFactory agentEndpointFactory = CreateDefaultAgentEndpointFactory();

            Assert.Throws<ArgumentException>(() => agentEndpointFactory.Create(null!));
            Assert.Throws<ArgumentException>(() => agentEndpointFactory.Create(" "));
        }

        private AgentEndpointFactory CreateDefaultAgentEndpointFactory()
        {
            return new AgentEndpointFactory();
        }
    }
}
