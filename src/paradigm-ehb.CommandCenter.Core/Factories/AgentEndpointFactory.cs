using System;
using System.Collections.Generic;
using System.Text;
using paradigm_ehb.CommandCenter.Core.Enums;
using paradigm_ehb.CommandCenter.Core.Interfaces;
using paradigm_ehb.CommandCenter.Core.Models;

namespace paradigm_ehb.CommandCenter.Core.Factories
{
    public sealed class AgentEndpointFactory : IAgentEndpointFactory
    {
        /// <summary>
        /// Creates a new AgentEndpoint instance with the specified IP address, port, connection security, display name,
        /// and optional metadata.
        /// </summary>
        /// <param name="ipAddress">The IP address of the agent endpoint. Cannot be null, empty, or whitespace.</param>
        /// <param name="port">The port number to associate with the agent endpoint. Defaults to 50051 if not specified.</param>
        /// <param name="secureConnection">Specifies whether the connection to the agent endpoint should be secure. Set to <see langword="true"/> to
        /// enable secure connections; otherwise, <see langword="false"/>.</param>
        /// <param name="displayName">An optional display name for the agent endpoint. If not provided, the display name will default to the IP
        /// address and port.</param>
        /// <param name="metadata">An optional collection of key-value pairs containing metadata to associate with the agent endpoint. Can be
        /// null if no metadata is required.</param>
        /// <returns>An AgentEndpoint instance initialized with the provided parameters.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="ipAddress"/> is null, empty, or consists only of whitespace.</exception>
        public AgentEndpoint Create(string ipAddress, int port = 50051, bool useTls = true, string? displayName = null, string? certPath = null, IDictionary<string, string>? metadata = null, bool monitoringEnabled = true)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
            {
                throw new ArgumentException("IP address cannot be null or empty.", nameof(ipAddress));
            }

            string finalDisplayName = displayName ?? $"{ipAddress}:{port}";

            return new AgentEndpoint
            {
                Id = Guid.NewGuid(),
                IpAddress = ipAddress,
                Port = port,
                UseTls = useTls,
                DisplayName = finalDisplayName,
                CertPath = certPath,
                Metadata = metadata,
                MonitoringEnabled = monitoringEnabled,
                LastSeen = null,
                Reachability = AgentReachability.Unknown,
                HealthStatus = AgentHealth.Unknown
            };
        }
    }
}
