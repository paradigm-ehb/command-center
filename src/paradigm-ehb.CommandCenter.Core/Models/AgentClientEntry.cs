using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Channels;

namespace paradigm_ehb.CommandCenter.Core.Models
{
    public sealed class AgentClientEntry
    {
        /// <summary>
        /// Gets the unique identifier for the endpoint.
        /// </summary>
        public Guid EndpointId { get; init; }

        /// <summary>
        /// Gets the gRPC channel used for remote procedure calls.
        /// </summary>
        /// <remarks>The channel provides the underlying transport for gRPC client operations. This
        /// property is initialized during object construction and cannot be modified after initialization.</remarks>
        public GrpcChannel Channel { get; init; }

        /// <summary>
        /// Gets the gRPC client used to communicate with the Greeter service.
        /// </summary>
        /// <remarks>Use this client to invoke remote procedures defined by the Greeter service, such as
        /// sending greeting requests. The property is initialized during object construction and cannot be modified
        /// afterwards.</remarks>
        public Greeter.GreeterClient Greeter { get; init; }
    }
}
