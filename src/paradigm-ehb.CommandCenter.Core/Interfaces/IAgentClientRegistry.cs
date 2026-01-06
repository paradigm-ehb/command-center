using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using paradigm_ehb.CommandCenter.Core.Models;

namespace paradigm_ehb.CommandCenter.Core.Interfaces
{
    /// <summary>
    /// Defines a contract for managing the registration, deregistration, and retrieval of agent client entries within
    /// the system.
    /// </summary>
    /// <remarks>Implementations of this interface are responsible for tracking agent client endpoints and
    /// their associated metadata. All operations are asynchronous and support cancellation via a provided token. The
    /// interface extends <see cref="IDisposable"/>, indicating that implementations may hold resources that should be
    /// released when no longer needed.</remarks>
    public interface IAgentClientRegistry : IDisposable
    {
        /// <summary>
        /// Registers a new agent client asynchronously using the specified entry information.
        /// </summary>
        /// <param name="entry">The agent client entry containing registration details. Cannot be null.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the registration operation.</param>
        /// <returns>A task that represents the asynchronous registration operation. The task result contains the outcome of the
        /// agent client registration.</returns>
        Task<AgentClientRegistrationResult> RegisterAsync(AgentClient entry, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously deregisters the specified endpoint from the system.
        /// </summary>
        /// <param name="endpointId">The unique identifier of the endpoint to deregister.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result is <see langword="true"/> if the endpoint
        /// was successfully deregistered; otherwise, <see langword="false"/>.</returns>
        Task<bool> DeregisterAsync(Guid endpointId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously retrieves a read-only collection of all registered agent client entries.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a read-only collection of agent
        /// client entries. The collection is empty if no agent clients are registered.</returns>
        Task<IReadOnlyCollection<AgentClient>> ListAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously retrieves the agent client entry associated with the specified endpoint identifier.
        /// </summary>
        /// <param name="endpointId">The unique identifier of the endpoint for which to retrieve the agent client entry.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the <see
        /// cref="AgentClient"/> associated with the specified endpoint if found; otherwise, <see
        /// langword="null"/>.</returns>
        Task<AgentClient?> GetAsync(Guid endpointId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Determines asynchronously whether the specified endpoint is currently registered.
        /// </summary>
        /// <param name="endpointId">The unique identifier of the endpoint to check for registration.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains <see langword="true"/> if the
        /// endpoint is registered; otherwise, <see langword="false"/>.</returns>
        Task<bool> IsRegisteredAsync(Guid endpointId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Determines asynchronously whether the specified agent client is registered.
        /// </summary>
        /// <param name="agentClient">The agent client to check for registration status. Cannot be null.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains <see langword="true"/> if the
        /// agent client is registered; otherwise, <see langword="false"/>.</returns>
        Task<bool> IsRegisteredAsync(AgentClient agentClient, CancellationToken cancellationToken = default);
    }
}
