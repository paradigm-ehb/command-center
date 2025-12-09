using paradigm_ehb.CommandCenter.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Text;

namespace paradigm_ehb.CommandCenter.Core.Interfaces
{
    /// <summary>
    /// Stores and enumerates agent endpoints. Lightweight, transport-agnostic.
    /// </summary>
    public interface IAgentRegistry
    {
        Task<RegistrationResult> RegisterAsync(AgentEndpoint endpoint, CancellationToken cancellationToken = default);
        Task<bool> DeregisterAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<AgentEndpoint>> ListAsync(CancellationToken cancellationToken = default);
        Task<AgentEndpoint?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
