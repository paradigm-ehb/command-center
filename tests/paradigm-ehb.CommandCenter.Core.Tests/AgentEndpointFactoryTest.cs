using paradigm_ehb.CommandCenter.Core.Factories;
using paradigm_ehb.CommandCenter.Core.Models;

namespace paradigm_ehb.CommandCenter.Core.Tests
{
    public class AgentEndpointFactoryTest
    {
        AgentEndpointFactory _agentEndpointFactory = new AgentEndpointFactory();

        [Fact]
        public void CreateEndpointWithAllParams()
        {
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

            AgentEndpoint agentEndpoint = _agentEndpointFactory.Create(ipAddress, port, useTls, displayName, metadata);

            Assert.NotNull(agentEndpoint);
            Assert.Equal(ipAddress, agentEndpoint.IpAddress);
            Assert.Equal(port, agentEndpoint.Port);
            Assert.Equal(useTls, agentEndpoint.UseTls);
            Assert.Equal(displayName, agentEndpoint.DisplayName);
            Assert.Equal(metadata, agentEndpoint.Metadata);
            Assert.Null(agentEndpoint.LastSeen);
            Assert.Equal(AgentHealthStatus.Unknown, agentEndpoint.HealthStatus);
        }
    }
}
