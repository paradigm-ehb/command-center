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
    internal class GrpcChannelFactory : GrpcClientFactory, IGrpcChannelFactory
    {
        public GrpcChannel CreateChannel(AgentEndpoint endpoint)
        {
            // TODO: add Channel Creation
        }

        public override TClient CreateClient<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TClient>(string name)
        {
            throw new NotImplementedException();
        }
    }
}
