using System;
using System.Collections.Generic;
using System.Text;

namespace paradigm_ehb.CommandCenter.Core.Models
{
    public sealed class AgentEndpoint
    {
        /// <summary>
        /// Unique identifier for the endpoint (client-assigned or generated).
        /// TODO: determine client-assigned or generated.
        /// </summary>
        public Guid Id { get; init; } = Guid.NewGuid();

        /// <summary>
        /// IP address or host name of the agent.
        /// </summary>
        public string IpAddress { get; init; } = string.Empty;

        /// <summary>
        /// gRPC port the agent listens on. Default commonly 50051.
        /// </summary>
        public int Port { get; init; } = 50051;

        /// <summary>
        /// User-friendly name for display in the UI.
        /// </summary>
        public string DisplayName { get; init; } = string.Empty;

        /// <summary>
        /// Optional arbitrary metadata (os, version, tags).
        /// </summary>
        public IDictionary<string, string>? Metadata { get; init; }

        /// <summary>
        /// Last time the agent responded to a health check or RPC.
        /// </summary>
        public DateTimeOffset? LastSeen { get; set; }

        /// <summary>
        /// Latest health flag maintained by health-checker.
        /// </summary>
        // public AgentHealthStatus HealthStatus { get; set; } // TODO: implement health checking
    }
}
