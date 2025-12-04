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
        GrpcChannel CreateChannel(AgentEndpoint endpoint);
    }
}
