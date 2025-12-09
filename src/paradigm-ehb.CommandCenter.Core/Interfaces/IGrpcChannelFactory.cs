using Grpc.Net.Client;
using paradigm_ehb.CommandCenter.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace paradigm_ehb.CommandCenter.Core.Interfaces
{
    /// <summary>
    /// Creates or returns a configured GrpcChannel for the specified endpoint.
    /// </summary>
    public interface IGrpcChannelFactory
    {
        /// <summary>
        /// Creates a new gRPC channel for communication with the specified agent endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint information for the agent to which the channel will connect. Cannot be null.</param>
        /// <returns>A <see cref="GrpcChannel"/> instance configured to communicate with the specified agent endpoint.</returns>
        GrpcChannel CreateChannel(AgentEndpoint endpoint);
    }
}
