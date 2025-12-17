using System;
using System.Collections.Generic;
using System.Text;

namespace paradigm_ehb.CommandCenter.Core.Enums
{
    /// <summary>
    /// Specifies the health status of an agent.
    /// Separated from <see cref="AgentReachability"/> which represents connectivity status.
    /// </summary>
    /// <remarks>Use this enumeration to represent the current operational state of an agent, such as a
    /// service or process. The values indicate whether the agent is functioning normally, experiencing issues, or if
    /// its status is unknown.</remarks>
    public enum AgentHealth
    {
        Unknown = 0,
        Healthy = 1,
        Degraded = 2
    }
}
