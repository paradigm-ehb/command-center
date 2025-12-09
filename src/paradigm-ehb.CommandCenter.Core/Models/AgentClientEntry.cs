using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Channels;

namespace paradigm_ehb.CommandCenter.Core.Models
{
    public sealed class AgentClientEntry
    {
        public Guid EndpointId { get; init; }
        public GrpcChannel Channel { get; init; }
        public Greeter.GreeterClient Greeter { get; init; }
        public Tester.TesterClient Tester { get; init; }
    }
}
