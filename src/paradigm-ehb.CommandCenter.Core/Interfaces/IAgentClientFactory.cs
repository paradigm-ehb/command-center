using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using paradigm_ehb.CommandCenter.Core.Models;

namespace paradigm_ehb.CommandCenter.Core.Interfaces
{
    /// <summary>
    /// Defines a factory for creating, registering, and retrieving agent client entries associated with agent
    /// endpoints.
    /// </summary>
    /// <remarks>Implementations of this interface are responsible for managing the lifecycle of agent
    /// clients, including creation, registration, and retrieval by endpoint. The interface extends <see
    /// cref="IDisposable"/>, so consumers should ensure proper disposal of the factory to release any associated
    /// resources.</remarks>
    public interface IAgentClientFactory : IDisposable
    {
        /// <summary>
        /// Asynchronously creates and registers a new client for the specified agent endpoint.
        /// </summary>
        /// <param name="endpoint">The agent endpoint to connect to. Cannot be null.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains an <see
        /// cref="AgentClientEntry"/> representing the registered client.</returns>
        Task<AgentClientEntry> CreateClientAsync(AgentEndpoint endpoint, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new client for the specified agent endpoint and registers it for use.
        /// </summary>
        /// <param name="endpoint">The agent endpoint to connect to. Cannot be null.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains an entry representing the
        /// registered agent client.</returns>
        Task<AgentClientEntry> CreateAndRegisterClientAsync(AgentEndpoint endpoint, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously retrieves the client entry associated with the specified endpoint identifier.
        /// </summary>
        /// <param name="endpointId">The unique identifier of the endpoint for which to retrieve the client entry.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the <see
        /// cref="AgentClientEntry"/> associated with the specified endpoint if found; otherwise, <see
        /// langword="null"/>.</returns>
        Task<AgentClientEntry?> GetClientAsync(Guid endpointId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously retrieves all registered agent client entries.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a read-only collection of all
        /// agent client entries. The collection is empty if no clients are registered.</returns>
        Task<IReadOnlyCollection<AgentClientEntry>> GetAllClientsAsync(CancellationToken cancellationToken = default);
    }
}
