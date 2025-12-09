using paradigm_ehb.CommandCenter.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace paradigm_ehb.CommandCenter.Core.Interfaces
{
    internal interface IAgentClientRegistry : IDisposable
    {
        /// <summary>
        /// Retrieves an existing client entry for the specified agent endpoint, or creates a new entry if none exists.
        /// </summary>
        /// <param name="endpoint">The agent endpoint for which to retrieve or create the client entry. Cannot be null.</param>
        /// <returns>An instance of <see cref="AgentClientEntry"/> associated with the specified endpoint. If an entry already
        /// exists, it is returned; otherwise, a new entry is created and returned.</returns>
        AgentClientEntry CreateOrGet(AgentEndpoint endpoint);

        /// <summary>
        /// Retrieves the agent client entry associated with the specified endpoint identifier.
        /// </summary>
        /// <param name="endpointId">The unique identifier of the endpoint for which to retrieve the agent client entry.</param>
        /// <returns>An <see cref="AgentClientEntry"/> instance if an entry exists for the specified endpoint; otherwise, <see
        /// langword="null"/>.</returns>
        AgentClientEntry? Get(Guid endpointId);

        /// <summary>
        /// Asynchronously retrieves a read-only collection of agent client entries.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a read-only collection of <see
        /// cref="AgentClientEntry"/> objects representing the agent clients. The collection will be empty if no agent
        /// clients are available.</returns>
        Task<IReadOnlyCollection<AgentClientEntry>> ListAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously removes the endpoint identified by the specified ID.
        /// </summary>
        /// <param name="endpointId">The unique identifier of the endpoint to remove.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the remove operation.</param>
        /// <returns>A task that represents the asynchronous remove operation. The task result is <see langword="true"/> if the
        /// endpoint was successfully removed; otherwise, <see langword="false"/>.</returns>
        Task<bool> RemoveAsync(Guid endpointId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Marks the specified endpoint as used, updating its usage status asynchronously.
        /// </summary>
        /// <param name="endpointId">The unique identifier of the endpoint to mark as used.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task MarkUsed(Guid endpointId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Selects the specified endpoint for subsequent operations.
        /// </summary>
        /// <param name="endpointId">The unique identifier of the endpoint to select. If <paramref name="endpointId"/> is <see langword="null"/>,
        /// no endpoint will be selected.</param>
        /// <returns>A task that represents the asynchronous select operation.</returns>
        Task Select(Guid? endpointId);
    }
}
