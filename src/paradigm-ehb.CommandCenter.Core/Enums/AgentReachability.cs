namespace paradigm_ehb.CommandCenter.Core.Enums
{
    /// <summary>
    /// Lightweight reachability/online status for an agent endpoint.
    /// Separated from <see cref="AgentHealth"/> which represents semantic/operational health.
    /// </summary>
    public enum AgentReachability
    {
        Unknown = 0,
        Online = 1,
        Offline = 2
    }
}