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
    public interface IAgentEndpointRegistry
    {
        /// <summary>
        /// Asynchronously registers the specified agent endpoint with the system.
        /// </summary>
        /// <param name="endpoint">The agent endpoint to register. Cannot be null.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the registration operation.</param>
        /// <returns>A task that represents the asynchronous registration operation. The task result contains a
        /// RegistrationResult indicating the outcome of the registration.</returns>
        Task<RegistrationResult> RegisterAsync(AgentEndpoint endpoint, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously deregisters the entity identified by the specified unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the entity to deregister.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the deregistration operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result is <see langword="true"/> if the entity
        /// was successfully deregistered; otherwise, <see langword="false"/>.</returns>
        Task<bool> DeregisterAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously retrieves a read-only collection of agent endpoints.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a read-only collection of <see
        /// cref="AgentEndpoint"/> objects representing the available agent endpoints.</returns>
        Task<IReadOnlyCollection<AgentEndpoint>> ListAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously retrieves the agent endpoint associated with the specified identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the agent endpoint to retrieve.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="AgentEndpoint"/>
        /// if found; otherwise, <see langword="null"/>.</returns>
        Task<AgentEndpoint?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
