namespace paradigm_ehb.CommandCenter.Core.Enums
{
    /// <summary>
    /// Specifies the reachability status of an agent.
    /// Separated from <see cref="AgentHealth"/> which represents semantic/operational health.
    /// </summary>
    /// <remarks>Use this enumeration to indicate whether an agent is currently reachable, unreachable, or if
    /// its status is unknown. This can be used to determine availability for communication or task
    /// assignment.</remarks>
    public enum AgentReachability
    {
        Unknown = 0,
        Online = 1,
        Offline = 2
    }
}