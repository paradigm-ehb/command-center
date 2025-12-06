using Grpc.Net.Client;
using Grpc.Net.ClientFactory;
using paradigm_ehb.CommandCenter.Core.Interfaces;
using paradigm_ehb.CommandCenter.Core.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace paradigm_ehb.CommandCenter.Core.Factories
{
    internal class GrpcChannelFactory : IGrpcChannelFactory
    {
        public GrpcChannel CreateChannel(AgentEndpoint endpoint)
        {
            // TODO: Add support for Health Checks, Interceptors, etc.
            return GrpcChannel.ForAddress($"http{(endpoint.UseTls ? 's' : null)}:{endpoint.IpAddress}:{endpoint.Port}");
        }
    }
}
