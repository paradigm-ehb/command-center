using paradigm_ehb.CommandCenter.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace paradigm_ehb.CommandCenter.Core.Interfaces
{
    public interface IAgentEndpointFactory
    {
        public AgentEndpoint Create(string ipAddress = "localhost", int port = 50051, bool useTls = true, string? displayName = null, IDictionary<string, string>? metadata = null);
    }
}
