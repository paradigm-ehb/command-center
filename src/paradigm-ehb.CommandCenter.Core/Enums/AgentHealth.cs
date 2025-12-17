using System;
using System.Collections.Generic;
using System.Text;

namespace paradigm_ehb.CommandCenter.Core.Enums
{
    /// <summary>
    /// Specifies the health status of an AgentEndpoint, indicating its current operational condition.
    /// </summary>
    /// <remarks>Use this enumeration to represent and evaluate the state of an agent in monitoring or
    /// management scenarios. The values provide standardized status indicators for health checks, diagnostics, or
    /// reporting. The meaning of each value is as follows: - Healthy: The agent is operating normally. - Degraded: The
    /// agent is experiencing reduced performance or partial functionality. - Offline: The agent is not currently
    /// available or reachable. - Unknown: The agent's health status cannot be determined.</remarks>
    public enum AgentHealth
    {
        Healthy = 1,
        Degraded = 2,
        Unknown = 3
    }
}
