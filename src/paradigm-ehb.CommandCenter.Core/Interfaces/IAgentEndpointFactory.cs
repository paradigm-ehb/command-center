using paradigm_ehb.CommandCenter.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace paradigm_ehb.CommandCenter.Core.Interfaces
{
    public interface IAgentEndpointFactory
    {
        /// <summary>
        /// Creates a new AgentEndpoint instance configured to connect to the specified IP address and port, with
        /// optional TLS, display name, and metadata settings.
        /// </summary>
        /// <param name="ipAddress">The IP address of the agent endpoint to connect to. Defaults to "localhost" if not specified.</param>
        /// <param name="port">The port number on which the agent endpoint is listening. Defaults to 50051 if not specified.</param>
        /// <param name="useTls">Specifies whether to use TLS for the connection. Set to <see langword="true"/> to enable TLS; otherwise,
        /// <see langword="false"/>.</param>
        /// <param name="displayName">An optional display name for the agent endpoint. If null, no display name is assigned.</param>
        /// <param name="metadata">An optional collection of key-value pairs to associate with the agent endpoint. If null, no metadata is
        /// attached.</param>
        /// <returns>An AgentEndpoint instance configured with the specified connection parameters.</returns>
        public AgentEndpoint Create(string ipAddress = "localhost", int port = 50051, bool useTls = true, string? displayName = null, IDictionary<string, string>? metadata = null);
    }
}
