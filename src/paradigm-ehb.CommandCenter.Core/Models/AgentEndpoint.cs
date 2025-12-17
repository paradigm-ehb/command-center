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
        /// Gets or sets the date and time when the entity was last seen.
        /// </summary>
        public DateTimeOffset? LastSeen { get; set; }

        public bool MonitoringEnabled { get; init; } = true;

        /// <summary>
        /// Occurs when the reachability status of an agent endpoint changes.
        /// </summary>
        /// <remarks>Subscribe to this event to be notified when the specified agent endpoint becomes
        /// reachable or unreachable. The event provides the affected endpoint and its new reachability state.</remarks>
        public event EventHandler<AgentEndpoint, AgentReachability>? ReachabilityChanged;

        private AgentReachability _reachability;

        /// <summary>
        /// Latest transport-level reachability (Online/Offline). Independent from <see cref="HealthStatus"/>.
        /// </summary>
        public AgentReachability Reachability
        {
            get => _reachability;
            set
            {
                if (_reachability == value) return;
                _reachability = value;
                try
                {
                    ReachabilityChanged?.Invoke(this, value);
                }
                catch
                {
                    // ignore handler exceptions
                }
            }
        }

        /// <summary>
        /// Occurs when the health status of the agent changes.
        /// </summary>
        /// <remarks>Subscribers are notified whenever the agent's health status is updated. The event
        /// provides the new health status as an argument.</remarks>
        public event EventHandler<AgentEndpoint, AgentHealth>? HealthStatusChanged;

        private AgentHealth _healthStatus;

        /// <summary>
        /// Latest health flag maintained by health-checker.
        /// </summary>
        public AgentHealth HealthStatus
        {
            get => _healthStatus;
            set
            {
                if (_healthStatus == value) return;
                _healthStatus = value;
                try
                {
                    HealthStatusChanged?.Invoke(this, value);
                }
                catch
                {
                    // ignore handler exceptions to avoid breaking callers
                }
            }
        }

        
    }
}
