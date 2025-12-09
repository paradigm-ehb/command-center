using System;
using System.Collections.Generic;
using paradigm_ehb.CommandCenter.Core.Models;

namespace paradigm_ehb.CommandCenter.Core.Interfaces
{
    /// <summary>
    /// Creates and manages gRPC client entries for agent endpoints.
    /// </summary>
    internal interface IGrpcClientFactory : IDisposable
    {
        /// <summary>
        /// Creates a new client entry for the specified agent endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint information for the agent to associate with the client. Cannot be null.</param>
        /// <returns>An <see cref="AgentClientEntry"/> instance representing the newly created client for the specified endpoint.</returns>
        AgentClientEntry CreateClient(AgentEndpoint endpoint);

        /// <summary>
        /// Retrieves the client entry associated with the specified endpoint identifier.
        /// </summary>
        /// <param name="endpointId">The unique identifier of the endpoint for which to retrieve the client entry.</param>
        /// <returns>An <see cref="AgentClientEntry"/> representing the client associated with the specified endpoint identifier,
        /// or <see langword="null"/> if no client is found.</returns>
        AgentClientEntry? GetClient(Guid endpointId);

        /// <summary>
        /// Returns all currently managed client entries.
        /// </summary>
        IEnumerable<AgentClientEntry> GetAllClients();
    }
}
