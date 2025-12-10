namespace paradigm_ehb.CommandCenter.Core.Models
{
    /// <summary>
    /// Result returned by <see cref="Interfaces.IAgentClientRegistry.RegisterAsync"/>.
    /// </summary>
    public sealed record AgentClientRegistrationResult(
            bool Registered,
            AgentClientEntry Entry,
            IReadOnlyCollection<string> Warnings,
            string? Message = null
        );
}