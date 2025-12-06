using System;
using System.Collections.Generic;
using System.Text;
using paradigm_ehb.CommandCenter.Core.Enums;

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
        /// Gets a value indicating whether Transport Layer Security (TLS) is enabled for the connection.
        /// </summary>
        /// <remarks>When <see langword="true"/>, all communication will be encrypted using TLS. If <see
        /// langword="false"/>, data will be transmitted without encryption. Enabling TLS is recommended for secure
        /// environments.</remarks>
        public bool UseTls { get; init; } = true;

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
        public AgentHealthStatus HealthStatus { get; set; } // TODO: implement health checking
    }
}
